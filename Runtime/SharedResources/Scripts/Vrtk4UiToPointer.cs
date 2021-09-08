namespace Tilia.VRTKUI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Tilia.Indicators.ObjectPointers;
    using UnityEngine;
    using Zinnia.Cast;
    using Zinnia.Data.Type;
    using Zinnia.Pointer;

    /// <summary>
    /// Mediator component for working with VRTK 3.3.0 ported UI system
    /// </summary>
    [DisallowMultipleComponent]
    public class Vrtk4UiToPointer : MonoBehaviour
    {
        [SerializeField] private PointerFacade pointerFacade;
        [SerializeField] private bool applyButtonsFromFacade = false;
        [SerializeField] private VRTK4_UIPointer UI_Pointer;
        private List<Vector3> temporalList = new List<Vector3>(2);
        private ObjectPointer.EventData eventData;
        private PointsCast.EventData pointerCastEventData;

        private void OnEnable()
        {
            UI_Pointer.UIPointerElementEnter += HoverEnter;
            UI_Pointer.UIPointerElementExit += OnHoverExit;
            UI_Pointer.UIPointerElementClick += OnSelectEnter;
            if (applyButtonsFromFacade)
            {
                UI_Pointer.activationButton = pointerFacade.ActivationAction;
                UI_Pointer.selectionButton = pointerFacade.SelectionAction;
            }
        }

        private void OnDisable()
        {
            UI_Pointer.UIPointerElementEnter -= HoverEnter;
            UI_Pointer.UIPointerElementExit -= OnHoverExit;
            UI_Pointer.UIPointerElementClick -= OnSelectEnter;
        }

        /// <summary>
        /// Generates all of the data for pointerfacade
        /// </summary>
        private void FillInEventData()
        {
            if (pointerCastEventData == null)
            {
                pointerCastEventData = new PointsCast.EventData();
                pointerCastEventData.Clear();
            }
            pointerCastEventData.Clear();
            pointerCastEventData.IsValid = true;
            
            if (eventData == null)
            {
                eventData = new ObjectPointer.EventData();
                eventData.Transform = transform;
                eventData.CurrentHoverDuration = 0.1f;
            }
            eventData.Clear();
            eventData.UseLocalValues = false;
            eventData.ScaleOverride = Vector3.one;
            eventData.Direction = pointerFacade.transform.forward;
            eventData.Origin = pointerFacade.Configuration.ObjectPointer.Origin.transform.position;
            eventData.PositionOverride = eventData.Origin;
            eventData.RotationOverride = pointerFacade.Configuration.ObjectPointer.Origin.transform.rotation;
        }

        private void HoverEnter(VRTK4_UIPointer interactable, VRTK4_UIPointer.VRTK4UIPointerEventArgs eventdata)
        {
            temporalList.Clear();
            temporalList.Add(eventdata.raycastResult.worldPosition);

            FillInEventData();
            pointerCastEventData.IsValid = true;
            var data = eventData;
            data.CurrentHoverDuration = 0.1f;
            data.IsCurrentlyHovering = true;
            data.CurrentPointsCastData = pointerCastEventData;
            data.CurrentPointsCastData.Points = new HeapAllocationFreeReadOnlyList<Vector3>(temporalList,0,temporalList.Count);
            pointerFacade.Configuration.EmitHoverChanged(data);
        }
        
        private void OnHoverExit(VRTK4_UIPointer interactable, VRTK4_UIPointer.VRTK4UIPointerEventArgs eventdata)
        {
            OnSelectExit(interactable, eventdata);
            
            temporalList.Clear();
            temporalList.Add(eventdata.raycastResult.worldPosition);
            
            FillInEventData();
            pointerCastEventData.IsValid = false;
            var data = eventData;
            data.CurrentHoverDuration = 0.9f;
            data.IsCurrentlyHovering = false;
            data.CurrentPointsCastData = pointerCastEventData;
            data.CurrentPointsCastData.Points = new HeapAllocationFreeReadOnlyList<Vector3>(temporalList,0,temporalList.Count);
            pointerFacade.Configuration.EmitHoverChanged(data);
        }
        
        private void OnSelectEnter(VRTK4_UIPointer interactable, VRTK4_UIPointer.VRTK4UIPointerEventArgs eventdata)
        {
            if (interactable == null)
            {
                return;
            }
            temporalList.Clear();
            temporalList.Add(eventdata.raycastResult.worldPosition);
            FillInEventData();
            pointerCastEventData.IsValid = true;
            var data = eventData;
            data.CurrentHoverDuration = 0.1f;
            data.IsCurrentlyHovering = true;
            data.IsCurrentlyActive = true;
            data.CurrentPointsCastData = pointerCastEventData;
            data.CurrentPointsCastData.Points = new HeapAllocationFreeReadOnlyList<Vector3>(temporalList,0,temporalList.Count);
            pointerFacade.Configuration.EmitSelected(data);
        }
        
        private void OnSelectExit(VRTK4_UIPointer interactable, VRTK4_UIPointer.VRTK4UIPointerEventArgs eventdata)
        {
            temporalList.Clear();
            temporalList.Add(eventdata.raycastResult.worldPosition);
            
            FillInEventData();
            pointerCastEventData.IsValid = true;
            var data = eventData;
            data.CurrentHoverDuration = 0.9f;
            data.IsCurrentlyHovering = false;
            data.IsCurrentlyActive = false;
            data.CurrentPointsCastData = pointerCastEventData;
            data.CurrentPointsCastData.Points = new HeapAllocationFreeReadOnlyList<Vector3>(temporalList,0,temporalList.Count);
            pointerFacade.Configuration.EmitExited(data);
        }
    }
}