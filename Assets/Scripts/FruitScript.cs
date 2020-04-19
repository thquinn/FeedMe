using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitScript : MonoBehaviour
{
    static float VIBRATION_INTENSITY = .04f;

    public GameObject popEffectPrefab;
    public Mesh[] colorMeshes;

    public Rigidbody rb;
    public MeshFilter meshFilter;
    public SpriteRenderer shadowRenderer;
    public Light glowLight;
    public GameObject meshObject;

    public FruitColor color;
    int frames;
    StemScript stemScript;
    float amountLeft = 1;
    Vector3 initialMeshPosition;
    float vibration;

    public void Spawn(StemScript stemScript, FruitColor color) {
        this.stemScript = stemScript;
        transform.parent = stemScript.transform;
        transform.localPosition = new Vector3(0, .8f, 0);
        transform.localRotation = Quaternion.identity;
        this.color = color;
        int colorIndex = (int)color;
        if (colorIndex > 1) {
            meshFilter.mesh = colorMeshes[colorIndex - 1];
            glowLight.color = Color.Lerp(color.ToUnityColor(), Color.white, .5f);
        }

    }
    public void Pick() {
        if (stemScript != null) {
            stemScript.Pick();
            stemScript = null;
        }
        transform.parent = null;
        transform.localScale = Vector3.one;
    }
    public bool Eat(float amount) {
        rb.velocity *= .5f;
        rb.angularVelocity *= .5f;
        amountLeft -= amount;
        vibration = Mathf.Lerp(vibration, Mathf.Pow(1 - amountLeft, 2) * VIBRATION_INTENSITY, .25f);
        if (amountLeft <= 0) {
            Instantiate(popEffectPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
            return true;
        }
        return false;
    }

    void Start() {
        initialMeshPosition = meshObject.transform.localPosition;
        transform.localScale = Vector3.zero;
    }
    void Update()
    {
        frames++;
        if (stemScript != null) {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one / transform.parent.localScale.x, .1f);
        }
        Util.UpdateShadow(gameObject, shadowRenderer);
        Vector3 localPosition = initialMeshPosition;
        localPosition.x += Mathf.Cos(frames * 2f) * vibration;
        meshObject.transform.localPosition = localPosition;
        vibration *= .98f;
    }
}

public enum FruitColor : int {
    None, Red, Purple, Green
}

public static class FruitExtensions {
    static Color[] UNITY_COLORS = new Color[] { Color.black, Color.red, new Color(.5f, 0, .5f), new Color(0, .5f, 0) };
    public static Color ToUnityColor(this FruitColor fruitColor) {
        return UNITY_COLORS[(int)fruitColor];
    }
}