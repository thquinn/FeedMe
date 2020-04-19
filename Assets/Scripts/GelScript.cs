using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GelScript : MonoBehaviour
{
    static Vector3 FRICTION_VECTOR = new Vector3(.88f, 1, .88f);
    static float PLAYER_CHASE_DISTANCE = 2;
    static float PLAYER_CHASE_DISTANCE_MAX = 11;
    static float FOOD_CHASE_DISTANCE = 6;
    static float SQR_EATING_DISTANCE = .4f;
    static float HUNGER_MODERATE = 60;
    static float HUNGER_SEVERE = 120;
    static float HUNGER_CRITICAL = 150;
    static float HUNGER_DEAD = 180;
    static float SATIATION_FRUIT = 30;
    static float Y_COOR_DEAD = -4;
    static float EAT_RATE = .033f;
    static float HOP_HEIGHT = .33f;
    static float HOP_FORCE = 3f;
    static int SPEED_BOOST_DURATION = 15 * 60;
    static float SPEED_BOOST_MULTIPLIER = 1.25f;
    static float HAPPY_HOP_FORCE = .5f;
    static float WHISTLE_HOP_FORCE = 3.75f;
    static float WHISTLE_HOP_HEIGHT = 1.65f;
    static int HOP_COOLDOWN = 15;
    static int WHISTLE_HOP_COOLDOWN = 75;
    static int WHISTLE_READY_TICKS = 180;
    static int WHISTLE_HOP_PUSH_FRAMES = 60;
    static float WHISTLE_HOP_PUSH_FORCE = .5f;
    static Vector3 WHISTLE_READY_ANIM_SCALE = new Vector3(1, .8f, 1);
    static Vector3 WHISTLE_READY_ANIM_SHIFT = new Vector3(0, -.15f, 0);
    static int STARE_TIME = 90;
    static float SPEECH_CONTENTS_SHAKE_AMOUNT = .033f;

    private LayerMask layerMaskTerrain, layerMaskGrabbable;
    Camera cam;
    Vector3 speechContentsInitialPosition;

    public GameObject player, gimbal, speechContents;
    public Rigidbody rb;
    public SpriteRenderer shadowRenderer;
    public Canvas speechCanvas;
    public CanvasGroup speechCanvasGroup;
    public Image bubbleImage, fruitImage, exclamationImage;
    public Sprite bubbleNormal, bubbleCritical;
    public DeathScript deathScript;
    public AudioSource[] sfxSqueaks, sfxMunches;
    public AudioSource sfxBeg;
    public MeshFilter meshFilter;
    public Mesh deadMesh;

    bool isOnGround;
    float distanceToPlayer, sqrDistanceToFood;
    float hunger;
    int hopCooldown;
    int speedBoostFrames;
    int whistleReadyFrames;
    int whistleHopPushFrames;

    FruitColor desiredFruit;
    float desireLeft;
    bool firstDesireDone;
    int stareTimer;

    void Start() {
        layerMaskTerrain = LayerMask.GetMask("Terrain");
        layerMaskGrabbable = LayerMask.GetMask("Grabbable");
        cam = Camera.main;
        speechContentsInitialPosition = speechContents.transform.localPosition;
        // Fix SFX volume curves.
        AnimationCurve squeakCurve = new AnimationCurve();
        squeakCurve.AddKey(0, 1);
        squeakCurve.AddKey(.33f, .33f);
        squeakCurve.AddKey(1, 0);
        squeakCurve.SmoothTangents(1, -.5f);
        foreach (AudioSource sfxSqueak in sfxSqueaks) {
            sfxSqueak.maxDistance = 15;
            sfxSqueak.rolloffMode = AudioRolloffMode.Custom;
            sfxSqueak.SetCustomCurve(AudioSourceCurveType.CustomRolloff, squeakCurve);
        }
    }

    void Update() {
        hunger += .0167f;
        if (hunger > HUNGER_DEAD) {
            Die(DeathReason.GelStarve);
            return;
        }
        if (transform.localPosition.y < Y_COOR_DEAD) {
            Die(DeathReason.GelFall);
            return;
        }
        if (hopCooldown > 0) {
            hopCooldown--;
        }
        if (speedBoostFrames > 0) {
            speedBoostFrames--;
        }
        if (whistleReadyFrames > 0) {
            whistleReadyFrames--;
            hopCooldown = 1;
        }
        if (whistleHopPushFrames > 0) {
            rb.AddForce(transform.forward * WHISTLE_HOP_PUSH_FORCE);
            whistleHopPushFrames--;
        }
        UpdateCachedState();
        UpdateDesire();
        Logic();
        if (isOnGround) {
            rb.velocity = Vector3.Scale(rb.velocity, FRICTION_VECTOR);
        }
        // Effects.
        SpeechEffects();
        Util.UpdateShadow(gameObject, shadowRenderer);
    }
    void UpdateCachedState() {
        isOnGround = Util.IsOnGround(gameObject, 16, .2f, .2f);
        Vector3 deltaPos = player.transform.localPosition - transform.localPosition;
        distanceToPlayer = Mathf.Sqrt(deltaPos.x * deltaPos.x + deltaPos.z * deltaPos.z);
    }
    void UpdateDesire() {
        if (desiredFruit == FruitColor.None || desireLeft <= 0) {
            FruitColor lastDesiredFruit = desiredFruit;
            while (desiredFruit == lastDesiredFruit) {
                desiredFruit = firstDesireDone ? (FruitColor)Random.Range(1, System.Enum.GetNames(typeof(FruitColor)).Length) : FruitColor.Red;
            }
            firstDesireDone = true;
            desireLeft = 1;
            fruitImage.color = desiredFruit.ToUnityColor();
        }
    }
    void Logic() {
        bool stare = false;
        FruitScript nearbyFruit = GetNearbyFruit();
        if (nearbyFruit == null && CanSeePlayer()) {
            // Player chase logic.
            LookAtPlayer();
            if (distanceToPlayer > PLAYER_CHASE_DISTANCE && distanceToPlayer < PLAYER_CHASE_DISTANCE_MAX) {
                Hop();
            } else if (distanceToPlayer < PLAYER_CHASE_DISTANCE_MAX) {
                stare = true;
            }
        } else if (nearbyFruit != null) {
            // Food logic.
            LookAtFood(nearbyFruit.gameObject);
            if (sqrDistanceToFood > SQR_EATING_DISTANCE) {
                Hop();
            } else {
                TinyPush(nearbyFruit.gameObject);
                if (hopCooldown == 0) {
                    HappyHop();
                    bool done = nearbyFruit.Eat(EAT_RATE);
                    hunger = Mathf.Max(0, hunger - EAT_RATE * SATIATION_FRUIT);
                    desireLeft = done ? 0 : desireLeft - EAT_RATE;
                    if (done) {
                        speedBoostFrames = SPEED_BOOST_DURATION;
                    }
                }
            }
        }
        // Stare logic.
        float dot = Vector3.Dot(cam.transform.forward, (transform.position - cam.transform.position).normalized);
        stare &= dot > .75f;
        stare &= isOnGround;
        stare &= whistleReadyFrames == 0;
        stare &= Time.timeScale > 0;
        if (stare) {
            stareTimer++;
        } else {
            stareTimer = 0;
        }
        if (stareTimer == STARE_TIME) {
            speechCanvasGroup.alpha = 1;
            if (hunger < HUNGER_MODERATE) {
                sfxBeg.pitch = 1.75f;
            } else if (hunger < HUNGER_SEVERE) {
                sfxBeg.pitch = 2f;
            } else {
                sfxBeg.pitch = 2.25f;
            }
            sfxBeg.Play();
            PlayerScript.CAN_INPUT = true;
        } else if (stareTimer < STARE_TIME) {
            speechCanvasGroup.alpha -= .1f;
        }
        // Whistle ready animation.
        bool whistleSquash = whistleReadyFrames > 0 && isOnGround && Time.timeScale > 0;
        gimbal.transform.localPosition = Vector3.Lerp(gimbal.transform.localPosition, whistleSquash ? WHISTLE_READY_ANIM_SHIFT : Vector3.zero, .2f);
        gimbal.transform.localScale = Vector3.Lerp(gimbal.transform.localScale, whistleSquash ? WHISTLE_READY_ANIM_SCALE : Vector3.one, .2f);
    }
    void Hop() {
        if (hopCooldown > 0 || !isOnGround) {
            return;
        }
        Vector3 forward = transform.forward;
        if (speedBoostFrames > 0) {
            forward *= SPEED_BOOST_MULTIPLIER;
        }
        forward.y += HOP_HEIGHT;
        forward *= HOP_FORCE;
        rb.AddForce(forward, ForceMode.Impulse);
        hopCooldown = HOP_COOLDOWN;
        AudioSource sfxSqueak = sfxSqueaks[Random.Range(0, sfxSqueaks.Length)];
        sfxSqueak.pitch = 1.5f;
        sfxSqueak.PlayOneShot(sfxSqueak.clip, 0.2f);
    }
    void HappyHop() {
        if (hopCooldown > 0 || !isOnGround) {
            return;
        }
        rb.AddForce(transform.up * HAPPY_HOP_FORCE, ForceMode.Impulse);
        hopCooldown = 10;
        AudioSource sfxMunch = sfxMunches[Random.Range(0, sfxMunches.Length)];
        sfxMunch.pitch = 1.5f;
        sfxMunch.PlayOneShot(sfxMunch.clip, 0.15f);
    }
    void LookAtPlayer() {
        // Y rotation.
        TurnToFace(player);
        // X rotation.
        float heightDifference = player.transform.localPosition.y - transform.localPosition.y + .33f;
        bool lookUp = hopCooldown == 0 && distanceToPlayer <= PLAYER_CHASE_DISTANCE;
        float newThetaX = lookUp ? -Mathf.Atan2(heightDifference, distanceToPlayer) * Mathf.Rad2Deg : 0;
        XTilt(newThetaX);
    }
    void LookAtFood(GameObject food) {
        TurnToFace(food);
        XTilt(0);
    }
    void TurnToFace(GameObject go) {
        float oldThetaY = transform.localRotation.eulerAngles.y;
        transform.LookAt(go.transform);
        float newThetaY = transform.localRotation.eulerAngles.y;
        oldThetaY = Util.CorrectDegreeDiscrepancy(oldThetaY, newThetaY);
        transform.localRotation = Quaternion.Euler(0, Mathf.Lerp(oldThetaY, newThetaY, .1f), 0);
    }
    void XTilt(float thetaX) {
        thetaX = Mathf.Clamp(thetaX, -45, 45);
        float oldThetaX = gimbal.transform.localRotation.eulerAngles.x;
        oldThetaX = Util.CorrectDegreeDiscrepancy(oldThetaX, thetaX);
        gimbal.transform.localRotation = Quaternion.Euler(Mathf.Lerp(oldThetaX, thetaX, .1f), 0, 0);
    }
    void TinyPush(GameObject gameObject) {
        float distance = (gameObject.transform.localPosition - transform.localPosition).sqrMagnitude;
        float tooClose = SQR_EATING_DISTANCE * .5f - distance;
        if (tooClose > 0) {
            Vector3 objPos = gameObject.transform.localPosition;
            objPos += transform.forward * tooClose * .1f;
            gameObject.transform.localPosition = objPos;
        }
    }

    void SpeechEffects() {
        speechCanvas.transform.LookAt(cam.transform);
        bubbleImage.sprite = hunger < HUNGER_SEVERE ? bubbleNormal : bubbleCritical;
        bubbleImage.materialForRendering.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
        exclamationImage.enabled = hunger > HUNGER_CRITICAL;
        float contentShake = Mathf.InverseLerp(HUNGER_MODERATE, HUNGER_SEVERE, hunger) * SPEECH_CONTENTS_SHAKE_AMOUNT;
        Vector3 shakePosition = speechContentsInitialPosition + new Vector3(contentShake * Random.Range(-1f, 1f), contentShake * Random.Range(-1f, 1f), 0);
        if (hunger < HUNGER_SEVERE) {
            speechContents.transform.localPosition = shakePosition;
            bubbleImage.transform.localPosition = Vector3.zero;
        } else {
            speechContents.transform.localPosition = shakePosition;
            bubbleImage.transform.localPosition = shakePosition - speechContentsInitialPosition;
        }
    }

    bool CanSeePlayer() {
        for (float offset = -.25f; offset <= .25f;  offset += .05f) {
            RaycastHit hitInfo;
            Vector3 rayOrigin = transform.localPosition + transform.right * offset;
            Vector3 rayDirection = player.transform.localPosition - transform.localPosition;
            float distance = rayDirection.magnitude;
            Physics.Raycast(rayOrigin, rayDirection, out hitInfo, distance, layerMaskTerrain);
            Debug.DrawLine(rayOrigin, player.transform.localPosition, Color.white, .1f);
            if (!hitInfo.collider) {
                return true;
            }
        }
        return false;
    }
    FruitScript GetNearbyFruit() {
        FruitScript closest = null;
        float closestSqrDistance = float.MaxValue;
        Collider[] grabbables = Physics.OverlapSphere(transform.position, FOOD_CHASE_DISTANCE, layerMaskGrabbable);
        foreach (Collider grabbable in grabbables) {
            if (grabbable.attachedRigidbody.isKinematic) {
                continue;
            }
            FruitScript fruitScript = grabbable.GetComponent<FruitScript>();
            if (fruitScript.color != desiredFruit) {
                continue;
            }
            float sqrDistance = (transform.localPosition - grabbable.transform.localPosition).sqrMagnitude;
            if (sqrDistance < closestSqrDistance) {
                closest = fruitScript;
                closestSqrDistance = sqrDistance;
            }
        }
        sqrDistanceToFood = closestSqrDistance;
        return closest;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.tag == "Death") {
            Die(DeathReason.GelHazard);
        }
    }
    void Die(DeathReason deathReason) {
        meshFilter.mesh = deadMesh;
        deathScript.GelDie(deathReason);
    }

    public void WhistleReady() {
        whistleReadyFrames = WHISTLE_READY_TICKS;
    }
    public bool IsWhistleReadied() {
        return whistleReadyFrames > 0;
    }
    public void WhistleJump() {
        whistleReadyFrames = 0;
        if (!isOnGround) {
            return;
        }
        Vector3 forward = transform.forward;
        forward.y += WHISTLE_HOP_HEIGHT;
        rb.AddForce(forward * WHISTLE_HOP_FORCE, ForceMode.Impulse);
        whistleHopPushFrames = WHISTLE_HOP_PUSH_FRAMES;
        hopCooldown = WHISTLE_HOP_COOLDOWN;
    }
}

