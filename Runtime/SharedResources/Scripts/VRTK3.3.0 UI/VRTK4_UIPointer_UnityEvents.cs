namespace Tilia.VRTKUI.UnityEventHelper
{
    using UnityEngine;
    using UnityEngine.Events;
    using System;

    public sealed class VRTK4_UIPointer_UnityEvents : VRTK4_UnityEvents<VRTK4_UIPointer>
    {
        [Serializable]
        public sealed class UIPointerEvent : UnityEvent<object, VRTK4_UIPointer.VRTK4UIPointerEventArgs> { }
        [Serializable]
        public sealed class UIPointerEventDirect : UnityEvent<VRTK4_UIPointer> { }


        public UIPointerEvent OnUIPointerElementEnter = new UIPointerEvent();
        public UIPointerEvent OnUIPointerElementExit = new UIPointerEvent();
        public UIPointerEvent OnUIPointerElementClick = new UIPointerEvent();
        public UIPointerEvent OnUIPointerElementDragStart = new UIPointerEvent();
        public UIPointerEvent OnUIPointerElementDragEnd = new UIPointerEvent();
        public UIPointerEventDirect OnActivationButtonPressed = new UIPointerEventDirect();
        public UIPointerEventDirect OnActivationButtonReleased = new UIPointerEventDirect();
        public UIPointerEventDirect OnSelectionButtonPressed = new UIPointerEventDirect();
        public UIPointerEventDirect OnSelectionButtonReleased = new UIPointerEventDirect();

        protected override void AddListeners(VRTK4_UIPointer component)
        {
            component.UIPointerElementEnter += UIPointerElementEnter;
            component.UIPointerElementExit += UIPointerElementExit;
            component.UIPointerElementClick += UIPointerElementClick;
            component.UIPointerElementDragStart += UIPointerElementDragStart;
            component.UIPointerElementDragEnd += UIPointerElementDragEnd;
            component.ActivationButtonPressed += ActivationButtonPressed;
            component.ActivationButtonReleased += ActivationButtonReleased;
            component.SelectionButtonPressed += SelectionButtonPressed;
            component.SelectionButtonReleased += SelectionButtonReleased;
        }

        protected override void RemoveListeners(VRTK4_UIPointer component)
        {
            component.UIPointerElementEnter -= UIPointerElementEnter;
            component.UIPointerElementExit -= UIPointerElementExit;
            component.UIPointerElementClick -= UIPointerElementClick;
            component.UIPointerElementDragStart -= UIPointerElementDragStart;
            component.UIPointerElementDragEnd -= UIPointerElementDragEnd;
            component.ActivationButtonPressed -= ActivationButtonPressed;
            component.ActivationButtonReleased -= ActivationButtonReleased;
            component.SelectionButtonPressed -= SelectionButtonPressed;
            component.SelectionButtonReleased -= SelectionButtonReleased;
        }

        private void UIPointerElementEnter(object o, VRTK4_UIPointer.VRTK4UIPointerEventArgs e)
        {
            OnUIPointerElementEnter.Invoke(o, e);
        }

        private void UIPointerElementExit(object o, VRTK4_UIPointer.VRTK4UIPointerEventArgs e)
        {
            OnUIPointerElementExit.Invoke(o, e);
        }

        private void UIPointerElementClick(object o, VRTK4_UIPointer.VRTK4UIPointerEventArgs e)
        {
            OnUIPointerElementClick.Invoke(o, e);
        }

        private void UIPointerElementDragStart(object o, VRTK4_UIPointer.VRTK4UIPointerEventArgs e)
        {
            OnUIPointerElementDragStart.Invoke(o, e);
        }

        private void UIPointerElementDragEnd(object o, VRTK4_UIPointer.VRTK4UIPointerEventArgs e)
        {
            OnUIPointerElementDragEnd.Invoke(o, e);
        }

        private void ActivationButtonPressed(VRTK4_UIPointer pointer)
        {
            OnActivationButtonPressed.Invoke(pointer);
        }

        private void ActivationButtonReleased(VRTK4_UIPointer pointer)
        {
            OnActivationButtonReleased.Invoke(pointer);
        }

        private void SelectionButtonPressed(VRTK4_UIPointer pointer)
        {
            OnSelectionButtonPressed.Invoke(pointer);
        }

        private void SelectionButtonReleased(VRTK4_UIPointer pointer)
        {
            OnSelectionButtonReleased.Invoke(pointer);
        }
    }
}