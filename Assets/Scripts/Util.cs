using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts {
    public class Util {
        static LayerMask layerMaskDefault;
        static float SHADOW_DISTANCE = 2;

        public static float CorrectDegreeDiscrepancy(float oldTheta, float newTheta) {
            if (oldTheta - newTheta < -180) {
                return oldTheta + 360;
            }
            if (oldTheta - newTheta > 180) {
                return oldTheta - 360;
            }
            return oldTheta;
        }

        public static void UpdateShadow(GameObject go, SpriteRenderer shadowRenderer) {
            if (layerMaskDefault == 0) {
                layerMaskDefault = LayerMask.GetMask("Default");
            }
            RaycastHit hitInfo;
            Physics.Raycast(go.transform.position, Vector3.down, out hitInfo, SHADOW_DISTANCE, layerMaskDefault);
            if (hitInfo.collider) {
                shadowRenderer.enabled = true;
                Vector3 position = shadowRenderer.transform.position;
                position.y = hitInfo.point.y + .01f;
                shadowRenderer.transform.position = position;
                Color c = shadowRenderer.color;
                c.a = Mathf.Lerp(.1f, 0, hitInfo.distance / SHADOW_DISTANCE);
                shadowRenderer.color = c;
            } else {
                shadowRenderer.enabled = false;
            }
        }

        public static int Mod(int x, int m) {
            return (x % m + m) % m;
        }

        public static string[] SplitNewLines(string text) {
            return text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        }
    }

    public static class ArrayExtensions {
        public static T[] Shuffle<T>(this T[] array) {
            int n = array.Length;
            for (int i = 0; i < n; i++) {
                int r = i + UnityEngine.Random.Range(0, n - i);
                T t = array[r];
                array[r] = array[i];
                array[i] = t;
            }
            return array;
        }
    }
}
