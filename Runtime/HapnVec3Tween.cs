using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HapnVec3Tween : MonoBehaviour
{
    [System.Serializable]
    public class HapnVector3Target : UnityEvent<Vector3> { }

    public HapnVector3Target toChange;
    public Vector3 startPos;
    public Vector3 endPos;
    public float duration;
    public AnimationCurve curve;
}
