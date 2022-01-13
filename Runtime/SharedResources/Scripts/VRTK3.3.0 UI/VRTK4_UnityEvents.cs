namespace Tilia.VRTKUI.UnityEventHelper
{
    using UnityEngine;

    public abstract class VRTK4_UnityEvents<T> : MonoBehaviour where T : Component
    {
        private T component;

        protected abstract void AddListeners(T component);
        protected abstract void RemoveListeners(T component);

        protected virtual void OnEnable()
        {
            component = GetComponent<T>();

            if (component != null)
            {
                AddListeners(component);
            }
            else
            {
                string eventsScriptName = GetType().Name;
                string scriptName = typeof(T).Name;
                Debug.LogError(
                    string.Format("The {0} script requires to be attached to a GameObject that contains a {1} script.", eventsScriptName, scriptName),
                    gameObject);
            }
        }

        protected virtual void OnDisable()
        {
            if (component != null)
            {
                RemoveListeners(component);
            }
        }
    }
}