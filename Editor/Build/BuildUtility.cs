using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// Utility class for the Addressables Build Content process.
    /// </summary>
    public static class BuildUtility
    {
        public const string BuiltInShaderBundle = "UnityBuiltInShaders";

        static HashSet<string> _editorAssemblyNameCache = null;
        static HashSet<string> _editorAssemblyName => _editorAssemblyNameCache ??= CompilationPipeline.GetAssemblies(AssembliesType.Editor)
            .Select(a => a.name).ToHashSet();

        static Dictionary<System.Reflection.Assembly, bool> _editorAssemblyCache = new();

        /// <summary>
        /// Determines if the given assembly is an editor assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>Returns true if the assembly is an editor assembly. Returns false otherwise.</returns>
        static bool IsEditorAssembly(System.Reflection.Assembly assembly)
        {
            if (_editorAssemblyCache.TryGetValue(assembly, out var isEditor))
                return isEditor;
            var assemblyName = assembly.GetName().Name;
            isEditor = _editorAssemblyName.Remove(assemblyName);
            return _editorAssemblyCache[assembly] = isEditor;
        }

        static readonly Dictionary<Type, bool> _editorTypes = new();

        public static bool IsEditorType(Type type)
        {
            if (_editorTypes.TryGetValue(type, out var isEditor))
                return isEditor;
            isEditor = IsEditorAssembly(type.Assembly);
            return _editorTypes[type] = isEditor;
        }

        /// <summary>
        /// Used during the build to check for unsaved scenes and provide a user popup if there are any.
        /// </summary>
        /// <returns>True if there were no unsaved scenes, or if user hits "Save and Continue" on popup.
        /// False if any scenes were unsaved, and user hits "Cancel" on popup.</returns>
        public static bool CheckModifiedScenesAndAskToSave()
        {
            var dirtyScenes = new List<Scene>();
            for (var i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty) dirtyScenes.Add(scene);
            }
            if (dirtyScenes.Count == 0)
                return true;

            if (EditorUtility.DisplayDialog(
                    "Unsaved Scenes",
                    "Modified Scenes must be saved to continue.",
                    "Save and Continue", "Cancel") is false)
            {
                return false;
            }

            EditorSceneManager.SaveScenes(dirtyScenes.ToArray());
            return true;
        }
    }
}