using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GelScript : MonoBehaviour
{
    static Vector3 FRICTION_VECTOR = new Vector3(.91f, 1, .91f);
    static float PLAYER_CHASE_DISTANCE = 2;
    static float FOOD_CHASE_DISTANCE = 6;
    static float SQR_EATING_DISTANCE = .4f;
    static float HUNGER_MODERATE = 60;
    static float HUNGER_SEVERE = 120;
    static float HUNGER_CRITICAL = 150;
    static float HUNGER_DEAD = 180;
    static float SATIATION_FRUIT = 30;
    static float HOP_HEIGHT = .33f;
    static float HOP_FORCE = 3f;
    static int SPEED_BOOST_DURATION = 15 * 60;
    static float SPEED_BOOST_MULTIPLIER = 1.25f;
    static float HAPPY_HOP_FORCE = .5f;
    static float WHISTLE_HOP_FORCE = 4.5f;
    static float WHISTLE_HOP_HEIGHT = 1.25f;
    static int HOP_COOLDOWN = 15;
    static int WHISTLE_HOP_COOLDOWN = 75;
    static float EAT_RATE = .033f;
    static int WHISTLE_READY_TICKS = 180;
    static Vector3 WHISTLE_READY_ANIM_SCALE = new Vector3(1, .8f, 1);
    static Vector3 WHISTLE_READY_ANIM_SHIFT = new Vector3(0, -.15f, 0);
    static int STARE_TIME = 90;
    static float SPEECH_CONTENTS_SHAKE_AMOUNT = .033f;

    private LayerMask layerMaskGrabbable;
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

    bool isOnGround;
    float distanceToPlayer, sqrDistanceToFood;
    float hunger;
    int hopCooldown;
    int speedBoostFrames;
    int whistleReadyTicks;
    FruitColor desiredFruit;
    float desireLeft;
    int stareTimer;

    void Start() {
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

    void Update()
    {
        hunger += .0167f;
        if (hopCooldown > 0) {
            hopCooldown--;
        }
        if (speedBoostFrames > 0) {
            speedBoostFrames--;
        }
        if (whistleReadyTicks > 0) {
            whistleReadyTicks--;
            hopCooldown = 1;
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
            desiredFruit = (FruitColor)Random.Range(1, System.Enum.GetNames(typeof(FruitColor)).Length);
            desireLeft = 1;
            fruitImage.color = desiredFruit.ToUnityColor();
        }
    }
    void Logic() {
        bool stare = false;
        FruitScript nearbyFruit = GetNearbyFruit();
        if (nearbyFruit == null) {
            // Player chase logic.
            LookAtPlayer();
            if (distanceToPlayer > PLAYER_CHASE_DISTANCE) {
                Hop();
            } else {
                stare = true;
            }
        } else {
            // Food logic.
            LookAtFood(nearbyFruit.gameObject);
            if (sqrDistanceToFood > SQR_EATING_DISTANCE) {
                Hop();
            } else if (hopCooldown == 0) {
                HappyHop();
                bool done = nearbyFruit.Eat(EAT_RATE);
                hunger = Mathf.Max(0, hunger - EAT_RATE * SATIATION_FRUIT);
                desireLeft = done ? 0 : desireLeft - EAT_RATE;
                if (done) {
                    speedBoostFrames = SPEED_BOOST_DURATION;
                }
            }
        }
        // Stare logic.
        float dot = Vector3.Dot(cam.transform.forward, (transform.position - cam.transform.position).normalized);
        stare &= dot > .75f;
        stare &= whistleReadyTicks == 0;
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
        } else if (stareTimer < STARE_TIME) {
            speechCanvasGroup.alpha -= .1f;
        }
        // Whistle ready animation.
        gimbal.transform.localPosition = Vector3.Lerp(gimbal.transform.localPosition, whistleReadyTicks > 0 ? WHISTLE_READY_ANIM_SHIFT : Vector3.zero, .2f);
        gimbal.transform.localScale = Vector3.Lerp(gimbal.transform.localScale, whistleReadyTicks > 0 ? WHISTLE_READY_ANIM_SCALE : Vector3.one, .2f);
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

    void SpeechEffects() {
        speechCanvas.transform.LookAt(cam.transform);
        bubbleImage.sprite = hunger < HUNGER_SEVERE ? bubbleNormal : bubbleCritical;
        exclamationImage.enabled = hunger > HUNGER_CRITICAL;
        float contentShake = Mathf.InverseLerp(HUNGER_MODERATE, HUNGER_SEVERE, hunger) * SPEECH_CONTENTS_SHAKE_AMOUNT;
        Vector3 contentPosition = speechContentsInitialPosition + new Vector3(contentShake * Random.Range(-1f, 1f), contentShake * Random.Range(-1f, 1f), 0);
        speechContents.transform.localPosition = contentPosition;
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
            deathScript.Die();
        }
    }

    public void WhistleReady() {
        whistleReadyTicks = WHISTLE_READY_TICKS;
    }
    public bool IsWhistleReadied() {
        return whistleReadyTicks > 0;
    }
    public void WhistleJump() {
        whistleReadyTicks = 0;
        if (!isOnGround) {
            return;
        }
        Vector3 forward = transform.forward;
        forward.y += WHISTLE_HOP_HEIGHT;
        rb.AddForce(forward * WHISTLE_HOP_FORCE, ForceMode.Impulse);
        hopCooldown = WHISTLE_HOP_COOLDOWN;
    }
}

