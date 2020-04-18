using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    static Vector3 FRICTION_VECTOR = new Vector3(.7f, 1, .7f);
    static float JUMP_FORCE = 4.5f;

    private LayerMask layerMaskTerrain, layerMaskGrabbable;

    public GameObject neck;
    public Rigidbody rb;
    public DeathScript deathScript;

    int jumpCooldown = 4;

    void Start()
    {
        Application.targetFrameRate = 60;
        layerMaskTerrain = LayerMask.GetMask("Terrain");
        layerMaskGrabbable = LayerMask.GetMask("Grabbable");
    }

    void Update()
    {
        LookControls(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        MoveControls(Input.GetAxis("Forward"), Input.GetAxis("Right"));
        if (Input.GetButtonDown("Grab")) {
            TryGrab();
        }
    }
    void LookControls(float x, float y) {
        transform.Rotate(0, x, 0);
        neck.transform.Rotate(-y, 0, 0);
    }
    void MoveControls(float forward, float right) {
        float speed = 3f;
        rb.velocity += transform.forward * forward * speed;
        rb.velocity += transform.right * right * speed;
        rb.velocity = Vector3.Scale(rb.velocity, FRICTION_VECTOR);
        if (jumpCooldown > 0) {
            jumpCooldown--;
        } else if (Input.GetButtonDown("Jump") && IsOnGround()) {
            rb.velocity += Vector3.up * JUMP_FORCE;
        }
    }

    void TryGrab() {
        RaycastHit hitInfo;
        Physics.Raycast(neck.transform.position, neck.transform.forward, out hitInfo, 2, layerMaskGrabbable);
        if (!hitInfo.collider) {
            return;
        }
        FruitScript fruitScript = hitInfo.collider.GetComponent<FruitScript>();
        fruitScript.Pick();
    }

    bool IsOnGround() {
        // TODO: Cast multiple rays to allow jumping while leaning over an edge.
        RaycastHit hitInfo;
        Physics.Raycast(transform.position, Vector3.down, out hitInfo, .55f, layerMaskTerrain);
        //Debug.DrawLine(transform.position, transform.position + Vector3.down * .55f, Color.white, 10);
        return hitInfo.collider != null;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.tag == "Death") {
            deathScript.Die();
        }
    }
}
