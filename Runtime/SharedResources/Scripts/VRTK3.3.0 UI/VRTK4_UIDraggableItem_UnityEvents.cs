namespace Tilia.VRTKUI.UnityEventHelper
{
    using UnityEngine;
    using UnityEngine.Events;
    using System;

    public sealed class VRTK4_UIDraggableItem_UnityEvents : VRTK4_UnityEvents<VRTK4_UIDraggableItem>
    {
        [Serializable]
        public sealed class UIDraggableItemEvent : UnityEvent<object, VRTK4_UIDraggableItem.UIDraggableItemEventArgs> { }

        public UIDraggableItemEvent OnDraggableItemDropped = new UIDraggableItemEvent();
        public UIDraggableItemEvent OnDraggableItemReset = new UIDraggableItemEvent();

        protected override void AddListeners(VRTK4_UIDraggableItem component)
        {
            component.DraggableItemDropped += DraggableItemDropped;
            component.DraggableItemReset += DraggableItemReset;
        }

        protected override void RemoveListeners(VRTK4_UIDraggableItem component)
        {
            component.DraggableItemDropped -= DraggableItemDropped;
            component.DraggableItemReset -= DraggableItemReset;
        }

        private void DraggableItemDropped(object o, VRTK4_UIDraggableItem.UIDraggableItemEventArgs e)
        {
            OnDraggableItemDropped.Invoke(o, e);
        }

        private void DraggableItemReset(object o, VRTK4_UIDraggableItem.UIDraggableItemEventArgs e)
        {
            OnDraggableItemReset.Invoke(o, e);
        }
    }
}