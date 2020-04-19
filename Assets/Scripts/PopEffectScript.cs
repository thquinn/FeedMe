using Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopEffectScript : MonoBehaviour {
    public AudioSource sfxPop;

    Camera cam;
    GameObject[] lines;
    int frames;

    void Start() {
        cam = Camera.main;
        lines = new GameObject[8];
        for (int i = 0; i < lines.Length; i++) {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.parent = transform;
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.Euler(0, 0, 360f / lines.Length * i);
            quad.transform.localScale = new Vector3(0, .025f, 1);
            lines[i] = quad;
        }
        sfxPop.Play();
    }

    // Update is called once per frame
    void Update() {
        frames++;
        float t = EasingFunction.EaseOutQuad(0, 1, frames / 13f);
        if (t >= 1) {
            Destroy(gameObject);
            return;
        }
        Vector3 direction = transform.position - cam.transform.position;
        transform.rotation = Quaternion.LookRotation(direction);
        // Update each line.
        float d = t * .3f;
        float xScale = t < .5f ? Mathf.Lerp(0, .066f, t * 2) : Mathf.Lerp(.066f, 0, (t - .5f) * 2);
        Vector3 localScale = new Vector3(xScale, Mathf.Min(xScale, .025f), 1);
        for (int i = 0; i < lines.Length; i++) {
            float radians = 2 * Mathf.PI / lines.Length * i;
            lines[i].transform.localPosition = new Vector3(d * Mathf.Cos(radians), d * Mathf.Sin(radians), 0);
            lines[i].transform.localScale = localScale;
        }
    }
}
