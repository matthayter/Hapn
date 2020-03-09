using UnityEngine;

namespace Hapn.Utils {
    public static class VectorExtensions {
        public static void SetX(this Vector3 v, float x) {
            v.Set(x, v.y, v.z);
        }
        public static void SetY(this Vector3 v, float y) {
            v.Set(v.x, y, v.z);
        }
        public static void SetZ(this Vector3 v, float z) {
            v.Set(v.x, v.y, z);
        }

        public static void SetZPos(this Transform t, float z) {
            t.position = new Vector3(t.position.x, t.position.y, z);
        }

        public static void SetLocalXPos(this Transform t, float x) {
            Vector3 current = t.localPosition;
            t.localPosition = new Vector3(x, current.y, current.z);
        }
        public static void SetLocalYPos(this Transform t, float y) {
            Vector3 current = t.localPosition;
            t.localPosition = new Vector3(current.x, y, current.z);
        }
        public static void SetLocalZPos(this Transform t, float z) {
            Vector3 current = t.localPosition;
            t.localPosition = new Vector3(current.x, current.y, z);
        }
    }
}