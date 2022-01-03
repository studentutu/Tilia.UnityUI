// Shared Methods|Utilities|90060

using UnityEngine;
using UnityEngine.EventSystems;

namespace Tilia.VRTKUI
{
    using System.Collections.Generic;

    /// <summary>
    /// The Shared Methods script is a collection of reusable static methods that are used across a range of different scripts.
    /// </summary>
    public static class VRTK4_SharedMethods
    {
        /// <summary>
        /// The GetDictionaryValue method attempts to retrieve a value from a given dictionary for the given key. It removes the need for a double dictionary lookup to ensure the key is valid and has the option of also setting the missing key value to ensure the dictionary entry is valid.
        /// </summary>
        /// <typeparam name="TKey">The datatype for the dictionary key.</typeparam>
        /// <typeparam name="TValue">The datatype for the dictionary value.</typeparam>
        /// <param name="dictionary">The dictionary to retrieve the value from.</param>
        /// <param name="key">The key to retrieve the value for.</param>
        /// <param name="defaultValue">The value to utilise when either setting the missing key (if `setMissingKey` is `true`) or the default value to return when no key is found (if `setMissingKey` is `false`).</param>
        /// <param name="setMissingKey">If this is `true` and the given key is not present, then the dictionary value for the given key will be set to the `defaultValue` parameter. If this is `false` and the given key is not present then the `defaultValue` parameter will be returned as the value.</param>
        /// <returns>The found value for the given key in the given dictionary, or the default value if no key is found.</returns>
        public static TValue GetDictionaryValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key,
            TValue defaultValue = default(TValue), bool setMissingKey = false)
        {
            bool keyExists;
            return GetDictionaryValue(dictionary, key, out keyExists, defaultValue, setMissingKey);
        }

        /// <summary>
        /// The GetDictionaryValue method attempts to retrieve a value from a given dictionary for the given key. It removes the need for a double dictionary lookup to ensure the key is valid and has the option of also setting the missing key value to ensure the dictionary entry is valid.
        /// </summary>
        /// <typeparam name="TKey">The datatype for the dictionary key.</typeparam>
        /// <typeparam name="TValue">The datatype for the dictionary value.</typeparam>
        /// <param name="dictionary">The dictionary to retrieve the value from.</param>
        /// <param name="key">The key to retrieve the value for.</param>
        /// <param name="keyExists">Sets the given parameter to `true` if the key exists in the given dictionary or sets to `false` if the key didn't existing in the given dictionary.</param>
        /// <param name="defaultValue">The value to utilise when either setting the missing key (if `setMissingKey` is `true`) or the default value to return when no key is found (if `setMissingKey` is `false`).</param>
        /// <param name="setMissingKey">If this is `true` and the given key is not present, then the dictionary value for the given key will be set to the `defaultValue` parameter. If this is `false` and the given key is not present then the `defaultValue` parameter will be returned as the value.</param>
        /// <returns>The found value for the given key in the given dictionary, or the default value if no key is found.</returns>
        public static TValue GetDictionaryValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key,
            out bool keyExists, TValue defaultValue = default(TValue), bool setMissingKey = false)
        {
            keyExists = false;
            if (dictionary == null)
            {
                return defaultValue;
            }

            TValue outputValue;
            if (dictionary.TryGetValue(key, out outputValue))
            {
                keyExists = true;
            }
            else
            {
                if (setMissingKey)
                {
                    dictionary.Add(key, defaultValue);
                }

                outputValue = defaultValue;
            }

            return outputValue;
        }

        /// <summary>
        /// The AddDictionaryValue method attempts to add a value for the given key in the given dictionary if the key does not already exist. If `overwriteExisting` is `true` then it always set the value even if they key exists.
        /// </summary>
        /// <typeparam name="TKey">The datatype for the dictionary key.</typeparam>
        /// <typeparam name="TValue">The datatype for the dictionary value.</typeparam>
        /// <param name="dictionary">The dictionary to set the value for.</param>
        /// <param name="key">The key to set the value for.</param>
        /// <param name="value">The value to set at the given key in the given dictionary.</param>
        /// <param name="overwriteExisting">If this is `true` then the value for the given key will always be set to the provided value. If this is `false` then the value for the given key will only be set if the given key is not found in the given dictionary.</param>
        /// <returns>Returns `true` if the given value was successfully added to the dictionary at the given key. Returns `false` if the given key already existed in the dictionary and `overwriteExisting` is `false`.</returns>
        public static bool AddDictionaryValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue value,
            bool overwriteExisting = false)
        {
            if (dictionary != null)
            {
                if (overwriteExisting)
                {
                    dictionary[key] = value;
                    return true;
                }
                else
                {
                    bool keyExists;
                    GetDictionaryValue(dictionary, key, out keyExists, value, true);
                    return !keyExists;
                }
            }

            return false;
        }

        /// <summary>
        /// The AddListValue method adds the given value to the given list. If `preventDuplicates` is `true` then the given value will only be added if it doesn't already exist in the given list.
        /// </summary>
        /// <typeparam name="TValue">The datatype for the list value.</typeparam>
        /// <param name="list">The list to retrieve the value from.</param>
        /// <param name="value">The value to attempt to add to the list.</param>
        /// <param name="preventDuplicates">If this is `false` then the value provided will always be appended to the list. If this is `true` the value provided will only be added to the list if it doesn't already exist.</param>
        /// <returns>Returns `true` if the given value was successfully added to the list. Returns `false` if the given value already existed in the list and `preventDuplicates` is `true`.</returns>
        public static bool AddListValue<TValue>(List<TValue> list, TValue value, bool preventDuplicates = false)
        {
            if (list != null && (!preventDuplicates || !list.Contains(value)))
            {
                list.Add(value);
                return true;
            }

            return false;
        }

        public static string GetPath(Transform current)
        {
            if (current.parent == null)
                return "/" + current.name;
            return GetPath(current.parent) + "/" + current.name;
        }
    }
}