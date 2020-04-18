using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    static Vector3 FRICTION_VECTOR = new Vector3(.7f, 1, .7f);

    public GameObject neck;
    public Rigidbody rb;
    public DeathScript deathScript;

    void Start()
    {
        Application.targetFrameRate = 60;
    }

    void Update()
    {
        LookControls(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        MoveControls(Input.GetAxis("Forward"), Input.GetAxis("Right"));
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
    }

    private void OnTriggerEnter(Collider other) {
        if (other.tag == "Death") {
            deathScript.Die();
        }
    }
}
