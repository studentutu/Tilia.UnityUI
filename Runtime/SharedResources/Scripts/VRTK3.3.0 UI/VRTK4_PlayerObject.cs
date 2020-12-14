//====================================================================================
//
// Purpose: Provide a way of tagging game objects as player specific objects to
// allow other scripts to identify these specific objects without needing to use tags
// or without needing to append the name of the game object. 
//
//====================================================================================
namespace Tilia.VRTKUI
{
    using UnityEngine;
    
    /// <summary>
    /// Required in order to get the reference for Pointer and propagate it to VRTK4_VR_InputModule
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VRTK4_PlayerObject : MonoBehaviour
    {
        [SerializeField] private VRTK4_UIPointer pointerReference;

        /// <summary>
        /// The type of object associated to the player.
        /// </summary>
        public enum ObjectTypes
        {
            /// <summary>
            /// No defined object.
            /// </summary>
            Null,
            /// <summary>
            /// An object that represents a virtual pointer.
            /// </summary>
            Pointer
        }

        public ObjectTypes objectType;

        /// <summary>
        /// The SetPlayerObject method tags the given game object with a special player object class for easier identification.
        /// </summary>
        /// <param name="obj">The game object to add the player object class to.</param>
        /// <param name="objType">The type of player object that is to be assigned.</param>
        public static void SetPlayerObject(GameObject obj, ObjectTypes objType)
        {
            VRTK4_PlayerObject currentPlayerObject = obj.GetComponent<VRTK4_PlayerObject>();
            if (currentPlayerObject == null)
            {
                currentPlayerObject = obj.AddComponent<VRTK4_PlayerObject>();
            }
            currentPlayerObject.objectType = objType;
        }

        /// <summary>
        /// The IsPlayerObject method determines if the given game object is a player object and can also check if it's of a specific type.
        /// </summary>
        /// <param name="obj">The GameObjet to check if it's a player object.</param>
        /// <param name="ofType">An optional ObjectType to check if the given GameObject is of a specific player object.</param>
        /// <returns>Returns true if the object is a player object with the optional given type.</returns>
        public static bool IsPlayerObject(GameObject obj, ObjectTypes ofType = ObjectTypes.Null)
        {
            VRTK4_PlayerObject[] playerObjects = obj.GetComponentsInParent<VRTK4_PlayerObject>(true);
            for (int i = 0; i < playerObjects.Length; i++)
            {
                if (ofType == ObjectTypes.Null || ofType == playerObjects[i].objectType)
                {
                    return true;
                }
            }
            return false;
        }
        
        public VRTK4_UIPointer GetPointer()
        {
            return pointerReference;
        }
    }
}