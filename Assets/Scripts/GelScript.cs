using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GelScript : MonoBehaviour
{
    static Vector3 FRICTION_VECTOR = new Vector3(.91f, 1, .91f);
    static float PLAYER_CHASE_DISTANCE = 2;
    static float SQR_EATING_DISTANCE = .4f;
    static float HOP_HEIGHT = .33f;
    static float HOP_FORCE = 3f;
    static float HAPPY_HOP_FORCE = .5f;
    static int HOP_COOLDOWN = 15;
    static float EAT_RATE = .005f;

    private LayerMask layerMaskGrabbable;

    public GameObject player, gimbal;
    public Rigidbody rb;
    public SpriteRenderer shadowRenderer;
    public DeathScript deathScript;
    public AudioClip audioClip;
    public AudioSource[] sfxSqueaks, sfxMunches;

    float distanceToPlayer, sqrDistanceToFood;
    int hopCooldown;

    void Start() {
        layerMaskGrabbable = LayerMask.GetMask("Grabbable");
    }

    void Update()
    {
        if (hopCooldown > 0) {
            hopCooldown--;
        }
        UpdateDistanceToPlayer();
        Logic();
        rb.velocity = Vector3.Scale(rb.velocity, FRICTION_VECTOR);
        // Effects.
        Util.UpdateShadow(gameObject, shadowRenderer);
    }
    void UpdateDistanceToPlayer() {
        Vector3 deltaPos = player.transform.localPosition - transform.localPosition;
        distanceToPlayer = Mathf.Sqrt(deltaPos.x * deltaPos.x + deltaPos.z * deltaPos.z);
    }
    void Logic() {
        FruitScript nearbyFruit = GetNearbyFruit();
        if (nearbyFruit == null) {
            // Player chase logic.
            LookAtPlayer();
            if (distanceToPlayer > PLAYER_CHASE_DISTANCE) {
                Hop();
            }
        } else {
            // Food logic.
            LookAtFood(nearbyFruit.gameObject);
            if (sqrDistanceToFood > SQR_EATING_DISTANCE) {
                Hop();
            } else {
                HappyHop();
                nearbyFruit.Eat(EAT_RATE);
            }
        }
    }
    void Hop() {
        if (hopCooldown > 0) {
            return;
        }
        Vector3 forward = transform.forward;
        forward.y += HOP_HEIGHT;
        rb.AddForce(forward * HOP_FORCE, ForceMode.Impulse);
        hopCooldown = HOP_COOLDOWN;
        AudioSource sfxSqueak = sfxSqueaks[Random.Range(0, sfxSqueaks.Length)];
        sfxSqueak.pitch = 1.5f;
        sfxSqueak.PlayOneShot(sfxSqueak.clip, 0.2f);
    }
    void HappyHop() {
        if (hopCooldown > 0) {
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
        float oldThetaX = gimbal.transform.localRotation.eulerAngles.x;
        oldThetaX = Util.CorrectDegreeDiscrepancy(oldThetaX, thetaX);
        gimbal.transform.localRotation = Quaternion.Euler(Mathf.Lerp(oldThetaX, thetaX, .1f), 0, 0);
    }

    FruitScript GetNearbyFruit() {
        FruitScript closest = null;
        float closestSqrDistance = float.MaxValue;
        Collider[] grabbables = Physics.OverlapSphere(transform.position, 5, layerMaskGrabbable);
        foreach (Collider grabbable in grabbables) {
            if (grabbable.attachedRigidbody.isKinematic) {
                continue;
            }
            float sqrDistance = (transform.localPosition - grabbable.transform.localPosition).sqrMagnitude;
            if (sqrDistance < closestSqrDistance) {
                closest = grabbable.GetComponent<FruitScript>();
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
}

