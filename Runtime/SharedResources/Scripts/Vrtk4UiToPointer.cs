using UnityEngine.Serialization;
using System.Collections.Generic;
using Tilia.Indicators.ObjectPointers;
using UnityEngine;
using Zinnia.Cast;
using Zinnia.Data.Type;
using Zinnia.Pointer;

namespace Tilia.VRTKUI
{
    /// <summary>
    /// Mediator component for working with VRTK 3.3.0 ported UI system
    /// </summary>
    [DisallowMultipleComponent]
    public class Vrtk4UiToPointer : MonoBehaviour
    {
        [SerializeField] private PointerFacade pointerFacade;
        [SerializeField] private bool applyButtonsFromFacade;
        [SerializeField, FormerlySerializedAs("UI_Pointer")] private VRTK4_UIPointer uiPointer;

        private const float HoverEnterDuration = 0.1f;
        private const float HoverExitDuration = 0.9f;
        
        private readonly List<Vector3> cachedRaycastResultWorldPositions = new(2);

        private ObjectPointer.EventData objectPointerEventData;
        private PointsCast.EventData pointsCastEventData;

        private void OnEnable()
        {
            if (ValidateSerializedFields() == false)
            {
                return;
            }

            uiPointer.UIPointerElementEnter += HoverEnter;
            uiPointer.UIPointerElementExit += OnHoverExit;
            uiPointer.UIPointerElementClick += OnSelectEnter;

            if (applyButtonsFromFacade)
            {
                uiPointer.activationButton = pointerFacade.ActivationAction;
                uiPointer.selectionButton = pointerFacade.SelectionAction;
            }
        }

        private void OnDisable()
        {
            if (ValidateSerializedFields() == false)
            {
                return;
            }

            uiPointer.UIPointerElementEnter -= HoverEnter;
            uiPointer.UIPointerElementExit -= OnHoverExit;
            uiPointer.UIPointerElementClick -= OnSelectEnter;
        }

        private bool ValidateSerializedFields()
        {
            return uiPointer != null && pointerFacade != null;
        }

        private void HoverEnter(VRTK4_UIPointer interactable, VRTK4_UIPointer.VRTK4UIPointerEventArgs eventData)
        {
            if (ValidatePointerData(interactable) == false)
            {
                return;
            }

            CacheRaycastResultWorldPosition(eventData.raycastResult.worldPosition);
            FillInEventData();
            ChangePointsCastEventDataValidState(true);

            pointerFacade.Configuration.EmitHoverChanged(PrepareHoverEventDataObject(HoverEnterDuration, true));
        }

        private void OnHoverExit(VRTK4_UIPointer interactable, VRTK4_UIPointer.VRTK4UIPointerEventArgs eventData)
        {
            if (ValidatePointerData(interactable) == false)
            {
                return;
            }

            OnSelectExit(eventData);
            CacheRaycastResultWorldPosition(eventData.raycastResult.worldPosition);
            FillInEventData();
            ChangePointsCastEventDataValidState(false);

            pointerFacade.Configuration.EmitHoverChanged(PrepareHoverEventDataObject(HoverExitDuration, false));
        }

        private void OnSelectEnter(VRTK4_UIPointer interactable, VRTK4_UIPointer.VRTK4UIPointerEventArgs eventData)
        {
            if (ValidatePointerData(interactable) == false)
            {
                return;
            }

            CacheRaycastResultWorldPosition(eventData.raycastResult.worldPosition);
            FillInEventData();
            ChangePointsCastEventDataValidState(true);

            pointerFacade.Configuration.EmitSelected(PrepareSelectEventDataObject(HoverEnterDuration, true, true));
        }

        private void OnSelectExit(VRTK4_UIPointer.VRTK4UIPointerEventArgs eventData)
        {
            CacheRaycastResultWorldPosition(eventData.raycastResult.worldPosition);
            FillInEventData();
            ChangePointsCastEventDataValidState(true);

            pointerFacade.Configuration.EmitExited(PrepareSelectEventDataObject(HoverExitDuration, false, false));
        }

        private bool ValidatePointerData(VRTK4_UIPointer interactable)
        {
            return interactable != null && pointerFacade != null;
        }

        private void CacheRaycastResultWorldPosition(Vector3 worldPosition)
        {
            cachedRaycastResultWorldPositions.Clear();
            cachedRaycastResultWorldPositions.Add(worldPosition);
        }
        
        /// <summary>
        /// Generates all of the data for pointerFacade
        /// </summary>
        private void FillInEventData()
        {
            pointsCastEventData ??= new PointsCast.EventData();

            objectPointerEventData ??= new ObjectPointer.EventData
            {
                Transform = transform
            };

            objectPointerEventData.Clear();
            objectPointerEventData.UseLocalValues = false;
            objectPointerEventData.ScaleOverride = Vector3.one;
            objectPointerEventData.Direction = pointerFacade.transform.forward;
            objectPointerEventData.PositionOverride = objectPointerEventData.Origin;
            var objectPointerOriginTransform = pointerFacade.Configuration.ObjectPointer.Origin.transform;
            objectPointerEventData.Origin = objectPointerOriginTransform.position;
            objectPointerEventData.RotationOverride = objectPointerOriginTransform.rotation;
        }

        private void ChangePointsCastEventDataValidState(bool isValid)
        {
            pointsCastEventData.IsValid = isValid;
        }

        private ObjectPointer.EventData PrepareSelectEventDataObject(float currentHoverDuration, bool isCurrentlyHovering, bool isCurrentlyActive)
        {
            var data = PrepareHoverEventDataObject(currentHoverDuration, isCurrentlyHovering);
            data.IsCurrentlyActive = isCurrentlyActive;

            return data;
        }

        private ObjectPointer.EventData PrepareHoverEventDataObject(float currentHoverDuration, bool isCurrentlyHovering)
        {
            var data = objectPointerEventData;
            data.CurrentHoverDuration = currentHoverDuration;
            data.IsCurrentlyHovering = isCurrentlyHovering;
            data.CurrentPointsCastData = pointsCastEventData;
            data.CurrentPointsCastData.Points = new HeapAllocationFreeReadOnlyList<Vector3>(cachedRaycastResultWorldPositions, 0, cachedRaycastResultWorldPositions.Count);

            return data;
        }
    }
}