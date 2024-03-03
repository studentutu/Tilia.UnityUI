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
        private static RaycastHit[] st_arrayOfRaycasts = new RaycastHit[10];
        private static RaycastHit2D[] st_arrayOfRaycasts2d = new RaycastHit2D[10];
        protected Canvas currentCanvas = null;
        protected RectTransform _rectTransform = null;
        protected const float UI_CONTROL_OFFSET = 0.00001f;
        private Vector3[] _corners = new Vector3[4];

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
                _rectTransform = currentCanvas.GetComponent<RectTransform>();
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
            if (CanvasToUse == null || eventCamera == null || !eventCamera.enabled)
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

            // Optimization: this canvas will only run only on a single VRTK_4_Input module.
            if (CurrentPointer != null && CurrentPointer.PointerActive() && IsIntersectedByRay(ray))
            {
                Raycast(CanvasToUse, eventCamera, eventData, ray, ref s_RaycastResults);
                AppendToListAllCurrentRaycasts(ref resultAppendList, ref s_RaycastResults);
                s_RaycastResults.Clear();
            }
        }

        private bool IsIntersectedByRay(Ray ray)
        {
            // Get the corners of the RectTransform in world space
            _rectTransform.GetWorldCorners(_corners);

            var bounds = new Bounds(_rectTransform.position, Vector3.zero);
            for (int i = 0; i < _corners.Length; i++)
            {
                bounds.Encapsulate(_corners[i]);
            }

            bounds.Expand(0.2f);

            return bounds.IntersectRay(ray) || bounds.Contains(ray.origin);
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
            if (CanvasToUse.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                float maxDistance = Vector3.Distance(ray.origin, CanvasToUse.transform.position) + 10f;
                int allraycasts = -1;

                if (blockingObjects == BlockingObjects.ThreeD || blockingObjects == BlockingObjects.All)
                {
                    allraycasts = Physics.RaycastNonAlloc(ray, st_arrayOfRaycasts, maxDistance, m_BlockingMask);

                    for (int i = 0; i < allraycasts; i++)
                    {
                        RaycastHit hit = st_arrayOfRaycasts[i];
                        if (hit.collider != null && !VRTK4_PlayerObject.IsPlayerObject(hit.collider.gameObject))
                        {
                            hitDistance = Mathf.Min(hit.distance, hitDistance);
                        }
                    }

                    ClearArrNonAlloc3D();
                }

                if (blockingObjects == BlockingObjects.TwoD || blockingObjects == BlockingObjects.All)
                {
                    allraycasts = Physics2D.RaycastNonAlloc(ray.origin, ray.direction, st_arrayOfRaycasts2d,
                        maxDistance,
                        m_BlockingMask);

                    for (int i = 0; i < allraycasts; i++)
                    {
                        RaycastHit2D hit = st_arrayOfRaycasts2d[i];
                        if (hit.collider != null && !VRTK4_PlayerObject.IsPlayerObject(hit.collider.gameObject))
                        {
                            hitDistance = Mathf.Min(hit.fraction * maxDistance, hitDistance);
                        }
                    }

                    ClearArrNonAlloc2D();
                }
            }

            return hitDistance;
        }

        private static void ClearArrNonAlloc3D()
        {
            for (int i = 0; i < st_arrayOfRaycasts.Length; i++)
            {
                st_arrayOfRaycasts[i] = default;
            }
        }

        private static void ClearArrNonAlloc2D()
        {
            for (int i = 0; i < st_arrayOfRaycasts2d.Length; i++)
            {
                st_arrayOfRaycasts2d[i] = default;
            }
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
            if (canvasIn == null || !canvasIn.enabled || !canvasIn.gameObject.activeInHierarchy)
                return;

            // bool checkRaycastGraphic = false;
            float hitDistance = GetHitDistance(ray, VRTK4_UIPointer.GetPointerLength(eventData.pointerId));
            IList<Graphic> canvasGraphics = null;
#if UNITY_2020_3_OR_NEWER
            canvasGraphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvasIn);
#else
            canvasGraphics = GraphicRegistry.GetGraphicsForCanvas(canvasIn);
#endif
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
#if UNITY_2020_3_OR_NEWER
                if (!RectTransformUtility.RectangleContainsScreenPoint(
                        graphic.rectTransform,
                        pointerPosition,
                        eventCameraIn, graphic.raycastPadding))
                {
                    continue;
                }

#else
                if (!RectTransformUtility.RectangleContainsScreenPoint(
                    graphic.rectTransform,
                    pointerPosition,
                    eventCameraIn))
                {
                    continue;
                }
#endif

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
                        depth = graphic.depth + canvasIn.sortingLayerID * 1000 + canvasIn.sortingOrder,
                        sortingLayer = canvasIn.sortingLayerID,
                        sortingOrder = canvasIn.sortingOrder,
                    };
                    VRTK4_SharedMethods.AddListValue(helperList, result);
                }
            }

            if (helperList.Count > 1)
                helperList.Sort(ComparisonInversed);
        }

        private static int ComparisonInversed(RaycastResult g1, RaycastResult g2)
        {
            return g2.depth.CompareTo(g1.depth);
        }
    }
}