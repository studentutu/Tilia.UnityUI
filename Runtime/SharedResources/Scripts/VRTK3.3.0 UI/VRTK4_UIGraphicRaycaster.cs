namespace Tilia.VRTKUI
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// This script allows VRTK to interact cleanly with Unity Canvases.
    /// It is mostly a duplicate of Unity's default GraphicsRaycaster:
    /// https://bitbucket.org/Unity-Technologies/ui/src/0155c39e05ca5d7dcc97d9974256ef83bc122586/UnityEngine.UI/UI/Core/GraphicRaycaster.c
    /// However, it allows for graphics to be hit when they are not in view of a camera.
    /// Note: Not intended for direct use. VRTK will intelligently replace the default GraphicsRaycaster
    ///   on canvases with this raycaster.
    /// </summary>>
    [DisallowMultipleComponent]
    public class VRTK4_UIGraphicRaycaster : GraphicRaycaster
    {
        public static VRTK4_UIPointer CurrentPointer;
        protected Canvas currentCanvas = null;
        protected const float UI_CONTROL_OFFSET = 0.00001f;

        // Use a static to prevent list reallocation. We only need one of these globally (single main thread), and only to hold temporary data
        [NonSerialized] private static List<RaycastResult> s_RaycastResults = new List<RaycastResult>();

        protected virtual Canvas CanvasToUse
        {
            get
            {
                if (currentCanvas != null)
                {
                    return currentCanvas;
                }

                currentCanvas = gameObject.GetComponent<Canvas>();
                return currentCanvas;
            }
        }

        /// <summary>
        /// Enables to raycast event from any point towards any VRTK4_Canvas (even when outside the view)
        /// </summary>
        /// <param name="eventData"> currentEventData (can get cleared in consequent stages) </param>
        /// <param name="resultAppendList"> actual hits on UI elements under canvases</param>
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (CanvasToUse == null || eventCamera == null)
            {
                return;
            }

            Ray ray;
            if (CurrentPointer != null)
            {
                ray = new Ray(CurrentPointer.GetOriginPosition(), CurrentPointer.GetOriginForward());
            }
            else
            {
                ray = new Ray(eventData.pointerCurrentRaycast.worldPosition,
                    eventData.pointerCurrentRaycast.worldNormal);
            }

            s_RaycastResults.Clear();
            Raycast(CanvasToUse, eventCamera, eventData, ray, ref s_RaycastResults);
            AppendToListAllCurrentRaycasts(ref resultAppendList, ref s_RaycastResults);
            s_RaycastResults.Clear();
        }

        /// <summary>
        /// Fills the list of cast rays
        /// </summary>
        protected virtual void AppendToListAllCurrentRaycasts(ref List<RaycastResult> resultAppendList,
            ref List<RaycastResult> raycastResults)
        {
            int totalCount = raycastResults.Count;
            for (int index = 0; index < totalCount; index++)
            {
                VRTK4_SharedMethods.AddListValue(resultAppendList, raycastResults[index]);
            }
        }

        // Skip check for near/far plane!
        protected virtual float GetHitDistance(Ray ray, float hitDistance)
        {
            if (CanvasToUse.renderMode != RenderMode.ScreenSpaceOverlay && blockingObjects != BlockingObjects.None)
            {
                float maxDistance = Vector3.Distance(ray.origin, CanvasToUse.transform.position) + 10f;

                if (blockingObjects == BlockingObjects.ThreeD || blockingObjects == BlockingObjects.All)
                {
                    RaycastHit hit;
                    Physics.Raycast(ray, out hit, maxDistance, m_BlockingMask);
                    if (hit.collider != null && !VRTK4_PlayerObject.IsPlayerObject(hit.collider.gameObject))
                    {
                        hitDistance = hit.distance;
                    }
                }

                if (blockingObjects == BlockingObjects.TwoD || blockingObjects == BlockingObjects.All)
                {
                    RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, maxDistance);

                    if (hit.collider != null && !VRTK4_PlayerObject.IsPlayerObject(hit.collider.gameObject))
                    {
                        hitDistance = hit.fraction * maxDistance;
                    }
                }
            }

            return hitDistance;
        }

        /// <summary>
        /// Changes - ignore displays. Improved Performance : no need to sort graphics only, we can sort all raycast once.
        /// </summary>
        /// <param name="canvasIn"></param>
        /// <param name="eventCameraIn"></param>
        /// <param name="eventData"></param>
        /// <param name="ray"></param>
        /// <param name="helperList"></param>
        protected virtual void Raycast(Canvas canvasIn, Camera eventCameraIn, PointerEventData eventData, Ray ray,
            ref List<RaycastResult> helperList)
        {
            float hitDistance = GetHitDistance(ray, VRTK4_UIPointer.GetPointerLength(eventData.pointerId));
            IList<Graphic> canvasGraphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvasIn);
            int totalCount = canvasGraphics.Count;
            for (int i = 0; i < totalCount; ++i)
            {
                Graphic graphic = canvasGraphics[i];

                if (graphic.depth == -1 || !graphic.raycastTarget || graphic.canvasRenderer.cull)
                {
                    continue;
                }

                Transform graphicTransform = graphic.transform;
                Vector3 graphicForward = graphicTransform.forward;
                float distance = Vector3.Dot(graphicForward, graphicTransform.position - ray.origin) /
                                 Vector3.Dot(graphicForward, ray.direction);

                if (distance < 0)
                {
                    continue;
                }

                //Prevents "flickering hover" on items near canvas center.
                if ((distance - UI_CONTROL_OFFSET) > hitDistance)
                {
                    continue;
                }

                Vector3 position = ray.GetPoint(distance);
                Vector2 pointerPosition = eventCameraIn.WorldToScreenPoint(position);

                if (!RectTransformUtility.RectangleContainsScreenPoint(
                    graphic.rectTransform,
                    pointerPosition,
                    eventCameraIn, graphic.raycastPadding))
                {
                    continue;
                }

                if (graphic.Raycast(pointerPosition, eventCameraIn))
                {
                    RaycastResult result = new RaycastResult()
                    {
                        gameObject = graphic.gameObject,
                        module = this,
                        distance = distance,
                        worldPosition = position,
                        worldNormal = -graphicForward,
                        screenPosition = pointerPosition,
                        depth = graphic.depth,
                        sortingLayer = canvasIn.sortingLayerID,
                        sortingOrder = canvasIn.sortingOrder,
                    };
                    VRTK4_SharedMethods.AddListValue(helperList, result);
                }
            }

            helperList.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));
        }
    }
}