using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    public static bool CAN_INPUT = false;

    static Vector3 FRICTION_VECTOR = new Vector3(.7f, 1, .7f);
    static float JUMP_FORCE = 4.75f;
    static Vector3 HOLD_POSITION = new Vector3(.2f, -.15f, .3f);
    static float THROW_FORCE = 80f;
    static float WHISTLE_DISTANCE = 8;
    static int WHISTLE_READY_NO_GO_COOLDOWN = 300;
    static int WHISTLE_READY_GO_COOLDOWN = 150;
    static int WHISTLE_GO_COOLDOWN = 20;
    static int WHISTLE_FAR_EFFECT_FRAMES = 300;
    static float Y_COOR_DEAD = -4;

    private LayerMask layerMaskTerrain, layerMaskGrabbable;

    public GameObject neck;
    public Rigidbody rb;
    public GelScript gelScript;
    public DeathScript deathScript;

    public AudioSource sfxWhistleReady, sfxWhistleGo, sfxWhistleDistant, sfxCrunch;

    Rigidbody grabbedBody;
    int whistleReadyCooldown, whistleGoCooldown;
    int holdDuration = 0;
    public int farWhistleFrames = 0;

    void Start()
    {
        Application.targetFrameRate = 60;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        layerMaskTerrain = LayerMask.GetMask("Terrain");
        layerMaskGrabbable = LayerMask.GetMask("Grabbable");
    }

    void Update() {
        if (Input.GetKey(KeyCode.Escape)) {
            Application.Quit();
        }
        if (!CAN_INPUT) {
            return;
        }
        if (transform.localPosition.y < Y_COOR_DEAD) {
            deathScript.PlayerDie(DeathReason.PlayerFall);
        }
        LookControls(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        MoveControls(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
        GrabControls();
        WhistleControls();
    }
    void LookControls(float x, float y) {
        transform.Rotate(0, x, 0);
        if (grabbedBody != null) {
            grabbedBody.transform.Rotate(0, x, 0, Space.World);
        }
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
        if (Input.GetButtonDown("Jump") && Util.IsOnGround(gameObject, 16, .4f, .55f)) {
            rb.velocity = Vector3.up * JUMP_FORCE;
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
            holdDuration++;
            Vector3 neckPosition = neck.transform.localPosition;
            neck.transform.Translate(HOLD_POSITION, Space.Self);
            Vector3 holdPosition = neck.transform.position;
            neck.transform.localPosition = neckPosition;
            float lerpFactor = Mathf.Min(.33f + .01f * holdDuration, .99f);
            grabbedBody.transform.position = Vector3.Lerp(grabbedBody.position, holdPosition, lerpFactor);
            grabbedBody.detectCollisions = false;
        } else {
            holdDuration = 0;
        }
    }
    void WhistleControls() {
        if (sfxWhistleGo.isPlaying) {
            sfxWhistleReady.volume -= .2f;
        } else {
            sfxWhistleReady.volume = 1f;
        }
        whistleReadyCooldown = Mathf.Max(0, whistleReadyCooldown - 1);
        whistleGoCooldown = Mathf.Max(0, whistleGoCooldown - 1);
        farWhistleFrames = Mathf.Max(0, farWhistleFrames - 1);
        if (!Input.GetButtonDown("Whistle")) {
            return;
        }
        float distanceToGel = (gelScript.transform.position - transform.position).magnitude;
        if (distanceToGel > WHISTLE_DISTANCE && whistleReadyCooldown == 0) {
            sfxWhistleDistant.Play();
            farWhistleFrames = WHISTLE_FAR_EFFECT_FRAMES;
            whistleReadyCooldown = WHISTLE_READY_GO_COOLDOWN;
        } else if (gelScript.IsWhistleReadied() && whistleGoCooldown == 0) {
            sfxWhistleGo.Play();
            gelScript.WhistleJump();
            whistleReadyCooldown = WHISTLE_READY_GO_COOLDOWN;
        } else if (whistleReadyCooldown == 0) {
            sfxWhistleReady.Play();
            gelScript.WhistleReady();
            whistleReadyCooldown = WHISTLE_READY_NO_GO_COOLDOWN;
            whistleGoCooldown = WHISTLE_GO_COOLDOWN;
        }
    }

    void TryGrab() {
        RaycastHit hitInfo;
        Physics.Raycast(neck.transform.position, neck.transform.forward, out hitInfo, 2, layerMaskGrabbable);
        if (!hitInfo.collider) {
            return;
        }
        grabbedBody = hitInfo.collider.GetComponent<Rigidbody>();
        grabbedBody.transform.parent = neck.transform;
        grabbedBody.isKinematic = true;
        grabbedBody.velocity = Vector3.zero;
        grabbedBody.angularVelocity = Vector3.zero;
        FruitScript fruitScript = hitInfo.collider.GetComponent<FruitScript>();
        fruitScript.Pick();
    }
    void Throw() {
        grabbedBody.isKinematic = false;
        grabbedBody.detectCollisions = true;
        grabbedBody.AddForce(neck.transform.forward * THROW_FORCE);
        grabbedBody.transform.parent = null;
        grabbedBody = null;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.tag == "Death") {
            sfxCrunch.Play();
            deathScript.PlayerDie(DeathReason.PlayerHazard);
        }
    }
}
