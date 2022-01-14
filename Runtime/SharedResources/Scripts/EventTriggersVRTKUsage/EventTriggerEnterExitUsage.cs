using System;
using Tilia.VRTKUI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Tilia.UnityUI
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

    public class EventTriggerEnterExitUsage : MonoBehaviour
    {
        [SerializeField] private TiliaUnityEventTriggerSubscriber OnEnter =
            new TiliaUnityEventTriggerSubscriber(EventTriggerType.PointerEnter);

        [SerializeField] private TiliaUnityEventTriggerSubscriber OnExit =
            new TiliaUnityEventTriggerSubscriber(EventTriggerType.PointerExit);

        private bool _isAlreadyEntered = false;

        private void OnEnable()
        {
            _isAlreadyEntered = false;
            OnEnter.AddListener(OnEnterListener);
            OnExit.AddListener(OnExitListener);
        }

        private void OnExitListener(BaseEventData arg0)
        {
            if (OnExit.Target == null || !enabled)
            {
                return;
            }

            if (!VRTK4_EventSystem.IsVRTK4Active())
            {
                ForceExit();
                return;
            }

            // when multiple pointers overlap - each of them executes all events.
            // Make Sure to check if the object itself is still hovered by any of the pointers.
            if (VRTK4_UIPointer.CheckIfObjectIsHovered(OnExit.Target.gameObject))
            {
                return;
            }

            ForceExit();
        }

        private void ForceExit()
        {
            _isAlreadyEntered = false;
            Debug.LogWarning("OnExit " + OnExit.Target.gameObject.name, OnExit.Target);
        }

        private void OnEnterListener(BaseEventData arg0)
        {
            if (OnEnter.Target == null)
            {
                return;
            }

            if (!VRTK4_EventSystem.IsVRTK4Active())
            {
                ForceEnter();
                return;
            }

            if (_isAlreadyEntered)
            {
                return;
            }

            ForceEnter();
        }

        private void ForceEnter()
        {
            _isAlreadyEntered = true;
            Debug.LogWarning("OnEntered " + OnEnter.Target.gameObject.name, OnEnter.Target);
        }

        private void OnDisable()
        {
            OnEnter.RemoveListener(OnEnterListener);
            OnExit.RemoveListener(OnExitListener);
        }
    }
}