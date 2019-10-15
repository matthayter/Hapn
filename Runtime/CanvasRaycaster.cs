//Attach this script to your Canvas GameObject.
//Also attach a GraphicsRaycaster component to your canvas by clicking the Add Component button in the Inspector window.
//Also make sure you have an EventSystem in your hierarchy.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Hapn {
    // Not Thread-safe
    [RequireComponent(typeof(Canvas))]
    public class CanvasRaycaster : MonoBehaviour
    {
        GraphicRaycaster m_Raycaster;
        PointerEventData m_PointerEventData;
        [SerializeField] EventSystem m_EventSystem = null;
        List<RaycastResult> m_resultsCache = new List<RaycastResult>(20);

        void Start()
        {
            //Fetch the Raycaster from the GameObject (the Canvas)
            m_Raycaster = GetComponent<GraphicRaycaster>();
            // Reuse the pointer data class to avoid extra alloc
            m_PointerEventData = new PointerEventData(m_EventSystem);
            if (m_EventSystem == null) Debug.LogError("CanvasRaycaster: Missing event system");
        }

        // Don't hold on to the returned list - this class will re-use it.
        public List<RaycastResult> RaycastMouseUnsafeReturn() {
            m_resultsCache.Clear();
            if (m_EventSystem == null) {
                Debug.LogError("CanvasRaycaster: Missing event system");
                return m_resultsCache;
            }
            // Set the Pointer Event Position to that of the mouse position
            m_PointerEventData.position = Input.mousePosition;

            // Raycast using the Graphics Raycaster and mouse click position
            m_Raycaster.Raycast(m_PointerEventData, m_resultsCache);
            return m_resultsCache;
        }
    }
}