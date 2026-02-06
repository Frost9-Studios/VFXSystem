using System.Collections.Generic;
using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// Utility for warning once per unique key.
    /// </summary>
    public static class WarnOnceLogger
    {
        private static readonly HashSet<string> EmittedWarnings = new HashSet<string>();

        /// <summary>
        /// Logs warning text once per key.
        /// </summary>
        /// <param name="key">Unique warning key.</param>
        /// <param name="message">Warning message.</param>
        public static void Log(string key, string message)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning(message);
                return;
            }

            lock (EmittedWarnings)
            {
                if (!EmittedWarnings.Add(key))
                {
                    return;
                }
            }

            Debug.LogWarning(message);
        }

        /// <summary>
        /// Clears tracked warning keys.
        /// </summary>
        public static void Clear()
        {
            lock (EmittedWarnings)
            {
                EmittedWarnings.Clear();
            }
        }
    }
}
