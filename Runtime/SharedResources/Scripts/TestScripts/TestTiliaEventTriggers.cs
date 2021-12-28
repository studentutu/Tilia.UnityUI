using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Tilia.UnityUi
{
    [Serializable]
    public class TiliaUnityEventTriggerSubscriber
    {
        [SerializeField] private EventTrigger _trigger;
        [SerializeField] private EventTriggerType _type;

        public EventTrigger Target => _trigger;

        public TiliaUnityEventTriggerSubscriber()
        {
        }

        public TiliaUnityEventTriggerSubscriber(EventTriggerType type)
        {
            _type = type;
        }

        public void AddListener(UnityAction<BaseEventData> call)
        {
            if (_trigger == null)
            {
                return;
            }

            RemoveListener(call);
            _trigger.AddListener(_type, call);
        }

        public void RemoveListener(UnityAction<BaseEventData> call)
        {
            if (_trigger == null)
            {
                return;
            }

            _trigger.RemoveListener(_type, call);
        }
    }

    public static class TiliaEventTriggerExtensions
    {
        public static void AddListener(
            this EventTrigger eventTrigger,
            EventTriggerType triggerType,
            UnityAction<BaseEventData> call)
        {
            if (eventTrigger == null)
            {
                throw new ArgumentNullException(nameof(eventTrigger));
            }

            if (call == null)
            {
                throw new ArgumentNullException(nameof(call));
            }

            EventTrigger.Entry entry = eventTrigger.triggers
                .Find(e => e.eventID == triggerType);
            if (entry == null)
            {
                entry = new EventTrigger.Entry();
                entry.eventID = triggerType;
                eventTrigger.triggers.Add(entry);
            }

            entry.callback.AddListener(call);
        }


        public static void RemoveListener(
            this EventTrigger eventTrigger,
            EventTriggerType triggerType,
            UnityAction<BaseEventData> call)
        {
            if (eventTrigger == null)
            {
                throw new ArgumentNullException(nameof(eventTrigger));
            }

            if (call == null)
            {
                throw new ArgumentNullException(nameof(call));
            }

            EventTrigger.Entry entry = eventTrigger.triggers
                .Find(e => e.eventID == triggerType);
            if (entry != null)
            {
                entry.callback.RemoveListener(call);
            }
        }
    }

    public class TestTiliaEventTriggers : MonoBehaviour
    {
        [SerializeField] private TiliaUnityEventTriggerSubscriber OnEnter =
            new TiliaUnityEventTriggerSubscriber(EventTriggerType.PointerEnter);

        [SerializeField] private TiliaUnityEventTriggerSubscriber OnExit =
            new TiliaUnityEventTriggerSubscriber(EventTriggerType.PointerExit);

        [SerializeField] private TiliaUnityEventTriggerSubscriber OnStartDrag =
            new TiliaUnityEventTriggerSubscriber(EventTriggerType.BeginDrag);

        [SerializeField] private TiliaUnityEventTriggerSubscriber OnEndDrag =
            new TiliaUnityEventTriggerSubscriber(EventTriggerType.EndDrag);


        private void OnEnable()
        {
            OnEnter.AddListener(OnEnterListener);
            OnExit.AddListener(OnExitListener);
            OnStartDrag.AddListener(OnStartDragListener);
            OnEndDrag.AddListener(OnEndDragListener);
        }

        private void OnEndDragListener(BaseEventData arg0)
        {
            Debug.LogWarning("OnEndDrag " + OnEndDrag.Target.gameObject.name, OnEndDrag.Target.gameObject);
        }

        private void OnStartDragListener(BaseEventData arg0)
        {
            Debug.LogWarning("OnStartDrag " + OnStartDrag.Target.gameObject.name, OnStartDrag.Target.gameObject);
        }

        private void OnExitListener(BaseEventData arg0)
        {
            Debug.LogWarning("OnExit " + OnExit.Target.gameObject.name, OnExit.Target.gameObject);
        }

        private void OnEnterListener(BaseEventData arg0)
        {
            Debug.LogWarning("OnEntered " + OnEnter.Target.gameObject.name, OnEnter.Target.gameObject);
        }

        private void OnDisable()
        {
            OnEnter.RemoveListener(OnEnterListener);
            OnExit.RemoveListener(OnExitListener);
            OnStartDrag.RemoveListener(OnStartDragListener);
            OnEndDrag.RemoveListener(OnEndDragListener);
        }
    }
}