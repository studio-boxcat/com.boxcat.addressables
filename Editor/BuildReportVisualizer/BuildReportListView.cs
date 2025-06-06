#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bundles.Editor
{
    [Serializable]
    internal class BuildReportListView
    {
        private BuildReportWindow m_Window;
        private ListView m_ListView;

        private VisualTreeAsset m_ReportListItemTreeAsset;

        [SerializeField]
        private List<BuildReportListItem> m_BuildReportItems = new List<BuildReportListItem>();

        [Serializable]
        internal class BuildReportListItem
        {
            public int Id { get; }
            public string FilePath { get; }
            public BuildLayout Layout { get; set; }

            public BuildReportListItem(int id, string filePath, BuildLayout layout)
            {
                Id = id;
                FilePath = filePath;
                Layout = layout;
            }
        }

        public BuildReportListView(BuildReportWindow window, VisualTreeAsset reportListItemTreeAsset)
        {
            m_Window = window;
            m_ReportListItemTreeAsset = reportListItemTreeAsset;
        }

        public void CreateGUI(VisualElement rootVisualElement)
        {
            m_BuildReportItems.Clear();

            for (int i = 0; i < ProjectConfigData.BuildReportFilePaths.Count; i++)
            {
                BuildLayout layout = null;
                string path = ProjectConfigData.BuildReportFilePaths[i];
                if (File.Exists(path))
                {
                    layout = BuildLayout.Open(path);
                }

                m_BuildReportItems.Insert(0, new BuildReportListItem(i, path, layout));
            }


            UQueryBuilder<ListView> listQuery = rootVisualElement.Query<ListView>(name: BuildReportUtility.ReportsList);
            m_ListView = listQuery.First();

            m_ListView.makeItem = () =>
            {
                var item = GUIUtility.Clone(m_ReportListItemTreeAsset);
                item.Q<VisualElement>(BuildReportUtility.ReportsListItemContainerLefthandElements).style.marginTop = new StyleLength(new Length(2f, LengthUnit.Pixel));
                item.Q<VisualElement>(BuildReportUtility.ReportsListItemContainerRighthandElements).style.marginTop = new StyleLength(new Length(2f, LengthUnit.Pixel));
                item.style.unityTextAlign = TextAnchor.MiddleCenter;
                return item;
            };
            m_ListView.bindItem = (e, i) => CreateItem(e, i);
            m_ListView.itemsSource = m_BuildReportItems;
            m_ListView.selectionChanged -= items => OnItemSelected(items);
            m_ListView.selectionChanged += items => OnItemSelected(items);
        }

        private static BuildLayout LoadLayout(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                BuildLayout layout = JsonUtility.FromJson<BuildLayout>(json);
                return layout;
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to read BuildReport from {filePath}, with Exception: {e}");
                throw;
            }
        }

        private void CreateItem(VisualElement element, int index)
        {
            BuildReportListItem reportListItem = m_BuildReportItems[index];
            var buildStatusImage = element.Q<Image>(BuildReportUtility.ReportsListItemBuildStatus);
            var buildPlatformLabel = element.Q<Label>(BuildReportUtility.ReportsListItemBuildPlatform);
            var buildTimeStampLabel = element.Q<Label>(BuildReportUtility.ReportsListItemBuildTimestamp);
            var buildDurationLabel = element.Q<Label>(BuildReportUtility.ReportsListItemBuildDuration);

            if (reportListItem.Layout == null)
            {
                buildStatusImage.image = EditorGUIUtility.IconContent("CollabError").image as Texture2D;
                buildTimeStampLabel.text = $"Cannot read file";
            }
            else
            {
                buildStatusImage.image = string.IsNullOrEmpty(reportListItem.Layout.BuildError) ?
                    EditorGUIUtility.IconContent("CollabNew").image as Texture2D :
                    EditorGUIUtility.IconContent("CollabError").image as Texture2D;
                buildPlatformLabel.text = reportListItem.Layout.BuildTarget.ToString();
                buildTimeStampLabel.text = BuildReportUtility.TimeAgo.GetString(reportListItem.Layout.BuildStart);
                buildDurationLabel.text = TimeSpan.FromSeconds(reportListItem.Layout.Duration).ToString("g");
            }

            element.AddManipulator(new ContextualMenuManipulator((evt) =>
            {
                evt.menu.AppendAction("Remove Report", (x) => RemoveReport(x), DropdownMenuAction.AlwaysEnabled, index);
                evt.menu.AppendAction("Remove All Reports", (x) => RemoveAllReports(x), DropdownMenuAction.AlwaysEnabled);
            }));
        }

        private void OnItemSelected(IEnumerable<object> items)
        {
            var item = items.First() as BuildReportListItem;
            int index = m_BuildReportItems.IndexOf(item);

            if (m_BuildReportItems[index].Layout == null)
            {
                Debug.LogError($"Unable to read '{m_BuildReportItems[index].FilePath}'");
                m_Window.ClearViews();
            }
            else
            {
                m_Window.Consume(LoadLayout(m_BuildReportItems[index].FilePath));
            }
        }

        internal void LoadNewestReport()
        {
            if (m_BuildReportItems.Count > 0)
            {
                if (m_BuildReportItems[0].Layout.ReadFull())
                    m_Window.Consume(m_BuildReportItems[0].Layout);
                else
                    Debug.LogWarning($"Unable to load build report at {m_BuildReportItems[0].FilePath}.");
            }
        }

        internal void AddReport(string filePath, BuildLayout layout)
        {
            BuildReportListItem item = m_BuildReportItems.Find(x => x.FilePath == filePath);
            if (item == null)
                m_BuildReportItems.Insert(0, new BuildReportListItem(m_BuildReportItems.Count, filePath, layout));
            else
                item.Layout = layout;

            if (m_ListView != null)
                m_ListView.Rebuild();
        }

        internal void AddReportFromFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath) && Path.GetExtension(filePath).ToLower() == ".json")
            {
                AddReportFromFile(filePath, m_ListView, true, true);
            }
        }

        private void AddReportFromFile(string filePath, ListView listView, bool logWarning, bool shouldRebuild)
        {
            string parsedFilePath = filePath.Replace("\\", "/");
            if (!ProjectConfigData.BuildReportFilePaths.Contains(parsedFilePath))
            {
                var layout = LoadLayout(filePath); // can consider adding error logs when file fails to load
                if (layout != null)
                {
                    ProjectConfigData.AddBuildReportFilePath(parsedFilePath);
                    m_BuildReportItems.Insert(0, new BuildReportListItem(m_BuildReportItems.Count, filePath, layout));

                    if (listView != null && shouldRebuild)
                        listView.Rebuild();
                }
                else
                {
                    L.E($"Failed to load build report at '{parsedFilePath}'");
                }
            }
            else if (logWarning)
                Debug.LogWarning($"Already added build report at '{parsedFilePath}'");
        }

        internal void AddReportsFromFolder(string filePath)
        {
            AddReportsFromFolder(filePath, m_ListView, true);
        }


        // Only rebuild when adding a bunch of files at once
        internal void AddReportsFromFolder(string folderPath, ListView listView, bool logWarning)
        {
            if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
            {
                foreach (string file in Directory.EnumerateFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly))
                {
                    AddReportFromFile(file, listView, logWarning, false);
                }
            }

            listView.Rebuild();
        }

        internal void RemoveReport(DropdownMenuAction action)
        {
            int index = (int)action.userData;
            ProjectConfigData.RemoveBuildReportFilePathAtIndex(index);
            m_BuildReportItems.RemoveAt(index);

            m_Window.ClearViews();
            m_ListView.Rebuild();
        }

        internal void RemoveAllReports(DropdownMenuAction action)
        {
            ProjectConfigData.ClearBuildReportFilePaths();
            m_BuildReportItems.Clear();

            m_Window.ClearViews();
            m_ListView.Rebuild();
        }
    }

}
#endif
