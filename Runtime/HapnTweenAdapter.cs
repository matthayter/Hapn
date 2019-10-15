using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HapnTweenAdapter : MonoBehaviour
{
    [System.Serializable]
    public class HapnVector2Target : UnityEvent<Vector2> { }

    public HapnVector2Target toChange;
    public Vector2 startPos;
    public Vector2 endPos;
    public float duration;
    public AnimationCurve curve;
}
