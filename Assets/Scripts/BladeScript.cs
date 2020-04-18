using Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeScript : MonoBehaviour
{
    static int CYCLE_FRAMES = 240;

    public MeshRenderer meshRenderer;
    public int cycleOffset;

    int frames;

    // Start is called before the first frame update
    void Start()
    {
        meshRenderer.material = Instantiate(meshRenderer.material);
        transform.localRotation = Quaternion.Euler(0, 0, -90);
        frames = cycleOffset;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale == 0) {
            return;
        }
        frames++;
        float t = (frames % CYCLE_FRAMES) / (float)CYCLE_FRAMES;
        float angle;
        if (t < .5f) {
            angle = EasingFunction.EaseInOutQuad(-90, 90, t * 2);
        } else {
            angle = EasingFunction.EaseInOutQuad(90, -90, (t - .5f) * 2);
        }
        transform.localRotation = Quaternion.Euler(0, 0, angle);
        Color tint = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(75, 30, Mathf.Abs(angle)));
        meshRenderer.material.SetColor("_Color", tint);
    }
}
