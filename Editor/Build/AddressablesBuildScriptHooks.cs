using UnityEngine.AddressableAssets.Util;

namespace UnityEditor.AddressableAssets.Build
{
    static class AddressablesBuildScriptHooks
    {
        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.playModeStateChanged += OnEditorPlayModeChanged;
        }

        static void OnEditorPlayModeChanged(PlayModeStateChange state)
        {
            // Build data only when exiting edit mode.
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            var catalog = AddressableCatalog.Default;
            if (catalog == null)
                return;

            var builder = DataBuilderList.Editor;
            L.I("[Addressables] BuildScriptHooks: " + builder.Name);

            var res = builder.BuildData(catalog, BuildTarget.NoTarget);
            if (!string.IsNullOrEmpty(res.Error))
            {
                L.E(res.Error);
                EditorApplication.isPlaying = false;
            }
        }
    }
}