using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitScript : MonoBehaviour
{
    public Rigidbody rb;
    public SpriteRenderer shadowRenderer;

    StemScript stemScript;

    public void Spawn(StemScript stemScript) {
        this.stemScript = stemScript;
        transform.localPosition = stemScript.transform.position + stemScript.transform.up * .22f;
    }
    public void Pick() {
        stemScript.Pick();
        rb.isKinematic = false;
    }

    // Update is called once per frame
    void Update()
    {
        Util.UpdateShadow(gameObject, shadowRenderer);
    }
}
