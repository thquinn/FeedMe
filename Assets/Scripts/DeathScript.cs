using Assets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathScript : MonoBehaviour {
    public CanvasGroup fadeCanvasGroup;
    public TextMeshProUGUI deathTMP;
    public GameObject gel, neck;
    public MusicScript musicScript;

    float time;
    DeathReason reason;
    int frames;
    Camera cam;
    Vector3 originalPos, targetPos;
    Quaternion originalRot, targetRot;

    public void PlayerDie(DeathReason deathReason) {
        if (reason != DeathReason.None) {
            return;
        }
        reason = deathReason;
        SetDeathText();
    }
    public void GelDie(DeathReason deathReason) {
        if (reason != DeathReason.None) {
            return;
        }
        Time.timeScale = 0;
        reason = deathReason;
        SetDeathText();
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

    void Start() {
        fadeCanvasGroup.alpha = 1;
    }
    void Update() {
        time += Time.deltaTime;
        if (deathTMP.color.a >= 1 && Input.GetButtonDown("Restart")) {
            Time.timeScale = 1;
            AudioListener.volume = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        if (reason != DeathReason.None) {
            AudioListener.volume -= .0025f;
            if (AudioListener.volume <= 0) {
                musicScript.Stop();
            }
            frames++;
        }

        if (reason == DeathReason.None) {
            fadeCanvasGroup.alpha -= .01f;
            if (!PlayerScript.CAN_INPUT) {
                float neckLerpFactor = Mathf.InverseLerp(3f, -4.5f, gel.transform.localPosition.x);
                float neckAngle = Mathf.Lerp(0, 28, neckLerpFactor);
                neck.transform.localRotation = Quaternion.Euler(neckAngle, 0, 0);
            }
            return;
        } else if (IsGelReason()) {
            float t = Mathf.Clamp01(frames / 120f);
            t = EasingFunction.EaseInOutQuad(0, 1, t);
            cam.transform.localPosition = Vector3.Lerp(originalPos, targetPos, t);
            cam.transform.localRotation = Quaternion.Lerp(originalRot, targetRot, t);
            if (frames > 180) {
                fadeCanvasGroup.alpha += .01f;
            }
            if (frames == 300) {
                deathTMP.color = Color.white;
            }
        } else if (reason == DeathReason.PlayerFall) {
            fadeCanvasGroup.alpha += .05f;
            if (frames == 60) {
                deathTMP.color = Color.white;
            }
        } else { // player hazard death
            if (frames == 1) {
                fadeCanvasGroup.alpha = 1;
            } else if (frames > 30) {
                Color c = deathTMP.color;
                c.a += .5f;
                deathTMP.color = c;
            }
        }
    }

    bool IsGelReason() {
        return (int)reason >= 3;
    }
    static string[] TRY_AGAIN_STRINGS = new string[] {
        "Press Enter to try again.",
        "Press Enter to give it another go.",
        "Press Enter to do better.",
        "Press Enter to make it right.",
    };
    void SetDeathText() {
        int totalSeconds = Mathf.FloorToInt(time);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        string timeString = minutes == 0 ? string.Format("{0} {1}", seconds, seconds == 1 ? "second" : "seconds") :
                            seconds == 0 ? string.Format("{0} {1}", minutes, minutes == 1 ? "minute" : "minutes") :
                                           string.Format("{0} {1} and {2} {3}", minutes, minutes == 1 ? "minute" : "minutes", seconds, seconds == 1 ? "second" : "seconds" );
        if (IsGelReason()) {
            timeString = string.Format("You kept him alive for {0}.", timeString);
        } else {
            timeString = string.Format("You kept your new friend alive for {0}.", timeString);
        }
        deathTMP.text = string.Format("{0}\n{1}\n{2}", DeathReasonHelper.ReasonString(reason), timeString, TRY_AGAIN_STRINGS[Random.Range(0, TRY_AGAIN_STRINGS.Length)]);
    }
}

public enum DeathReason : int {
    None, PlayerFall, PlayerHazard, GelFall, GelStarve, GelHazard
}

public static class DeathReasonHelper {
    static string[] REASON_STRINGS = new string[] {
        "I'm... not quite sure how you died.",
        "You fell to your death.",
        "You walked into danger.",
        "You let your new friend fall into a pit.",
        "You let your new friend starve.",
        "You led your new friend into danger.",
    };
    public static string ReasonString(DeathReason reason) {
        return REASON_STRINGS[(int)reason];
    }
}