using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitScript : MonoBehaviour
{
    static float VIBRATION_INTENSITY = .02f;

    public GameObject popEffectPrefab;

    public Rigidbody rb;
    public SpriteRenderer shadowRenderer;
    public GameObject meshObject;

    int frames;
    StemScript stemScript;
    float amountLeft = 1;
    Vector3 initialMeshPosition;
    float vibration;

    public void Spawn(StemScript stemScript) {
        this.stemScript = stemScript;
        transform.localPosition = stemScript.transform.position + stemScript.transform.up * .22f;
    }
    public void Pick() {
        if (stemScript != null) {
            stemScript.Pick();
            stemScript = null;
        }
    }
    public void Eat(float amount) {
        rb.velocity *= .9f;
        rb.angularVelocity *= .8f;
        amountLeft -= amount;
        vibration = Mathf.Lerp(vibration, Mathf.Pow(1 - amountLeft, 2) * VIBRATION_INTENSITY, .25f);
        if (amountLeft <= 0) {
            Instantiate(popEffectPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    void Start() {
        initialMeshPosition = meshObject.transform.localPosition;
        transform.localScale = Vector3.zero;
    }
    void Update()
    {
        frames++;
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, .1f);
        Util.UpdateShadow(gameObject, shadowRenderer);
        Vector3 localPosition = initialMeshPosition;
        localPosition.x += Mathf.Cos(frames * 2f) * vibration;
        meshObject.transform.localPosition = localPosition;
        vibration *= .98f;
    }
}
