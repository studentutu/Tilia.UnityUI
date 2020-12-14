namespace Tilia.VRTKUI
{
    using UnityEngine;
    
    /// <summary>
    /// Responsible for propagating input to VRTK4_InputModule
    /// </summary>
    [DisallowMultipleComponent]
    public class VRTK_UIPointerAutoActivator : MonoBehaviour
    {
        private VRTK4_UICanvas parentCanvas;
        
        protected virtual void OnTriggerEnter(Collider collider)
        {
            VRTK4_PlayerObject colliderCheck = collider.GetComponentInParent<VRTK4_PlayerObject>();
            VRTK4_UIPointer pointerCheck = colliderCheck == null? null: colliderCheck.GetPointer();
            if (pointerCheck != null && colliderCheck != null 
                                     && colliderCheck.objectType == VRTK4_PlayerObject.ObjectTypes.Pointer)
            {
                if (parentCanvas == null)
                {
                    parentCanvas = gameObject.GetComponentInParent<VRTK4_UICanvas>();
                }
                pointerCheck.autoActivatingCanvas = parentCanvas == null? null : parentCanvas.gameObject;
            }
        }

        protected virtual void OnTriggerExit(Collider collider)
        {
            VRTK4_PlayerObject colliderCheck = collider.GetComponentInParent<VRTK4_PlayerObject>();
            VRTK4_UIPointer pointerCheck = colliderCheck == null? null: colliderCheck.GetPointer();
            if (pointerCheck != null && 
                pointerCheck.autoActivatingCanvas == gameObject && 
                colliderCheck.objectType == VRTK4_PlayerObject.ObjectTypes.Pointer)
            {
                pointerCheck.autoActivatingCanvas = null;
            }
        }
    }
}