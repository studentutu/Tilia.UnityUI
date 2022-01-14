using UnityEngine;

namespace Tilia.VRTKUI
{
    using System.Collections;
    using System.Linq;
    using System.Reflection;
    using UnityEngine.EventSystems;

    /// <summary>
    /// Overriden standard event system so that it will work both with standard UI and specific UI Pointers 
    /// </summary>
    [DisallowMultipleComponent]
    public class VRTK4_EventSystem : EventSystem
    {
        private static VRTK4_EventSystem _instance = null;

        public static VRTK4_EventSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<VRTK4_EventSystem>();
                }

                return _instance;
            }
        }

        protected EventSystem previousEventSystem;
        protected VRTK4_VRInputModule vrInputModule;
        public VRTK4_VRInputModule VRInputModule => vrInputModule;

        public static bool IsVRTK4Active()
        {
            return _instance != null;
        }

        private static readonly FieldInfo[] EVENT_SYSTEM_FIELD_INFOS =
            typeof(EventSystem).GetFields(BindingFlags.Public | BindingFlags.Instance);

        private static readonly PropertyInfo[] EVENT_SYSTEM_PROPERTY_INFOS = typeof(EventSystem)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Except(new[] {typeof(EventSystem).GetProperty("enabled")}).ToArray();

        private static readonly FieldInfo BASE_INPUT_MODULE_EVENT_SYSTEM_FIELD_INFO =
            typeof(BaseInputModule).GetField("m_EventSystem", BindingFlags.NonPublic | BindingFlags.Instance);

        public void InitializeWithVRModule(VRTK4_VRInputModule module)
        {
            vrInputModule = module;
        }

        protected override void OnEnable()
        {
            previousEventSystem = EventSystem.current;
            if (previousEventSystem != null)
            {
                previousEventSystem.enabled = false;
                CopyValuesFrom(previousEventSystem, this);
            }

            if (vrInputModule == null)
            {
                vrInputModule = gameObject.GetComponent<VRTK4_VRInputModule>();
                if (vrInputModule == null)
                {
                    vrInputModule = gameObject.AddComponent<VRTK4_VRInputModule>();
                }
            }

            base.OnEnable();
            StartCoroutine(SetEventSystemOfBaseInputModulesAfterFrameDelay(this));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Destroy(vrInputModule);

            if (previousEventSystem != null)
            {
                previousEventSystem.enabled = true;
                CopyValuesFrom(this, previousEventSystem);
                SetEventSystemOfBaseInputModules(previousEventSystem);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (current == this)
            {
                vrInputModule.Process();
            }
        }

#if UNITY_5_5_OR_NEWER
        protected override void OnApplicationFocus(bool hasFocus)
        {
            //Don't call the base implementation because it will set a pause flag for this EventSystem
        }
#endif

        private static void CopyValuesFrom(EventSystem fromEventSystem, EventSystem toEventSystem)
        {
            foreach (FieldInfo fieldInfo in EVENT_SYSTEM_FIELD_INFOS)
            {
                fieldInfo.SetValue(toEventSystem, fieldInfo.GetValue(fromEventSystem));
            }

            foreach (PropertyInfo propertyInfo in EVENT_SYSTEM_PROPERTY_INFOS)
            {
                if (propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(toEventSystem, propertyInfo.GetValue(fromEventSystem, null), null);
                }
            }
        }

        private static IEnumerator SetEventSystemOfBaseInputModulesAfterFrameDelay(EventSystem eventSystem)
        {
            yield return null;
            SetEventSystemOfBaseInputModules(eventSystem);
        }

        private static void SetEventSystemOfBaseInputModules(EventSystem eventSystem)
        {
            /*
            BaseInputModule has a private field referencing the current EventSystem. That field is set in
            BaseInputModule.OnEnable only. It's used in BaseInputModule.OnEnable and BaseInputModule.OnDisable
            to call EventSystem.UpdateModules.
            This means we could just disable and enable every enabled BaseInputModule to fix that reference.

            But the StandaloneInputModule (which is added by default when adding an EventSystem in the Editor)
            requires EventSystem. Which means we can't correctly destroy the old EventSystem first and then add
            our own one.
            We therefore update that private reference directly here.
            */
            foreach (BaseInputModule module in FindObjectsOfType<BaseInputModule>())
            {
                BASE_INPUT_MODULE_EVENT_SYSTEM_FIELD_INFO.SetValue(module, eventSystem);
            }

            eventSystem.UpdateModules();
        }
    }
}