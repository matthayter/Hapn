using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class HapnAnchorTweenAdapter : MonoBehaviour
{
    public RectTransform target;
    [FormerlySerializedAs("startAnchors")]
    public Rect startOrActiveAnchors;
    [FormerlySerializedAs("endAnchors")]
    public Rect endOrInactiveAnchors;
    public float duration;
    public AnimationCurve curve;

    public void SetAnchors(Rect anchors) {
        target.anchorMin = anchors.min;
        target.anchorMax = anchors.max;
    }
}
