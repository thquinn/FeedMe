using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    static Vector3 FRICTION_VECTOR = new Vector3(.7f, 1, .7f);
    static float JUMP_FORCE = 4.5f;
    static Vector3 HOLD_POSITION = new Vector3(.2f, -.15f, .3f);
    static float THROW_FORCE = 80f;

    private LayerMask layerMaskTerrain, layerMaskGrabbable;

    public GameObject neck;
    public Rigidbody rb;    
    public DeathScript deathScript;

    Rigidbody grabbedBody;
    int jumpCooldown;

    void Start()
    {
        Application.targetFrameRate = 60;
        Cursor.lockState = CursorLockMode.Locked;
        layerMaskTerrain = LayerMask.GetMask("Terrain");
        layerMaskGrabbable = LayerMask.GetMask("Grabbable");
    }

    void Update() {
        LookControls(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        MoveControls(Input.GetAxis("Forward"), Input.GetAxis("Right"));
        GrabControls();
    }
    void LookControls(float x, float y) {
        transform.Rotate(0, x, 0);
        float thetaX = neck.transform.localRotation.eulerAngles.x;
        if (thetaX > 180) {
            thetaX -= 360;
        }
        thetaX = Mathf.Clamp(thetaX - y, -90, 90);
        neck.transform.localRotation = Quaternion.Euler(thetaX, 0, 0);
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
            jumpCooldown = 4;
        }
    }
    void GrabControls() {
        if (Input.GetButtonDown("Grab")) {
            if (grabbedBody == null) {
                TryGrab();
            } else {
                Throw();
            }
        }
        if (grabbedBody) {
            Vector3 neckPosition = neck.transform.localPosition;
            neck.transform.Translate(HOLD_POSITION, Space.Self);
            Vector3 holdPosition = neck.transform.position;
            Debug.DrawLine(holdPosition, holdPosition + Vector3.up, Color.white, 1);
            neck.transform.localPosition = neckPosition;
            grabbedBody.transform.position = Vector3.Lerp(grabbedBody.position, holdPosition, .33f);
            grabbedBody.detectCollisions = false;
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
        grabbedBody = hitInfo.collider.GetComponent<Rigidbody>();
        grabbedBody.transform.parent = neck.transform;
        grabbedBody.isKinematic = true;
        grabbedBody.velocity = Vector3.zero;
        grabbedBody.angularVelocity = Vector3.zero;
    }
    void Throw() {
        grabbedBody.isKinematic = false;
        grabbedBody.detectCollisions = true;
        grabbedBody.AddForce(neck.transform.forward * THROW_FORCE);
        grabbedBody.transform.parent = null;
        grabbedBody = null;
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
