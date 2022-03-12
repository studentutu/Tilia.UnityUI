using System;
using Zinnia.Action;

namespace Tilia.VRTKUI
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using System.Collections.Generic;

    /// <summary>
    /// [RequireComponent(typeof(Vrtk4UiToPointer))]
    /// Provides the ability to interact with UICanvas elements and the contained Unity UI elements within.
    /// </summary>
    /// <remarks>
    /// **Optional Components:**
    ///  * `VRTK_ControllerEvents` - The events component to listen for the button presses on. This must be applied on the same GameObject as this script if one is not provided via the `Controller` parameter.
    ///
    /// **Script Usage:**
    ///  * Place the `VRTK_UIPointer` script on either:
    ///    * The controller script alias GameObject of the controller to emit the UIPointer from (e.g. Right Controller Script Alias).
    ///    * Any other scene GameObject and provide a valid `Transform` component to the `Pointer Origin Transform` parameter of this script. This does not have to be a controller and can be any GameObject that will emit the UIPointer.
    ///
    /// **Script Dependencies:**
    ///  * A UI Canvas attached to a Unity World UI Canvas.
    /// </remarks>
    /// <example>
    /// `VRTK/Examples/034_Controls_InteractingWithUnityUI` uses the `VRTK_UIPointer` script on the right Controller to allow for the interaction with Unity UI elements using a Simple Pointer beam. The left Controller controls a Simple Pointer on the headset to demonstrate gaze interaction with Unity UI elements.
    /// </example>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Vrtk4UiToPointer))]
    public class VRTK4_UIPointer : MonoBehaviour
    {
        private static List<VRTK4_UIPointer> VrtkUiPointers = new List<VRTK4_UIPointer>();

        /// <summary>
        /// Event Payload
        /// </summary>
        /// <param name="controllerReference">The reference to the controller that was used.</param>
        /// <param name="isActive">The state of whether the UI Pointer is currently active or not.</param>
        /// <param name="currentTarget">The current UI element that the pointer is colliding with.</param>
        /// <param name="previousTarget">The previous UI element that the pointer was colliding with.</param>
        /// <param name="raycastResult">The raw raycast result of the UI ray collision.</param>
        public struct VRTK4UIPointerEventArgs
        {
            public int UIPointerId;
            public bool isActive;
            public GameObject currentTarget;
            public GameObject previousTarget;
            public RaycastResult raycastResult;
        }

        /// <summary>
        /// Event Payload
        /// </summary>
        /// <param name="sender">this object</param>
        /// <param name="e"><see cref="VRTK4UIPointerEventArgs"/></param>
        public delegate void UIPointerEventHandler(VRTK4_UIPointer sender, VRTK4UIPointerEventArgs e);

        /// <summary>
        /// Event Payload
        /// </summary>
        /// <param name="sender">this object</param>
        public delegate void UIControllerInteractionEvent(VRTK4_UIPointer sender);


        /// <summary>
        /// Methods of when to consider a UI Click action
        /// </summary>
        public enum ClickMethods
        {
            /// <summary>
            /// Consider a UI Click action has happened when the UI Click alias button is released.
            /// </summary>
            ClickOnButtonUp,

            /// <summary>
            /// Consider a UI Click action has happened when the UI Click alias button is pressed.
            /// </summary>
            ClickOnButtonDown
        }

        [Header("Activation Settings")] [Tooltip("Optional.The independent scroll axis from the device.")]
        public Vector2Action axisAction = null;

        [Tooltip("The button used to activate/deactivate the UI raycast for the pointer.")]
        public BooleanAction activationButton = null;

        [Header("Selection Settings")]
        [Tooltip("The button used to execute the select action at the pointer's target position.")]
        public BooleanAction selectionButton = null;

        [Tooltip("Determines when the UI Click event action should happen.")]
        public ClickMethods clickMethod = ClickMethods.ClickOnButtonUp;

        [Tooltip(
            "Determines whether the UI click action should be triggered when the pointer is deactivated. If the pointer is hovering over a clickable element then it will invoke the click action on that element. Note: Only works with `Click Method =  Click_On_Button_Up`")]
        public bool attemptClickOnDeactivate = false;

        [Tooltip(
            "The amount of time the pointer can be over the same UI element before it automatically attempts to click it. 0f means no click attempt will be made.")]
        public float clickAfterHoverDuration = 0f;

        [Header("Customisation Settings")] [Tooltip("The maximum length the UI Raycast will reach.")]
        public float maximumLength = float.PositiveInfinity;

        [Tooltip(
            "A custom transform to use as the origin of the pointer. If no pointer origin transform is provided then the transform the script is attached to is used.")]
        public Transform customOrigin = null;

        [HideInInspector] public PointerEventData pointerEventData;
        [HideInInspector] public GameObject hoveringElement;
        [HideInInspector] public float hoverDurationTimer = 0f;
        [HideInInspector] public bool canClickOnHover = false;

        /// <summary>
        /// The GameObject of the front trigger activator of the canvas currently being activated by this pointer.
        /// </summary>
        [HideInInspector] public GameObject autoActivatingCanvas = null;

        /// <summary>
        /// Determines if the UI Pointer has collided with a valid canvas that has collision click turned on.
        /// </summary>
        [HideInInspector] public bool collisionClick = false;

        /// <summary>
        /// Emitted when the UI activation button is pressed.
        /// </summary>
        public event UIControllerInteractionEvent ActivationButtonPressed;

        /// <summary>
        /// Emitted when the UI activation button is released.
        /// </summary>
        public event UIControllerInteractionEvent ActivationButtonReleased;

        /// <summary>
        /// Emitted when the UI selection button is pressed.
        /// </summary>
        public event UIControllerInteractionEvent SelectionButtonPressed;

        /// <summary>
        /// Emitted when the UI selection button is released.
        /// </summary>
        public event UIControllerInteractionEvent SelectionButtonReleased;

        /// <summary>
        /// Emitted when the UI Pointer is colliding with a valid UI element.
        /// </summary>
        public event UIPointerEventHandler UIPointerElementEnter;

        /// <summary>
        /// Emitted when the UI Pointer is no longer colliding with any valid UI elements.
        /// </summary>
        public event UIPointerEventHandler UIPointerElementExit;

        /// <summary>
        /// Emitted when the UI Pointer has clicked the currently collided UI element.
        /// </summary>
        public event UIPointerEventHandler UIPointerElementClick;

        /// <summary>
        /// Emitted when the UI Pointer begins dragging a valid UI element.
        /// </summary>
        public event UIPointerEventHandler UIPointerElementDragStart;

        /// <summary>
        /// Emitted when the UI Pointer stops dragging a valid UI element.
        /// </summary>
        public event UIPointerEventHandler UIPointerElementDragEnd;

        protected static Dictionary<int, float> pointerLengths = new Dictionary<int, float>();
        protected bool pointerClicked = false;
        protected bool beamEnabledState = false;
        protected bool lastPointerPressState = false;
        protected bool lastPointerClickState = false;
        protected GameObject currentTarget;
        [NonSerialized] public bool IsValidStateForClickFromHover = false;
        [NonSerialized] public bool ExplicitBlockClickOnce = false;

        protected VRTK4_VRInputModule cachedVRInputModule;

        /// <summary>
        /// The GetPointerLength method retrieves the maximum UI Pointer length for the given pointer ID.
        /// </summary>
        /// <param name="pointerId">The pointer ID for the UI Pointer to recieve the length for.</param>
        /// <returns>The maximum length the UI Pointer will cast to.</returns>
        internal static float GetPointerLength(int pointerId)
        {
            if (EventSystem.current == null)
            {
                return 0;
            }

            var asVrtk4System = EventSystem.current as VRTK4_EventSystem;
            if (asVrtk4System == null)
            {
                return 0;
            }

            return VRTK4_SharedMethods.GetDictionaryValue(pointerLengths, pointerId, float.MaxValue);
        }

        public virtual void OnUIPointerElementEnter(VRTK4UIPointerEventArgs e)
        {
            if (e.currentTarget != currentTarget)
            {
                ResetHoverTimer();
            }

            if (clickAfterHoverDuration > 0f && hoverDurationTimer <= 0f)
            {
                canClickOnHover = true;
                hoverDurationTimer = clickAfterHoverDuration;
            }

            currentTarget = e.currentTarget;
            IsValidStateForClickFromHover = true;
            if (pointerEventData.pointerPress != null && pointerEventData.pointerPress != currentTarget)
            {
                IsValidStateForClickFromHover = false;
            }

            if (UIPointerElementEnter != null)
            {
                UIPointerElementEnter(this, e);
            }
        }

        public virtual void OnUIPointerElementExit(VRTK4UIPointerEventArgs e)
        {
            if (e.previousTarget == currentTarget)
            {
                ResetHoverTimer();
            }

            ExplicitBlockClickOnce = false;
            IsValidStateForClickFromHover = false;
            if (UIPointerElementExit != null)
            {
                UIPointerElementExit(this, e);

                if (attemptClickOnDeactivate && !e.isActive && e.previousTarget)
                {
                    if (pointerEventData == null)
                    {
                        pointerEventData = new PointerEventData(EventSystem.current);
                    }

                    pointerEventData.pointerPress = e.previousTarget;
                }
            }
        }

        public virtual void OnUIPointerElementClick(VRTK4UIPointerEventArgs e)
        {
            if (e.currentTarget == currentTarget)
            {
                ResetHoverTimer();
            }

            if (IsValidStateForClickFromHover && !ExplicitBlockClickOnce)
            {
                if (UIPointerElementClick != null)
                {
                    UIPointerElementClick(this, e);
                }
            }
        }

        public virtual void OnUIPointerElementDragStart(VRTK4UIPointerEventArgs e)
        {
            if (UIPointerElementDragStart != null)
            {
                UIPointerElementDragStart(this, e);
            }
        }

        public virtual void OnUIPointerElementDragEnd(VRTK4UIPointerEventArgs e)
        {
            if (UIPointerElementDragEnd != null)
            {
                UIPointerElementDragEnd(this, e);
            }
        }

        protected virtual void OnActivationButtonPressed()
        {
            if (ActivationButtonPressed != null)
            {
                ActivationButtonPressed(this);
            }
        }

        protected virtual void OnActivationButtonReleased()
        {
            if (ActivationButtonReleased != null)
            {
                ActivationButtonReleased(this);
            }
        }

        protected virtual void OnSelectionButtonPressed()
        {
            if (SelectionButtonPressed != null)
            {
                SelectionButtonPressed(this);
            }
        }

        protected virtual void OnSelectionButtonReleased()
        {
            if (SelectionButtonReleased != null)
            {
                SelectionButtonReleased(this);
            }
        }

        public static bool CheckIfObjectIsHovered(GameObject targetObject)
        {
            foreach (var pointer in VrtkUiPointers)
            {
                if (targetObject == pointer.hoveringElement)
                {
                    return true;
                }

                if (targetObject == pointer.pointerEventData.pointerEnter)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Please check if event system if VRTK4_EventSystem.IsVRTK4Active()
        /// </summary>
        public static VRTK4_UIPointer CheckAndGetPointerIfObjectIsHovered(GameObject targetObject)
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            var asVrtk4System = EventSystem.current as VRTK4_EventSystem;
            if (asVrtk4System == null)
            {
                return null;
            }

            foreach (var pointer in VrtkUiPointers)
            {
                if (pointer.currentTarget == targetObject)
                {
                    return pointer;
                }

                if (targetObject == pointer.hoveringElement)
                {
                    return pointer;
                }

                if (targetObject == pointer.pointerEventData.pointerEnter)
                {
                    return pointer;
                }
            }

            return null;
        }

        internal static VRTK4_UIPointer GetByEventData(PointerEventData eventData)
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            var asVrtk4System = EventSystem.current as VRTK4_EventSystem;
            if (asVrtk4System == null)
            {
                return null;
            }

            foreach (var pointer in VrtkUiPointers)
            {
                if (pointer.currentTarget == eventData.pointerCurrentRaycast.gameObject)
                {
                    return pointer;
                }

                if (eventData.selectedObject == pointer.currentTarget)
                {
                    return pointer;
                }

                foreach (var hoveredWith in eventData.hovered)
                {
                    if (pointer.gameObject == hoveredWith || pointer.currentTarget == hoveredWith)
                    {
                        return pointer;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Please check if event system if VRTK4_EventSystem.IsVRTK4Active()
        /// </summary>
        public static VRTK4_UIPointer GetByPointerId(int pointerId)
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            var asVrtk4System = EventSystem.current as VRTK4_EventSystem;
            if (asVrtk4System == null)
            {
                return null;
            }

            if (VrtkUiPointers.Count > pointerId && pointerId >= 0)
            {
                return VrtkUiPointers[pointerId];
            }

            return null;
        }

        private int GetIndexOfUIPointer()
        {
            for (int i = 0; i < VrtkUiPointers.Count; i++)
            {
                if (VrtkUiPointers[i] == this)
                {
                    return i;
                }
            }

            return -1;
        }

        public virtual VRTK4UIPointerEventArgs SetUIPointerEvent(RaycastResult currentRaycastResult,
            GameObject newCurrentTarget, GameObject lastTarget = null)
        {
            VRTK4UIPointerEventArgs e;
            e.UIPointerId = GetIndexOfUIPointer();
            e.isActive = PointerActive();
            e.currentTarget = newCurrentTarget;
            e.previousTarget = lastTarget;
            e.raycastResult = currentRaycastResult;
            return e;
        }

        /// <summary>
        /// The SetEventSystem method is used to set up the global Unity event system for the UI pointer. It also handles disabling the existing Standalone Input Module that exists on the EventSystem and adds a custom VRTK Event System VR Input component that is required for interacting with the UI with VR inputs.
        /// </summary>
        /// <param name="eventSystem">The global Unity event system to be used by the UI pointers.</param>
        /// <returns>A custom input module that is used to detect input from VR pointers.</returns>
        protected virtual VRTK4_VRInputModule SetEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = VRTK4_EventSystem.Instance;
            }

            if (eventSystem == null)
            {
                Debug.LogError(
                    string.Format("{0} REQUIRED_COMPONENT_MISSING_FROM_SCENE {1}", nameof(VRTK4_UIPointer),
                        "EventSystem"),
                    gameObject);
                return null;
            }

            var eventSystemGameObject = eventSystem.gameObject;
            if (eventSystem.GetType() != typeof(VRTK4_EventSystem))
            {
                // remove old system, put vrtk 4
                var listOfTypes = new List<System.Type>();
                foreach (var baseInputModule in eventSystemGameObject.GetComponents<BaseInputModule>())
                {
                    listOfTypes.Add(baseInputModule.GetType());
                    baseInputModule.enabled = false;
                    GameObject.DestroyImmediate(baseInputModule);
                }

                eventSystem.enabled = false;
                GameObject.DestroyImmediate(eventSystem);
                var eventSystemTyped = VRTK4_EventSystem.Instance;
                if (eventSystemTyped == null)
                {
                    eventSystemGameObject.AddComponent<VRTK4_EventSystem>();
                }

                foreach (var itemType in listOfTypes)
                {
                    eventSystemGameObject.AddComponent(itemType);
                }
            }

            VRTK4_VRInputModule needed = VRTK4_EventSystem.Instance.VRInputModule;
            if (needed == null)
            {
                needed = eventSystemGameObject.GetComponent<VRTK4_VRInputModule>();
                if (needed == null)
                {
                    needed = eventSystemGameObject.AddComponent<VRTK4_VRInputModule>();
                }

                VRTK4_EventSystem.Instance.InitializeWithVRModule(needed);
            }

            return needed;
        }

        /// <summary>
        /// The RemoveEventSystem resets the Unity EventSystem back to the original state before the VRTK_VRInputModule was swapped for it.
        /// </summary>
        public virtual void RemoveEventSystem()
        {
            VRTK4_EventSystem vrtk4EventSystem = FindObjectOfType<VRTK4_EventSystem>();

            if (vrtk4EventSystem == null)
            {
                Debug.LogError(
                    string.Format("{0} REQUIRED_COMPONENT_MISSING_FROM_SCENE {1}", nameof(VRTK4_UIPointer),
                        "EventSystem"),
                    gameObject);
                return;
            }

            Destroy(vrtk4EventSystem);
        }

        /// <summary>
        /// The PointerActive method determines if the ui pointer beam should be active based on whether the pointer alias is being held and whether the Hold Button To Use parameter is checked.
        /// </summary>
        /// <returns>Returns `true` if the ui pointer should be currently active.</returns>
        public virtual bool PointerActive()
        {
            bool isActive = IsActivationButtonPressed();
            if (isActive)
            {
                return true;
            }

            pointerClicked = false;
            if (IsActivationButtonPressed() && !lastPointerPressState)
            {
                pointerClicked = true;
            }

            lastPointerPressState = activationButton.Value;

            if (pointerClicked)
            {
                beamEnabledState = !beamEnabledState;
            }

            return beamEnabledState;
        }

        /// <summary>
        /// The IsActivationButtonPressed method is used to determine if the configured activation button is currently in the active state.
        /// </summary>
        /// <returns>Returns `true` if the activation button is active.</returns>
        public virtual bool IsActivationButtonPressed()
        {
            return activationButton.Value;
        }

        /// <summary>
        /// The IsSelectionButtonPressed method is used to determine if the configured selection button is currently in the active state.
        /// </summary>
        /// <returns>Returns `true` if the selection button is active.</returns>
        public virtual bool IsSelectionButtonPressed()
        {
            return selectionButton.Value;
        }

        /// <summary>
        /// The ValidClick method determines if the UI Click button is in a valid state to register a click action.
        /// </summary>
        /// <param name="checkLastClick">If this is true then the last frame's state of the UI Click button is also checked to see if a valid click has happened.</param>
        /// <param name="lastClickState">This determines what the last frame's state of the UI Click button should be in for it to be a valid click.</param>
        /// <returns>Returns `true` if the UI Click button is in a valid state to action a click, returns `false` if it is not in a valid state.</returns>
        public virtual bool ValidClick(bool checkLastClick, bool lastClickState = false)
        {
            bool controllerClicked =
                (collisionClick ? collisionClick : IsSelectionButtonPressed());
            bool result = (checkLastClick
                ? controllerClicked && lastPointerClickState == lastClickState
                : controllerClicked);
            lastPointerClickState = controllerClicked;
            return result;
        }

        /// <summary>
        /// The GetOriginPosition method returns the relevant transform position for the pointer based on whether the pointerOriginTransform variable is valid.
        /// </summary>
        /// <returns>A Vector3 of the pointer transform position</returns>
        public virtual Vector3 GetOriginPosition()
        {
            return (customOrigin != null ? customOrigin : transform).position;
        }

        /// <summary>
        /// The GetOriginPosition method returns the relevant transform forward for the pointer based on whether the pointerOriginTransform variable is valid.
        /// </summary>
        /// <returns>A Vector3 of the pointer transform forward</returns>
        public virtual Vector3 GetOriginForward()
        {
            return (customOrigin != null ? customOrigin : transform).forward;
        }

        protected virtual void OnEnable()
        {
            VrtkUiPointers.Add(this);
            customOrigin = (customOrigin == null ? transform : customOrigin);
            ConfigureEventSystem();
            pointerClicked = false;
            lastPointerPressState = false;
            lastPointerClickState = false;
            beamEnabledState = false;

            activationButton.ValueChanged.AddListener(OnActiveButton);
            selectionButton.ValueChanged.AddListener(OnSelectionButton);
        }

        private void OnActiveButton(bool newValue)
        {
            if (newValue)
            {
                OnActivationButtonPressed();
            }
            else
            {
                OnActivationButtonReleased();
            }
        }

        private void OnSelectionButton(bool newValue)
        {
            if (newValue)
            {
                OnSelectionButtonPressed();
            }
            else
            {
                OnSelectionButtonReleased();
            }
        }

        protected virtual void OnDisable()
        {
            VrtkUiPointers.Remove(this);

            if (cachedVRInputModule)
            {
                cachedVRInputModule.Pointers.Remove(this);
            }
        }

        protected virtual void LateUpdate()
        {
            if (pointerEventData == null)
            {
                pointerEventData = new PointerEventData(EventSystem.current);
            }

            pointerEventData.pointerId = GetIndexOfUIPointer();
            VRTK4_SharedMethods.AddDictionaryValue(pointerLengths, pointerEventData.pointerId, maximumLength, true);
        }

        protected virtual void ResetHoverTimer()
        {
            hoverDurationTimer = 0f;
            canClickOnHover = false;
        }

        protected virtual void ConfigureEventSystem()
        {
            if (cachedVRInputModule == null)
            {
                cachedVRInputModule = SetEventSystem();
            }

            if (VRTK4_EventSystem.Instance != null && cachedVRInputModule != null)
            {
                if (pointerEventData == null)
                {
                    pointerEventData = new PointerEventData(VRTK4_EventSystem.Instance);
                }

                if (!cachedVRInputModule.Pointers.Contains(this))
                {
                    cachedVRInputModule.Pointers.Add(this);
                }
            }
        }
    }
}