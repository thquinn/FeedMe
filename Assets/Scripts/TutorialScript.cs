using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialScript : MonoBehaviour
{
    public Image image;

    int frames;

    void Start() {
        if (Camera.main.aspect < 16 / 9f) {
            float xRes = 600 * Camera.main.aspect;
            float missingX = 1067 - xRes;
            Vector3 localPosition = transform.localPosition;
            localPosition.x += missingX / 2;
            transform.localPosition = localPosition;
        }
        if (!PlayerScript.CAN_INPUT) {
            AdjustAlpha(-1);
        }
    }
    void Update()
    {
        frames++;
        if (frames > 1500) {
            AdjustAlpha(-.005f);
            if (image.color.a <= 0) {
                Destroy(gameObject);
                return;
            }
        } else if (frames > 360) {
            AdjustAlpha(.05f);
        }
    }

    void AdjustAlpha(float f) {
        Color c = image.color;
        c.a = Mathf.Clamp01(c.a + f);
        image.color = c;
    }
}
;