using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tilia.VRTKUI
{
    /// <summary>
    /// Add this to VRTK 4 Event System gameobject to Receive Unity event trigger events on your 3D objects
    /// </summary>
    public class VRTK4_3DGraphicRaycaster : BaseRaycaster
    {
        public static VRTK4_UIPointer CurrentPointer;
        private static RaycastHit[] st_arrayOfRaycasts = new RaycastHit[10];

        [SerializeField] private LayerMask m_BlockingMask;

        // Use a static to prevent list reallocation. We only need one of these globally (single main thread), and only to hold temporary data
        [NonSerialized] private static List<RaycastResult> s_RaycastResults = new List<RaycastResult>();

        private Camera _camera = null;

        public override Camera eventCamera
        {
            get
            {
                if (_camera == null || !_camera.enabled)
                {
                    _camera = Camera.main;
                }

                return _camera;
            }
        }


        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (eventCamera == null || !eventCamera.enabled || VRTK4_EventSystem.Instance == null)
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
            Raycast(eventCamera, eventData, ray, ref s_RaycastResults);
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


        /// <summary>
        /// Changes - ignore displays.
        /// </summary>
        /// <param name="eventCameraIn"></param>
        /// <param name="eventData"></param>
        /// <param name="ray"></param>
        /// <param name="helperList"></param>
        protected virtual void Raycast(Camera eventCameraIn, PointerEventData eventData, Ray ray,
            ref List<RaycastResult> helperList)
        {
            RaycastHit hitResult = default;
            float hitDistance = VRTK4_UIPointer.GetPointerLength(eventData.pointerId);
            bool isSuccesfullRaycasted = TryGetHitDistance(ray, ref hitDistance, out hitResult);
            ClearArrNonAlloc3D();
            if (isSuccesfullRaycasted)
            {
                Transform graphicTransform = hitResult.transform;
                Vector3 graphicForward = graphicTransform.forward;
                float distance = Vector3.Dot(graphicForward, graphicTransform.position - ray.origin) /
                                 Vector3.Dot(graphicForward, ray.direction);

                if (distance < 0)
                {
                    return;
                }

                Vector3 position = ray.GetPoint(distance);
                Vector2 pointerPosition = eventCameraIn.WorldToScreenPoint(position);
                var result = new RaycastResult
                {
                    gameObject = hitResult.collider.gameObject,
                    module = this,
                    distance = hitResult.distance,
                    worldPosition = hitResult.point,
                    worldNormal = hitResult.normal,
                    screenPosition = pointerPosition,
                    depth = 0,
                    sortingLayer = 0,
                    sortingOrder = 7
                };
                VRTK4_SharedMethods.AddListValue(helperList, result);
                helperList.Sort(ComparisonInversedDistance);
            }
        }

        private static int ComparisonInversedDistance(RaycastResult g1, RaycastResult g2)
        {
            if (g2.sortingOrder == 7)
            {
                return g2.distance.CompareTo(g1.distance);
            }

            if (g1.sortingOrder == 7)
            {
                return g2.distance.CompareTo(g1.distance);
            }

            return 0;
        }

        // Skip check for near/far plane!
        private bool TryGetHitDistance(Ray ray, ref float hitDistance, out RaycastHit hitResult)
        {
            float maxDistance = hitDistance;
            int allraycasts = -1;

            allraycasts = Physics.RaycastNonAlloc(ray, st_arrayOfRaycasts, maxDistance, m_BlockingMask);
            if (allraycasts > 0)
            {
                if (allraycasts > 1)
                {
                    System.Array.Sort(st_arrayOfRaycasts, 0, allraycasts, VRTK3DRaycastHitComparer.instance);
                }

                RaycastHit hit;
                for (int i = 0; i < allraycasts && i < st_arrayOfRaycasts.Length; i++)
                {
                    hit = st_arrayOfRaycasts[i];
                    if (hit.collider != null && !VRTK4_PlayerObject.IsPlayerObject(hit.collider.gameObject))
                    {
                        hitDistance = hit.distance;
                        hitResult = hit;
                        return true;
                    }
                }
            }

            hitResult = default;
            return false;
        }

        private static void ClearArrNonAlloc3D()
        {
            for (int i = 0; i < st_arrayOfRaycasts.Length; i++)
            {
                st_arrayOfRaycasts[i] = default;
            }
        }

        private class VRTK3DRaycastHitComparer : IComparer<RaycastHit>
        {
            public static VRTK3DRaycastHitComparer instance = new VRTK3DRaycastHitComparer();

            public int Compare(RaycastHit x, RaycastHit y)
            {
                return x.distance.CompareTo(y.distance);
            }
        }
    }
}