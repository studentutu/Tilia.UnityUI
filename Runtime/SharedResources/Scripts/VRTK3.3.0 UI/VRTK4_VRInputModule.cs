namespace Tilia.VRTKUI
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using System.Collections.Generic;

    /// <summary>
    /// Main Processor of all of the input. By default supports standard Unity input + UI Pointers
    /// </summary>
    public class VRTK4_VRInputModule : PointerInputModule
    {
        public List<VRTK4_UIPointer> Pointers = new List<VRTK4_UIPointer>();
        protected List<RaycastResult> raycasts = new List<RaycastResult>();

        public virtual void Initialise()
        {
            Pointers.Clear();
        }

        //Needed to allow other regular (non-VR) InputModules in combination with VRTK_EventSystem
        public override bool IsModuleSupported()
        {
            return false;
        }

        public override void Process()
        {
            for (int i = 0; i < Pointers.Count; i++)
            {
                VRTK4_UIPointer pointer = Pointers[i];
                if (pointer.gameObject.activeInHierarchy && pointer.enabled)
                {
                    List<RaycastResult> results = new List<RaycastResult>();
                    if (pointer.PointerActive() || pointer.autoActivatingCanvas != null)
                    {
                        results = CheckRaycasts(pointer);
                    }

                    //Process events
                    Hover(pointer, results);
                    Click(pointer, results);
                    Drag(pointer, results);
                    Scroll(pointer, results);
                }
            }
        }

        protected virtual List<RaycastResult> CheckRaycasts(VRTK4_UIPointer pointer)
        {
            RaycastResult raycastResult = new RaycastResult();
            raycastResult.worldPosition = pointer.GetOriginPosition();
            raycastResult.worldNormal = pointer.GetOriginForward();

            pointer.pointerEventData.pointerCurrentRaycast = raycastResult;
            VRTK4_UIGraphicRaycaster.CurrentPointer = pointer;
            raycasts.Clear();
            eventSystem.RaycastAll(pointer.pointerEventData, raycasts); // already sorted!
            // raycasts.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));
            if (raycasts.Count > 0)
            {
                var toUse = raycasts[0];
                var lastKnownPosition = pointer.pointerEventData.position;
                pointer.pointerEventData.position = toUse.screenPosition;
                pointer.pointerEventData.delta = pointer.pointerEventData.position - lastKnownPosition;
                pointer.pointerEventData.pointerCurrentRaycast = toUse;
            }

            VRTK4_UIGraphicRaycaster.CurrentPointer = null;
            return raycasts;
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
            VRTK4_UIGraphicRaycaster canvasCheck = obj.GetComponentInParent<VRTK4_UIGraphicRaycaster>();
            return canvasCheck != null && canvasCheck.enabled;
        }

        protected virtual void CheckPointerHoverClick(VRTK4_UIPointer pointer, List<RaycastResult> results)
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

        protected virtual void Hover(VRTK4_UIPointer pointer, List<RaycastResult> results)
        {
            if (pointer.pointerEventData.pointerEnter != null)
            {
                CheckPointerHoverClick(pointer, results);
                if (!ValidElement(pointer.pointerEventData.pointerEnter))
                {
                    pointer.pointerEventData.pointerEnter = null;
                    return;
                }

                if (NoValidCollision(pointer, results))
                {
                    ExecuteEvents.ExecuteHierarchy(pointer.pointerEventData.pointerEnter, pointer.pointerEventData,
                        ExecuteEvents.pointerExitHandler);
                    pointer.pointerEventData.hovered.Remove(pointer.pointerEventData.pointerEnter);
                    pointer.pointerEventData.pointerEnter = null;
                }
            }

            for (int i = 0; i < 1 && i < results.Count; i++)
            {
                RaycastResult result = results[i];
                GameObject target = ExecuteEvents.ExecuteHierarchy(result.gameObject, pointer.pointerEventData,
                    ExecuteEvents.pointerEnterHandler);
                if (!ValidElement(result.gameObject))
                {
                    continue;
                }

                target = (target == null ? result.gameObject : target);

                if (target != null)
                {
                    Selectable selectable = target.GetComponent<Selectable>();
                    if (selectable != null)
                    {
                        Navigation noNavigation = new Navigation();
                        noNavigation.mode = Navigation.Mode.None;
                        selectable.navigation = noNavigation;
                    }

                    if (pointer.hoveringElement != null && pointer.hoveringElement != target)
                    {
                        pointer.OnUIPointerElementExit(pointer.SetUIPointerEvent(result, null,
                            pointer.hoveringElement));
                    }

                    pointer.OnUIPointerElementEnter(pointer.SetUIPointerEvent(result, target,
                        pointer.hoveringElement));
                    pointer.hoveringElement = target;
                    pointer.pointerEventData.pointerCurrentRaycast = result;
                    pointer.pointerEventData.pointerEnter = target;
                    pointer.pointerEventData.hovered.Add(pointer.pointerEventData.pointerEnter);
                    break;
                }

                if (result.gameObject != pointer.hoveringElement)
                {
                    pointer.OnUIPointerElementEnter(pointer.SetUIPointerEvent(result, result.gameObject,
                        pointer.hoveringElement));
                }

                pointer.hoveringElement = result.gameObject;
            }

            if (pointer.hoveringElement && results.Count == 0)
            {
                pointer.OnUIPointerElementExit(pointer.SetUIPointerEvent(new RaycastResult(), null,
                    pointer.hoveringElement));
                pointer.hoveringElement = null;
            }
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
            if (pointer.pointerEventData.pointerPress)
            {
                if (!ValidElement(pointer.pointerEventData.pointerPress))
                {
                    pointer.pointerEventData.pointerPress = null;
                    return true;
                }

                if (pointer.pointerEventData.eligibleForClick)
                {
                    if (!IsHovering(pointer))
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
                    ExecuteEvents.ExecuteHierarchy(pointer.pointerEventData.pointerPress, pointer.pointerEventData,
                        ExecuteEvents.pointerClickHandler);
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
            pointer.pointerEventData.scrollDelta =
                (pointer.axisAction != null ? pointer.axisAction.Value : Vector2.zero);
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