using UnityEditor;

namespace NhemBootstrap.Editor.Config {
    /// <summary>Persists the selected <see cref="ConfigProfile"/> between Unity Editor sessions using EditorPrefs.</summary>
    public static class ProfilePersistence {
        private const string PrefKey = "NhemBootstrap.SelectedProfile";

        /// <summary>Saves the selected profile to EditorPrefs.</summary>
        /// <param name="profile">The profile to persist.</param>
        public static void Save(ConfigProfile profile) {
            EditorPrefs.SetInt(PrefKey, (int)profile);
        }

        /// <summary>Loads the previously saved profile from EditorPrefs.</summary>
        /// <returns>The saved profile, or <see cref="ConfigProfile.Custom"/> if no value has been saved.</returns>
        public static ConfigProfile Load() {
            int value = EditorPrefs.GetInt(PrefKey, (int)ConfigProfile.Custom);
            return (ConfigProfile)value;
        }

        /// <summary>Clears the saved profile from EditorPrefs.</summary>
        public static void Clear() {
            EditorPrefs.DeleteKey(PrefKey);
        }
    }
}
