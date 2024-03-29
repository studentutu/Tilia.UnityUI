﻿namespace Tilia.VRTKUI
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    using System.Collections;
    using System.Reflection;

    /// <summary>
    /// Denotes a Unity World UI Canvas can be interacted with a UIPointer script.
    /// </summary>
    /// <remarks>
    /// **Script Usage:**
    ///  * Place the `VRTK4_UICanvas` script on the Unity World UI Canvas to allow UIPointer interactions with.
    ///
    /// **Script Dependencies:**
    ///  * A UI Pointer attached to another GameObject (e.g. controller script alias) to interact with the UICanvas script.
    /// </remarks>
    public class VRTK4_UICanvas : MonoBehaviour
    {
        [Tooltip(
            "Determines if a UI Click action should happen when a UI Pointer game object collides with this canvas.")]
        public bool clickOnPointerCollision = false;

        [Tooltip(
            "Determines if a UI Pointer will be auto activated if a UI Pointer game object comes within the given distance of this canvas. If a value of `0` is given then no auto activation will occur." +
            " If used, prefer to add your predefined ACTIVATOR_FRONT_TRIGGER_GAMEOBJECT, otherwise new default one will be created. ")]
        public float autoActivateWithinDistance = 0f;

        [Tooltip(
            "If you need drag, pick and drop functionality on the canvas, enabled this. Note: scroll, drag and scroll works as is." +
            " If used, prefer to add your predefined CANVAS_DRAGGABLE_PANEL, otherwise new default one will be created. ")]
        public bool supportDragAndDropUI = false;

        protected BoxCollider canvasBoxCollider;
        protected Rigidbody canvasRigidBody;
        protected Coroutine draggablePanelCreation;
        protected const string CANVAS_DRAGGABLE_PANEL = "VRTK_UICANVAS_DRAGGABLE_PANEL";
        protected const string ACTIVATOR_FRONT_TRIGGER_GAMEOBJECT = "VRTK_UICANVAS_ACTIVATOR_FRONT_TRIGGER";

        protected virtual void OnEnable()
        {
            StartCoroutine(WaitOneFrame());
        }

        private IEnumerator WaitOneFrame()
        {
            yield return null;
            SetupCanvas();
        }

        protected virtual void OnDisable()
        {
            RemoveCanvas();
        }

        protected virtual void OnDestroy()
        {
            RemoveCanvas();
        }

        protected virtual void OnTriggerEnter(Collider currentCollider)
        {
            VRTK4_PlayerObject colliderCheck = currentCollider.GetComponentInParent<VRTK4_PlayerObject>();
            VRTK4_UIPointer pointerCheck = colliderCheck == null ? null : colliderCheck.GetPointer();
            if (pointerCheck != null && colliderCheck != null &&
                colliderCheck.objectType == VRTK4_PlayerObject.ObjectTypes.Pointer)
            {
                pointerCheck.collisionClick = clickOnPointerCollision;
            }
        }

        protected virtual void OnTriggerExit(Collider currentCollider)
        {
            VRTK4_PlayerObject colliderCheck = currentCollider.GetComponentInParent<VRTK4_PlayerObject>();
            VRTK4_UIPointer pointerCheck = colliderCheck == null ? null : colliderCheck.GetPointer();
            if (pointerCheck != null)
            {
                pointerCheck.collisionClick = false;
            }
        }

        protected virtual void SetupCanvas()
        {
            if (this == null || !isActiveAndEnabled || VRTK4_EventSystem.Instance == null)
            {
                return;
            }

            Canvas canvas = GetComponent<Canvas>();

            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
            {
                Debug.LogError(
                    string.Format("{0} REQUIRED_COMPONENT_MISSING_FROM_GAMEOBJECT {1} Make sure {2}",
                        nameof(VRTK4_UICanvas), gameObject.name, "that Canvas is set to `Render Mode = World Space`"),
                    gameObject);
                return;
            }

            RectTransform canvasRectTransform = canvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRectTransform.sizeDelta;
            //copy public params then disable existing graphic raycaster
            GraphicRaycaster defaultRaycaster = canvas.gameObject.GetComponent<GraphicRaycaster>();
            VRTK4_UIGraphicRaycaster customRaycaster =
                canvas.gameObject.GetComponent<VRTK4_UIGraphicRaycaster>();

            //if it doesn't already exist, add the custom raycaster
            if (customRaycaster == null)
            {
                customRaycaster = canvas.gameObject.AddComponent<VRTK4_UIGraphicRaycaster>();
            }

            if (defaultRaycaster != null && defaultRaycaster.enabled)
            {
                customRaycaster.ignoreReversedGraphics = defaultRaycaster.ignoreReversedGraphics;
                customRaycaster.blockingObjects = defaultRaycaster.blockingObjects;

                //Use Reflection to transfer the BlockingMask
                customRaycaster.GetType()
                    .GetField("m_BlockingMask",
                        BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
                        customRaycaster,
                        defaultRaycaster.GetType()
                            .GetField("m_BlockingMask", BindingFlags.Instance | BindingFlags.NonPublic)
                            .GetValue(defaultRaycaster));

                defaultRaycaster.enabled = false;
            }

            //add a box collider and background image to ensure the rays always hit
            if (canvas.gameObject.GetComponent<BoxCollider>() == null)
            {
                Vector2 pivot = canvasRectTransform.pivot;
                float zSize = 0.1f;
                float zScale = canvasRectTransform.localScale.z > 0 ? zSize / canvasRectTransform.localScale.z : 1;


                canvasBoxCollider = canvas.gameObject.AddComponent<BoxCollider>();
                canvasBoxCollider.size = new Vector3(canvasSize.x, canvasSize.y, zScale);
                canvasBoxCollider.center = new Vector3(canvasSize.x / 2 - canvasSize.x * pivot.x,
                    canvasSize.y / 2 - canvasSize.y * pivot.y, zScale / 2f);
                canvasBoxCollider.isTrigger = true;
                canvasBoxCollider.gameObject.layer = VRTK4_SharedMethods.UI_Layer;
            }

            if (canvas.gameObject.GetComponent<Rigidbody>() == null)
            {
                canvasRigidBody = canvas.gameObject.AddComponent<Rigidbody>();
                canvasRigidBody.isKinematic = true;
            }

            if (supportDragAndDropUI)
                draggablePanelCreation = StartCoroutine(CreateDraggablePanel(canvas, canvasSize));
            CreateActivator(canvas, canvasSize);
        }

        protected virtual IEnumerator CreateDraggablePanel(Canvas canvas, Vector2 canvasSize)
        {
            if (canvas != null && !canvas.transform.Find(CANVAS_DRAGGABLE_PANEL))
            {
                yield return null;

                GameObject draggablePanel = new GameObject(CANVAS_DRAGGABLE_PANEL, typeof(RectTransform));
                draggablePanel.AddComponent<LayoutElement>().ignoreLayout = true;
                draggablePanel.AddComponent<Image>().color = Color.clear;
                draggablePanel.AddComponent<EventTrigger>();
                draggablePanel.transform.SetParent(canvas.transform);
                draggablePanel.transform.localPosition = Vector3.zero;
                draggablePanel.transform.localRotation = Quaternion.identity;
                draggablePanel.transform.localScale = Vector3.one;
                draggablePanel.transform.SetAsFirstSibling();

                draggablePanel.GetComponent<RectTransform>().sizeDelta = canvasSize;
            }
        }

        protected virtual void CreateActivator(Canvas canvas, Vector2 canvasSize)
        {
            //if autoActivateWithinDistance is greater than 0 then create the front collider sub object
            if (autoActivateWithinDistance > 0f && canvas != null &&
                !canvas.transform.Find(ACTIVATOR_FRONT_TRIGGER_GAMEOBJECT))
            {
                var comp = canvas.gameObject.GetComponent<Rigidbody>();
                if (comp == null)
                {
                    comp = canvas.gameObject.AddComponent<Rigidbody>();
                }

                comp.isKinematic = true;
                var anoptherComp = canvas.gameObject.GetComponent<VRTK_UIPointerAutoActivator>();
                if (anoptherComp == null)
                {
                    canvas.gameObject.AddComponent<VRTK_UIPointerAutoActivator>();
                }

                RectTransform canvasRectTransform = canvas.GetComponent<RectTransform>();
                Vector2 pivot = canvasRectTransform.pivot;

                GameObject frontTrigger = new GameObject(ACTIVATOR_FRONT_TRIGGER_GAMEOBJECT);
                frontTrigger.transform.SetParent(canvas.transform);
                frontTrigger.transform.SetAsFirstSibling();
                frontTrigger.transform.localPosition = new Vector3(canvasSize.x / 2 - canvasSize.x * pivot.x,
                    canvasSize.y / 2 - canvasSize.y * pivot.y);
                frontTrigger.transform.localRotation = Quaternion.identity;
                frontTrigger.transform.localScale = Vector3.one;
                frontTrigger.layer = LayerMask.NameToLayer("Ignore Raycast");

                float actualActivationDistance = canvasRectTransform.localScale.z > 0
                    ? autoActivateWithinDistance / canvasRectTransform.localScale.z
                    : 1;
                BoxCollider boxColl = frontTrigger.AddComponent<BoxCollider>();
                boxColl.size = new Vector3(canvasSize.x, canvasSize.y, actualActivationDistance);
                boxColl.center = new Vector3(0f, 0f, -(actualActivationDistance / 2));
                boxColl.isTrigger = true;
                frontTrigger.AddComponent<VRTK_UIPointerAutoActivator>();
            }
        }

        protected virtual void RemoveCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();

            if (canvas == null)
            {
                return;
            }

            GraphicRaycaster defaultRaycaster = canvas.gameObject.GetComponent<GraphicRaycaster>();
            VRTK4_UIGraphicRaycaster customRaycaster = canvas.gameObject.GetComponent<VRTK4_UIGraphicRaycaster>();
            //if a custom raycaster exists then remove it
            if (customRaycaster != null)
            {
                Destroy(customRaycaster);
            }

            //If the default raycaster is disabled, then re-enable it
            if (defaultRaycaster != null && !defaultRaycaster.enabled)
            {
                defaultRaycaster.enabled = true;
            }

            //Check if there is a collider and remove it if there is
            if (canvasBoxCollider != null)
            {
                Destroy(canvasBoxCollider);
            }

            if (canvasRigidBody != null)
            {
                Destroy(canvasRigidBody);
            }

            if (draggablePanelCreation != null)
            {
                StopCoroutine(draggablePanelCreation);
            }

            Transform draggablePanel = canvas.transform.Find(CANVAS_DRAGGABLE_PANEL);
            if (draggablePanel != null)
            {
                Destroy(draggablePanel.gameObject);
            }

            Transform frontTrigger = canvas.transform.Find(ACTIVATOR_FRONT_TRIGGER_GAMEOBJECT);
            if (frontTrigger != null)
            {
                Destroy(frontTrigger.gameObject);
            }
        }
    }
}