using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Bundles.Editor
{
    /// <summary>
    /// Rule class to check scene dependencies for duplicates
    /// </summary>
    internal class CheckSceneDupeDependencies : BundleRuleBase
    {
        /// <inheritdoc />
        public override string ruleName => "Check Scene to Bundles Duplicate Dependencies";

        /// <summary>
        /// Clear analysis and calculate built in resources and corresponding bundle dependencies for scenes
        /// </summary>
        /// <param name="catalog">The current Bundles catalog object</param>
        /// <returns>List of results from analysis</returns>
        public override List<AnalyzeResult> RefreshAnalysis(AssetCatalog catalog)
        {
            ClearAnalysis();

            string[] scenePaths = (from editorScene in EditorBuildSettings.scenes
                where editorScene.enabled
                select editorScene.path).ToArray();
            return CalculateBuiltInResourceDependenciesToBundleDependecies(catalog, scenePaths);
        }

        /// <inheritdoc />
        internal protected override string[] GetResourcePaths()
        {
            List<string> scenes = new List<string>(EditorBuildSettings.scenes.Length);
            foreach (EditorBuildSettingsScene settingsScene in EditorBuildSettings.scenes)
            {
                if (settingsScene.enabled)
                    scenes.Add(settingsScene.path);
            }

            return scenes.ToArray();
        }
    }
}
