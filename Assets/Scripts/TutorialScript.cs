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
            Vector3 localPosition = transform.localPosition;
            localPosition.x *= Camera.main.aspect / (16 / 9f);
            transform.localPosition = localPosition;
        }
    }
    void Update()
    {
        frames++;
        if (frames > 1200) {
            Color c = image.color;
            c.a -= .005f;
            if (c.a <= 0) {
                Destroy(gameObject);
                return;
            }
            image.color = c;
        }
    }
}
