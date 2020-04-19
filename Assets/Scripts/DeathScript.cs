using Assets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DeathScript : MonoBehaviour
{
    public CanvasGroup fadeCanvasGroup;
    public TextMeshProUGUI deathTMP;
    public GameObject gel, neck;

    bool dead;
    int frames;
    Camera cam;
    Vector3 originalPos, targetPos;
    Quaternion originalRot, targetRot;

    public void Die() {
        Time.timeScale = 0;
        dead = true;
        cam = Camera.main;
        cam.transform.parent = transform.parent;
        originalPos = cam.transform.localPosition;
        targetPos = gel.transform.position + gel.transform.forward * 1.25f;
        originalRot = cam.transform.localRotation;
        cam.transform.localPosition = targetPos;
        cam.transform.LookAt(gel.transform.position + new Vector3(0, .25f, 0));
        targetRot = cam.transform.localRotation;
        cam.transform.localPosition = originalPos;
        cam.transform.localRotation = originalRot;
    }

    void Update()
    {
        if (!dead) {
            fadeCanvasGroup.alpha -= .01f;
            if (!PlayerScript.CAN_INPUT) {
                float neckLerpFactor = Mathf.InverseLerp(3f, -4.5f, gel.transform.localPosition.x);
                float neckAngle = Mathf.Lerp(0, 28, neckLerpFactor);
                neck.transform.localRotation = Quaternion.Euler(neckAngle, 0, 0);
            }
            return;
        } else {
            frames++;
            float t = Mathf.Clamp01(frames / 120f);
            t = EasingFunction.EaseInOutQuad(0, 1, t);
            cam.transform.localPosition = Vector3.Lerp(originalPos, targetPos, t);
            cam.transform.localRotation = Quaternion.Lerp(originalRot, targetRot, t);
        }
    }
}
