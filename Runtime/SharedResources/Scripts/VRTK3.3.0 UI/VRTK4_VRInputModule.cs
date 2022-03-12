namespace Tilia.VRTKUI
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using System.Collections.Generic;

    /// <summary>
    /// Main Processor of all of the input. By default supports standard Unity input + UI Pointers
    /// </summary>
    public class VRTK4_VRInputModule : PointerInputModule
    {
        public List<VRTK4_UIPointer> Pointers = new List<VRTK4_UIPointer>();
        private List<RaycastResult> raycasts = new List<RaycastResult>();

        private Dictionary<VRTK4_UIPointer, List<RaycastResult>> PointersWithRaycasts =
            new Dictionary<VRTK4_UIPointer, List<RaycastResult>>();

        private List<List<RaycastResult>> poolOfLists = new List<List<RaycastResult>>(2);


        //Needed to allow other regular (non-VR) InputModules in combination with VRTK_EventSystem
        public override bool IsModuleSupported()
        {
            return false;
        }

        public override void Process()
        {
            if (poolOfLists.Count == 0)
            {
                poolOfLists.Add(new List<RaycastResult>(30));
                poolOfLists.Add(new List<RaycastResult>(30));
            }

            while (poolOfLists.Count < Pointers.Count)
            {
                poolOfLists.Add(new List<RaycastResult>(30));
            }

            for (int i = 0; i < poolOfLists.Count; i++)
            {
                poolOfLists[i].Clear();
            }

            PointersWithRaycasts.Clear();
            for (int i = 0; i < Pointers.Count; i++)
            {
                PointersWithRaycasts.Add(Pointers[i], poolOfLists[i]);
            }

            for (int i = 0; i < Pointers.Count; i++)
            {
                VRTK4_UIPointer pointer = Pointers[i];
                if (pointer != null && pointer.gameObject.activeInHierarchy && pointer.enabled)
                {
                    if (pointer.PointerActive() || pointer.autoActivatingCanvas != null)
                    {
                        PointersWithRaycasts[pointer].AddRange(CheckRaycasts(pointer));
                    }
                }
            }

            //Process Hover events
            Hover();
            // Process events
            for (int i = 0; i < Pointers.Count; i++)
            {
                VRTK4_UIPointer pointer = Pointers[i];
                if (pointer != null && pointer.gameObject.activeInHierarchy && pointer.enabled)
                {
                    Click(pointer, PointersWithRaycasts[pointer]);
                    Drag(pointer, PointersWithRaycasts[pointer]);
                    Scroll(pointer, PointersWithRaycasts[pointer]);
                }
            }

            for (int i = 0; i < poolOfLists.Count; i++)
            {
                poolOfLists[i].Clear();
            }

            PointersWithRaycasts.Clear();
        }

        protected virtual List<RaycastResult> CheckRaycasts(VRTK4_UIPointer pointer)
        {
            RaycastResult raycastResult = new RaycastResult();
            raycastResult.worldPosition = pointer.GetOriginPosition();
            raycastResult.worldNormal = pointer.GetOriginForward();

            pointer.pointerEventData.pointerCurrentRaycast = raycastResult;
            VRTK4_UIGraphicRaycaster.CurrentPointer = pointer;
            VRTK4_3DGraphicRaycaster.CurrentPointer = pointer;
            raycasts.Clear();
            eventSystem.RaycastAll(pointer.pointerEventData, raycasts);
            raycasts.Sort(ComparisonInversedDistance);
            if (raycasts.Count > 0)
            {
                var toUse = raycasts[0];
                var lastKnownPosition = pointer.pointerEventData.position;
                pointer.pointerEventData.position = toUse.screenPosition;
                pointer.pointerEventData.delta = pointer.pointerEventData.position - lastKnownPosition;
                pointer.pointerEventData.pointerCurrentRaycast = toUse;
            }

            VRTK4_UIGraphicRaycaster.CurrentPointer = null;
            VRTK4_3DGraphicRaycaster.CurrentPointer = null;
            return raycasts;
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

        protected virtual bool CheckTransformTree(Transform target, Transform source)
        {
            if (target == null)
            {
                return false;
            }

            if (target == source)
            {
                return true;
            }

            return CheckTransformTree(target.transform.parent, source);
        }

        protected virtual bool NoValidCollision(VRTK4_UIPointer pointer, List<RaycastResult> results)
        {
            return (results.Count == 0 || !CheckTransformTree(results[0].gameObject.transform,
                pointer.pointerEventData.pointerEnter.transform));
        }

        /// <summary>
        /// Can only be used after the processing!
        /// </summary>
        /// <param name="pointer"></param>
        /// <returns></returns>
        protected virtual bool IsHovering(VRTK4_UIPointer pointer)
        {
            for (int i = 0; i < pointer.pointerEventData.hovered.Count; i++)
            {
                GameObject hoveredObject = pointer.pointerEventData.hovered[i];
                if (pointer.pointerEventData.pointerEnter != null &&
                    hoveredObject != null &&
                    CheckTransformTree(hoveredObject.transform, pointer.pointerEventData.pointerEnter.transform))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sanity Check for Graphic Raycaster
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected virtual bool ValidElement(GameObject obj)
        {
            bool isValid = false;
            if (obj.layer != LayerMask.NameToLayer("UI"))
            {
                var anyHandler = obj.GetComponentInParent<IEventSystemHandler>();
                if (anyHandler != null)
                {
                    isValid = true;
                }
            }

            VRTK4_UIGraphicRaycaster canvasCheck = obj.GetComponentInParent<VRTK4_UIGraphicRaycaster>();
            return isValid || canvasCheck != null && canvasCheck.enabled;
        }

        private void CheckPointerHoverClick(VRTK4_UIPointer pointer, List<RaycastResult> results)
        {
            if (pointer.hoverDurationTimer > 0f)
            {
                pointer.hoverDurationTimer -= Time.deltaTime;
            }

            if (pointer.canClickOnHover && pointer.hoverDurationTimer <= 0f)
            {
                pointer.canClickOnHover = false;
                ClickOnDown(pointer, results, true);
            }
        }

        public enum UsageOfHover
        {
            OnEnter,
            OnExit
        }

        private class UsageHoverLast
        {
            public GameObject gameObject;
            public VRTK4_UIPointer pointerExit;
            public VRTK4_UIPointer pointerEnter;
            public UsageOfHover lastUsage;
        }

        private static void AddOrChange(Dictionary<GameObject, UsageHoverLast> dict, GameObject go,
            VRTK4_UIPointer pointer, UsageOfHover hoverUsage)
        {
            if (!dict.ContainsKey(go))
            {
                dict.Add(go, new UsageHoverLast());
            }

            dict[go].lastUsage = hoverUsage;
            dict[go].gameObject = go;
            if (hoverUsage == UsageOfHover.OnEnter)
            {
                dict[go].pointerEnter = pointer;
            }
            else
            {
                dict[go].pointerExit = pointer;
            }
        }

        private static readonly Dictionary<GameObject, UsageHoverLast> listPointerEnterExit =
            new Dictionary<GameObject, UsageHoverLast>(8);

        private static readonly Dictionary<VRTK4_UIPointer, GameObject> _alreadyEntered =
            new Dictionary<VRTK4_UIPointer, GameObject>();


        protected virtual void Hover()
        {
            listPointerEnterExit.Clear();
            _alreadyEntered.Clear();
            foreach (var item in PointersWithRaycasts)
            {
                VRTK4_UIPointer pointer = item.Key;

                if (pointer.pointerEventData.pointerEnter != null)
                {
                    _alreadyEntered.Add(pointer, pointer.pointerEventData.pointerEnter);
                    CheckPointerHoverClick(pointer, item.Value);
                }


                if (pointer.pointerEventData.pointerEnter != null &&
                    !ValidElement(pointer.pointerEventData.pointerEnter))
                {
                    AddOrChange(listPointerEnterExit, pointer.pointerEventData.pointerEnter, pointer,
                        UsageOfHover.OnExit);
                    pointer.OnUIPointerElementExit(pointer.SetUIPointerEvent(new RaycastResult(), null,
                        pointer.pointerEventData.pointerEnter));
                    pointer.pointerEventData.pointerEnter = null;
                    continue;
                }

                if (pointer.pointerEventData.pointerEnter != null &&
                    NoValidCollision(pointer, item.Value))
                {
                    pointer.pointerEventData.hovered.Remove(pointer.pointerEventData.pointerEnter);
                    AddOrChange(listPointerEnterExit, pointer.pointerEventData.pointerEnter, pointer,
                        UsageOfHover.OnExit);
                    pointer.OnUIPointerElementExit(pointer.SetUIPointerEvent(new RaycastResult(), null,
                        pointer.pointerEventData.pointerEnter));
                    pointer.pointerEventData.pointerEnter = null;
                }
            }

            foreach (var item in PointersWithRaycasts)
            {
                VRTK4_UIPointer pointer = item.Key;
                for (int i = 0; i < 1 && i < item.Value.Count; i++)
                {
                    RaycastResult result = item.Value[i];
                    if (!ValidElement(result.gameObject))
                    {
                        continue;
                    }

                    GameObject target = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(result.gameObject);
                    // listPointerEnter.Add(pointer.pointerEventData.pointerEnter);

                    target = (target == null ? result.gameObject : target);

                    if (target != null)
                    {
                        if (pointer.hoveringElement != null && pointer.hoveringElement != target)
                        {
                            AddOrChange(listPointerEnterExit, pointer.hoveringElement, pointer,
                                UsageOfHover.OnExit);
                            pointer.OnUIPointerElementExit(pointer.SetUIPointerEvent(result, null,
                                pointer.hoveringElement));
                        }

                        pointer.OnUIPointerElementEnter(pointer.SetUIPointerEvent(result, target,
                            pointer.hoveringElement));
                        pointer.hoveringElement = target;
                        pointer.pointerEventData.pointerCurrentRaycast = result;
                        pointer.pointerEventData.pointerEnter = target;
                        AddCheckForDuplicates(pointer.pointerEventData.hovered, pointer.pointerEventData.pointerEnter);
                        AddOrChange(listPointerEnterExit, pointer.pointerEventData.pointerEnter, pointer,
                            UsageOfHover.OnEnter);
                        break;
                    }

                    if (result.gameObject != pointer.hoveringElement)
                    {
                        AddOrChange(listPointerEnterExit, result.gameObject, pointer,
                            UsageOfHover.OnEnter);
                        pointer.OnUIPointerElementEnter(pointer.SetUIPointerEvent(result, result.gameObject,
                            pointer.hoveringElement));
                    }

                    pointer.hoveringElement = result.gameObject;
                }

                if (pointer.hoveringElement != null && item.Value.Count == 0)
                {
                    pointer.pointerEventData.hovered.Clear();
                    AddOrChange(listPointerEnterExit, pointer.hoveringElement, pointer,
                        UsageOfHover.OnExit);
                    pointer.OnUIPointerElementExit(pointer.SetUIPointerEvent(new RaycastResult(), null,
                        pointer.hoveringElement));
                    pointer.hoveringElement = null;
                }
            }

            foreach (var item in listPointerEnterExit)
            {
                if (item.Value.lastUsage == UsageOfHover.OnEnter)
                {
                    if (_alreadyEntered.ContainsKey(item.Value.pointerEnter) &&
                        _alreadyEntered[item.Value.pointerEnter] == item.Value.gameObject)
                    {
                        continue;
                    }

                    ExecuteEvents.ExecuteHierarchy(item.Value.gameObject,
                        item.Value.pointerEnter.pointerEventData,
                        ExecuteEvents.pointerEnterHandler);
                }
                else
                {
                    ExecuteEvents.ExecuteHierarchy(item.Value.gameObject,
                        item.Value.pointerExit.pointerEventData,
                        ExecuteEvents.pointerExitHandler);
                }
            }

            listPointerEnterExit.Clear();
            _alreadyEntered.Clear();
        }

        private static void AddCheckForDuplicates(List<GameObject> objects, GameObject potentiallyNew)
        {
            if (objects.Contains(potentiallyNew))
            {
                return;
            }

            objects.Add(potentiallyNew);
        }

        protected virtual void Click(VRTK4_UIPointer pointer, List<RaycastResult> results)
        {
            switch (pointer.clickMethod)
            {
                case VRTK4_UIPointer.ClickMethods.ClickOnButtonUp:
                    ClickOnUp(pointer, results);
                    break;
                case VRTK4_UIPointer.ClickMethods.ClickOnButtonDown:
                    ClickOnDown(pointer, results);
                    break;
            }
        }

        protected virtual void ClickOnUp(VRTK4_UIPointer pointer, List<RaycastResult> results)
        {
            pointer.pointerEventData.eligibleForClick = pointer.ValidClick(false);

            if (!AttemptClick(pointer))
            {
                IsEligibleClick(pointer, results);
            }
        }

        protected virtual void ClickOnDown(VRTK4_UIPointer pointer, List<RaycastResult> results,
            bool forceClick = false)
        {
            pointer.pointerEventData.eligibleForClick = (forceClick ? true : pointer.ValidClick(true));

            if (IsEligibleClick(pointer, results))
            {
                pointer.pointerEventData.eligibleForClick = false;
                AttemptClick(pointer);
            }
        }

        protected virtual bool IsEligibleClick(VRTK4_UIPointer pointer, List<RaycastResult> results)
        {
            if (pointer.pointerEventData.eligibleForClick)
            {
                for (int i = 0; i < 1 && i < results.Count; i++)
                {
                    RaycastResult result = results[i];
                    GameObject target = ExecuteEvents.ExecuteHierarchy(result.gameObject, pointer.pointerEventData,
                        ExecuteEvents.pointerDownHandler);
                    pointer.pointerEventData.delta = Vector2.zero;
                    if (!ValidElement(result.gameObject))
                    {
                        return false;
                    }

                    if (target != null)
                    {
                        pointer.pointerEventData.pressPosition = pointer.pointerEventData.position;
                        pointer.pointerEventData.pointerPressRaycast = result;
                        pointer.pointerEventData.pointerPress = target;
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual bool AttemptClick(VRTK4_UIPointer pointer)
        {
            if (pointer.pointerEventData.pointerPress != null)
            {
                if (!ValidElement(pointer.pointerEventData.pointerPress))
                {
                    pointer.pointerEventData.pointerPress = null;
                    return true;
                }

                if (pointer.pointerEventData.eligibleForClick)
                {
                    bool isHoveringBool = IsHovering(pointer);
                    if (!isHoveringBool)
                    {
                        ExecuteEvents.ExecuteHierarchy(pointer.pointerEventData.pointerPress, pointer.pointerEventData,
                            ExecuteEvents.pointerUpHandler);
                        pointer.pointerEventData.pointerPress = null;
                    }
                }
                else
                {
                    pointer.OnUIPointerElementClick(pointer.SetUIPointerEvent(
                        pointer.pointerEventData.pointerPressRaycast, pointer.pointerEventData.pointerPress));
                    if (pointer.IsValidStateForClickFromHover && !pointer.ExplicitBlockClickOnce)
                    {
                        ExecuteEvents.ExecuteHierarchy(pointer.pointerEventData.pointerPress, pointer.pointerEventData,
                            ExecuteEvents.pointerClickHandler);
                    }

                    pointer.ExplicitBlockClickOnce = false;

                    ExecuteEvents.ExecuteHierarchy(pointer.pointerEventData.pointerPress, pointer.pointerEventData,
                        ExecuteEvents.pointerUpHandler);
                    pointer.pointerEventData.pointerPress = null;
                }

                return true;
            }

            return false;
        }

        protected virtual void Drag(VRTK4_UIPointer pointer, List<RaycastResult> results)
        {
            pointer.pointerEventData.dragging =
                pointer.IsSelectionButtonPressed() && pointer.pointerEventData.delta != Vector2.zero;

            if (pointer.pointerEventData.pointerDrag)
            {
                if (!ValidElement(pointer.pointerEventData.pointerDrag))
                {
                    pointer.pointerEventData.pointerDrag = null;
                    return;
                }

                if (pointer.pointerEventData.dragging)
                {
                    if (IsHovering(pointer))
                    {
                        ExecuteEvents.ExecuteHierarchy(pointer.pointerEventData.pointerDrag, pointer.pointerEventData,
                            ExecuteEvents.dragHandler);
                    }
                }
                else
                {
                    ExecuteEvents.ExecuteHierarchy(pointer.pointerEventData.pointerDrag, pointer.pointerEventData,
                        ExecuteEvents.dragHandler);
                    ExecuteEvents.ExecuteHierarchy(pointer.pointerEventData.pointerDrag, pointer.pointerEventData,
                        ExecuteEvents.endDragHandler);
                    for (int i = 0; i < results.Count; i++)
                    {
                        ExecuteEvents.ExecuteHierarchy(results[i].gameObject, pointer.pointerEventData,
                            ExecuteEvents.dropHandler);
                    }

                    pointer.pointerEventData.pointerDrag = null;
                }
            }
            else if (pointer.pointerEventData.dragging)
            {
                for (int i = 0; i < results.Count; i++)
                {
                    RaycastResult result = results[i];
                    if (!ValidElement(result.gameObject))
                    {
                        continue;
                    }

                    ExecuteEvents.ExecuteHierarchy(result.gameObject, pointer.pointerEventData,
                        ExecuteEvents.initializePotentialDrag);
                    ExecuteEvents.ExecuteHierarchy(result.gameObject, pointer.pointerEventData,
                        ExecuteEvents.beginDragHandler);
                    GameObject target = ExecuteEvents.ExecuteHierarchy(result.gameObject, pointer.pointerEventData,
                        ExecuteEvents.dragHandler);
                    if (target != null)
                    {
                        pointer.pointerEventData.pointerDrag = target;
                        break;
                    }
                }
            }
        }

        // Still required to scroll over all elements
        protected virtual void Scroll(VRTK4_UIPointer pointer, List<RaycastResult> results)
        {
            if (pointer.axisAction == null)
            {
                return;
            }

            pointer.pointerEventData.scrollDelta = pointer.axisAction.Value;
            for (int i = 0; i < results.Count; i++)
            {
                if (pointer.pointerEventData.scrollDelta != Vector2.zero)
                {
                    ExecuteEvents.ExecuteHierarchy(results[i].gameObject, pointer.pointerEventData,
                        ExecuteEvents.scrollHandler);
                }
            }
        }
    }
}