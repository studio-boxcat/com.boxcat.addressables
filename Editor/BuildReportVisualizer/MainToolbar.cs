#if UNITY_2022_2_OR_NEWER
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bundles.Editor
{
    internal class MainToolbar
    {
        private BuildReportListView m_ReportsList;

        private bool m_LeftPaneCollapsed = false;
        private bool m_RightPaneCollapse = false;

        internal MainToolbar(BuildReportListView reportsList)
        {
            m_ReportsList = reportsList;
        }

        public void CreateGUI(VisualElement rootVisualElement)
        {
            var mainToolbar = rootVisualElement.Q<UnityEditor.UIElements.Toolbar>(BuildReportUtility.MainToolbar);

            //var themeStyle = AssetDatabase.LoadAssetAtPath(EditorGUIUtility.isProSkin ? BuildReportUtility.MainToolbarButtonsDarkUssPath : BuildReportUtility.MainToolbarButtonsLightUssPath, typeof(StyleSheet)) as StyleSheet;
            //mainToolbar.styleSheets.Add(themeStyle);
            var mainToolbarStyle = AssetDatabase.LoadAssetAtPath(BuildReportUtility.MainToolbarButtonsUssPath, typeof(StyleSheet)) as StyleSheet;
            mainToolbar.styleSheets.Add(mainToolbarStyle);

            Texture2D inspectorIcon = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_UnityEditor.InspectorWindow@2x" : "UnityEditor.InspectorWindow@2x").image as Texture2D;
            Texture2D sidebarIcon = BuildReportUtility.GetIcon(EditorGUIUtility.isProSkin ? BuildReportUtility.SideBarDark : BuildReportUtility.SideBarLight) as Texture2D;

            var mainToolbarCollapseLeftPaneButton = rootVisualElement.Q<ToolbarButton>(BuildReportUtility.MainToolbarCollapseLeftPaneButton);
            var mainToolbarCollapseLeftPaneButtonIcon = mainToolbarCollapseLeftPaneButton.Q<Image>(BuildReportUtility.MainToolbarCollapseLeftPaneButtonIcon);
            mainToolbarCollapseLeftPaneButtonIcon.image = sidebarIcon;
            var leftMiddlePaneSplitter = rootVisualElement.Q<TwoPaneSplitView>(BuildReportUtility.LeftMiddlePaneSplitter);
            mainToolbarCollapseLeftPaneButton.clicked += () =>
            {
                if (m_LeftPaneCollapsed)
                {
                    leftMiddlePaneSplitter.UnCollapse();
                    mainToolbarCollapseLeftPaneButtonIcon.image = sidebarIcon;
                }
                else
                {
                    leftMiddlePaneSplitter.CollapseChild(0);
                    mainToolbarCollapseLeftPaneButtonIcon.image = sidebarIcon;
                }
                m_LeftPaneCollapsed = !m_LeftPaneCollapsed;
            };

            var mainToolbarCollapseRightPaneButton = rootVisualElement.Q<ToolbarButton>(BuildReportUtility.MainToolbarCollapseRightPaneButton);
            var mainToolbarCollapseRightPaneButtonIcon = mainToolbarCollapseRightPaneButton.Q<Image>(BuildReportUtility.MainToolbarCollapseRightPaneButtonIcon);
            mainToolbarCollapseRightPaneButtonIcon.image = inspectorIcon;
            var middleRightPaneSplitter = rootVisualElement.Q<TwoPaneSplitView>(BuildReportUtility.MiddleRightPaneSplitter);
            mainToolbarCollapseRightPaneButton.clicked += () =>
            {
                if (m_RightPaneCollapse)
                {
                    middleRightPaneSplitter.UnCollapse();
                    mainToolbarCollapseRightPaneButtonIcon.image = inspectorIcon;

                }
                else
                {
                    middleRightPaneSplitter.CollapseChild(1);
                    mainToolbarCollapseRightPaneButtonIcon.image = inspectorIcon;
                }
                m_RightPaneCollapse = !m_RightPaneCollapse;
            };

            var mainToolbarAddReportButton = rootVisualElement.Q<ToolbarMenu>(BuildReportUtility.MainToolbarAddReportButton);
            mainToolbarAddReportButton.menu.AppendAction("Add Report...", x =>
            {
                var path = EditorUtility.OpenFilePanelWithFilters("Locate Build Report", Paths.BuildReportPath, new[] { "Build Report", "json" });
                m_ReportsList.AddReportFromFile(path);
            }, DropdownMenuAction.AlwaysEnabled);
            mainToolbarAddReportButton.menu.AppendAction("Add Reports from Folder...", x =>
            {
                var path = EditorUtility.OpenFolderPanel("Locate folder with Build Reports", Paths.BuildReportPath, "");
                m_ReportsList.AddReportsFromFolder(path);
            }, DropdownMenuAction.AlwaysEnabled);

            var mainToolbarAddReportButtonIcon = mainToolbarAddReportButton.Q<Image>(BuildReportUtility.MainToolbarAddReportButtonIcon);
            mainToolbarAddReportButtonIcon.image = EditorGUIUtility.IconContent("Toolbar Plus").image as Texture2D;
            mainToolbarAddReportButton.Insert(0, mainToolbarAddReportButtonIcon); // list the button icon first
        }
    }

}
#endif
