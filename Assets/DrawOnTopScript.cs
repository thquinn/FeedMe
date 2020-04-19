using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawOnTopScript : MonoBehaviour
{
    public Image[] images;

    void Start() {
        foreach (Image image in images) {
            image.materialForRendering.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
        }
    }
}
