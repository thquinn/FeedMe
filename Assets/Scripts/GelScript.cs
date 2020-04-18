using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GelScript : MonoBehaviour
{
    static Vector3 FRICTION_VECTOR = new Vector3(.91f, 1, .91f);
    static float PLAYER_CHASE_DISTANCE = 2;
    static float HOP_HEIGHT = .33f;
    static float HOP_FORCE = 3f;
    static int HOP_COOLDOWN = 15;

    public GameObject player, gimbal;
    public Rigidbody rb;
    public SpriteRenderer shadowRenderer;
    public DeathScript deathScript;

    float distanceToPlayer;
    int hopCooldown;

    // Update is called once per frame
    void Update()
    {
        if (hopCooldown > 0) {
            hopCooldown--;
        }
        UpdateDistanceToPlayer();
        LookAtPlayer();
        // Movement.
        if (distanceToPlayer > PLAYER_CHASE_DISTANCE) {
            Hop();
        }
        rb.velocity = Vector3.Scale(rb.velocity, FRICTION_VECTOR);
        // Effects.
        Util.UpdateShadow(gameObject, shadowRenderer);
    }
    void UpdateDistanceToPlayer() {
        Vector3 deltaPos = player.transform.localPosition - transform.localPosition;
        distanceToPlayer = Mathf.Sqrt(deltaPos.x * deltaPos.x + deltaPos.z * deltaPos.z);
    }
    void Hop() {
        if (hopCooldown > 0) {
            return;
        }
        Vector3 forward = transform.forward;
        forward.y += HOP_HEIGHT;
        rb.AddForce(forward * HOP_FORCE, ForceMode.Impulse);
        hopCooldown = HOP_COOLDOWN;
    }
    void LookAtPlayer() {
        // Y rotation.
        float oldThetaY = transform.localRotation.eulerAngles.y;
        transform.LookAt(player.transform);
        float newThetaY = transform.localRotation.eulerAngles.y;
        oldThetaY = Util.CorrectDegreeDiscrepancy(oldThetaY, newThetaY);
        transform.localRotation = Quaternion.Euler(0, Mathf.Lerp(oldThetaY, newThetaY, .1f), 0);
        // X rotation.
        float heightDifference = player.transform.localPosition.y - transform.localPosition.y + .33f;
        float oldThetaX = gimbal.transform.localRotation.eulerAngles.x;
        bool lookUp = hopCooldown == 0 && distanceToPlayer <= PLAYER_CHASE_DISTANCE;
        float newThetaX = lookUp ? -Mathf.Atan2(heightDifference, distanceToPlayer) * Mathf.Rad2Deg : 0;
        oldThetaX = Util.CorrectDegreeDiscrepancy(oldThetaX, newThetaX);
        gimbal.transform.localRotation = Quaternion.Euler(Mathf.Lerp(oldThetaX, newThetaX, .1f), 0, 0);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.tag == "Death") {
            deathScript.Die();
        }
    }
}

