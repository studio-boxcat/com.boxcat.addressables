#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bundles.Editor
{
    internal abstract class ContentView : IBuildReportConsumer
    {
        protected BuildLayout m_Report;

        internal ToolbarSearchField m_SearchField;
        internal string m_SearchValue;

        protected MultiColumnTreeView m_TreeView = null;
        public MultiColumnTreeView ContentTreeView => m_TreeView;

        protected IList<IBundlesBuildReportItem> m_TreeItems = null;

        public struct TreeDataReportItem
        {
            public int Id;
            public IBundlesBuildReportItem ReportItem;

            public TreeDataReportItem(int id, IBundlesBuildReportItem reportItem)
            {
                Id = id;
                ReportItem = reportItem;
            }

        }
        protected Dictionary<Hash128, TreeDataReportItem> m_DataHashtoReportItem = null;
        public Dictionary<Hash128, TreeDataReportItem> DataHashtoReportItem
        {
            get { return m_DataHashtoReportItem; }
        }

        public Action<IEnumerable<object>> ItemsSelected;

        internal abstract ContentViewColumnData[] ColumnDataForView { get; }

        public abstract void Consume(BuildLayout buildReport);

        public abstract void CreateGUI(VisualElement rootVisualElement);

        public virtual void ClearGUI()
        {
            if (m_TreeView != null)
            {
                // Clear removes the column header
                // m_TreeView.Clear();
                m_TreeView.SetRootItems(default(IList<TreeViewItemData<IBundlesBuildReportItem>>));
                m_TreeView.Rebuild();
            }
        }

        internal BuildReportHelperConsumer m_HelperConsumer;
        private DetailsView m_DetailsView;
        private VisualTreeAsset m_TreeViewItem;
        private VisualTreeAsset m_TreeViewNavigableItem;

        internal ContentView(BuildReportHelperConsumer helperConsumer, DetailsView detailsView)
        {
            m_HelperConsumer = helperConsumer;
            m_DetailsView = detailsView;
            m_TreeViewItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildReportUtility.TreeViewItemFilePath);
            m_TreeViewNavigableItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildReportUtility.TreeViewNavigableItemFilePath);
        }

        public abstract IList<IBundlesBuildReportItem> CreateTreeViewItems(BuildLayout report);

        // Expresses bundle data as a flat list of TreeViewItemData objects.
        protected IList<TreeViewItemData<IBundlesBuildReportItem>> CreateTreeRootsFlatList(IList<IBundlesBuildReportItem> items, Dictionary<Hash128, TreeDataReportItem> dataHashToReportItem)
        {
            int id = 0;
            var roots = new List<TreeViewItemData<IBundlesBuildReportItem>>(items.Count);

            foreach (IBundlesBuildReportItem item in items)
            {
                dataHashToReportItem.Add(BuildReportUtility.ComputeDataHash(item.Name, ""), new TreeDataReportItem(id, item));
                roots.Add(new TreeViewItemData<IBundlesBuildReportItem>(id++, item));
            }
            return roots;
        }

        internal static void SetCallbacksForColumns(Columns columns, ContentViewColumnData[] columnNameToWidth)
        {
            foreach (ContentViewColumnData data in columnNameToWidth)
            {
                Column col = columns[data.Name];
                col.makeCell = () => new Label();
                col.bindCell = data.BindCellCallback;
                col.makeHeader = () => new Label();
                col.bindHeader = data.BindHeaderCallback;
            }
        }

        private IOrderedEnumerable<IBundlesBuildReportItem> OrderByType(string columnName, Type t)
        {
            if (t == typeof(int))
                return m_TreeItems.OrderBy(item => int.Parse(item.GetSortContent(columnName)));
            if (t == typeof(ulong))
                return m_TreeItems.OrderBy(item => ulong.Parse(item.GetSortContent(columnName)));
            return null;
        }

        private readonly Dictionary<string, Type> m_NumericColumnNames = new Dictionary<string, Type>()
        {
            {BuildReportUtility.AssetsContentViewColSizePlusRefs, typeof(ulong)},
            {BuildReportUtility.AssetsContentViewColSizeUncompressed, typeof(ulong)},
            {BuildReportUtility.AssetsContentViewColBundleSize, typeof(ulong)},
            {BuildReportUtility.AssetsContentViewColRefsBy, typeof(int)},
            {BuildReportUtility.AssetsContentViewColRefsTo, typeof(int)},
            {BuildReportUtility.BundlesContentViewColSizePlusRefs, typeof(ulong)},
            {BuildReportUtility.BundlesContentViewColSizeUncompressed, typeof(ulong)},
            {BuildReportUtility.BundlesContentViewBundleSize, typeof(ulong)},
            {BuildReportUtility.BundlesContentViewColRefsBy, typeof(int)},
            {BuildReportUtility.BundlesContentViewColRefsTo, typeof(int)},
            {BuildReportUtility.GroupsContentViewColSizePlusRefs, typeof(ulong)},
            {BuildReportUtility.GroupsContentViewColSizeUncompressed, typeof(ulong)},
            {BuildReportUtility.GroupsContentViewColBundleSize, typeof(ulong)},
            {BuildReportUtility.GroupsContentViewColRefsBy, typeof(int)},
            {BuildReportUtility.GroupsContentViewColRefsTo, typeof(int)},
            {BuildReportUtility.DuplicatedAssetsContentViewSpaceSaved, typeof(ulong)},
            {BuildReportUtility.DuplicatedAssetsContentViewDuplicationCount, typeof(int)},
            {BuildReportUtility.DuplicatedAssetsContentViewColSize, typeof(ulong)}
        };

        public void CreateTreeViewHeader(VisualElement element, string colName, bool isAssetColumn)
        {
            (element as Label).text = ContentTreeView.columns[colName].title;
            if (isAssetColumn)
                element.AddToClassList(BuildReportUtility.TreeViewAssetHeader);
            else
                element.AddToClassList(BuildReportUtility.TreeViewHeader);
        }

        public void CreateTreeViewCell(VisualElement element, int index, string colName, bool isNameColumn, Type type)
        {
            IBundlesBuildReportItem itemData = null;
            if (type == typeof(AssetsContentView))
               itemData = ContentTreeView.GetItemDataForIndex<AssetsViewBuildReportItem>(index);
            if (type == typeof(BundlesContentView))
                itemData = ContentTreeView.GetItemDataForIndex<BundlesViewBuildReportItem>(index);
            if (type == typeof(GroupsContentView))
                itemData = ContentTreeView.GetItemDataForIndex<GroupsViewBuildReportItem>(index);
            if (type == typeof(DuplicatedAssetsContentView))
                itemData = ContentTreeView.GetItemDataForIndex<DuplicatedAssetsViewBuildReportItem>(index);
            if (isNameColumn)
            {
                ShowEntryIcon(element, itemData, m_TreeViewItem, colName);
                element.AddToClassList(BuildReportUtility.TreeViewIconElement);
            }
            else
            {
                (element as Label).text = itemData.GetCellContent(colName);
                element.AddToClassList(BuildReportUtility.TreeViewElement);
            }
        }

        protected bool EntryAppearsInSearch(IBundlesBuildReportItem item, string searchValue)
        {
            if (string.IsNullOrEmpty(searchValue))
                return true;
            if (item.Name.ToLowerInvariant().Contains(searchValue))
                return true;
            return false;
        }

        public void ShowEntryIcon(VisualElement element, IBundlesBuildReportItem itemData, VisualTreeAsset baseItem, string colName)
        {
            (element as Label).text = string.Empty;
            element.Clear();

            VisualElement treeItem = GUIUtility.Clone(baseItem);
            var icon = treeItem.Q<Image>(BuildReportUtility.TreeViewItemIcon);
            var name = treeItem.Q<TextElement>(BuildReportUtility.TreeViewItemName);
            name.text = itemData.GetCellContent(colName);

            if (itemData is IBundlesBuildReportAsset asset)
            {
                string path = asset.ExplicitAsset == null ? asset.DataFromOtherAsset.AssetPath : asset.ExplicitAsset.AssetPath;
                Texture iconTexture = AssetDatabase.GetCachedIcon(path);
                if (iconTexture == null)
                    icon.AddToClassList(BuildReportUtility.TreeViewItemNoIcon);
                else
                    icon.image = iconTexture;

                if (asset.DataFromOtherAsset != null)
                    name.AddToClassList(BuildReportUtility.TreeViewImplicitAsset);
                if (asset is DuplicatedAssetsViewBuildReportDuplicatedAsset)
                    name.AddToClassList(BuildReportUtility.TreeViewDuplicatedAsset);
            }
            else if (itemData is GroupsViewBuildReportGroup group ||
                     itemData is BundlesViewBuildReportIndirectlyReferencedBundles ||
                     itemData is GroupsViewBuildReportIndirectlyReferencedBundles)
                icon.image = EditorGUIUtility.IconContent("d_FolderOpened Icon").image as Texture2D;
            else if (itemData is IBundlesBuildReportBundle)
                icon.image = EditorGUIUtility.IconContent("Package Manager").image as Texture2D;
            else
                icon.AddToClassList(BuildReportUtility.TreeViewItemNoIcon);

            name.AddManipulator(new ContextualMenuManipulator((ContextualMenuPopulateEvent evt) =>
            {
                evt.menu.AppendAction("Search in this window", (e) =>
                {
                    string newSearchValue = name.text;
                    m_SearchField.Q<TextField>().value = newSearchValue;
                });
            }));
        }

        public ContentView UseCachedView(VisualElement rootVisualElement)
        {
            VisualElement view = rootVisualElement.Q<VisualElement>(BuildReportUtility.ContentView);
            view.Add(m_TreeView);
            return this;
        }

        internal List<IBundlesBuildReportItem> SortByColumnDescription(SortColumnDescription col)
        {
            IOrderedEnumerable<IBundlesBuildReportItem> sortedTreeRootEnumerable;
            if (m_NumericColumnNames.ContainsKey(col.columnName))
            {
                Type t = m_NumericColumnNames[col.columnName];
                sortedTreeRootEnumerable = OrderByType(col.columnName, t);
            }
            else
            {
                sortedTreeRootEnumerable = m_TreeItems.OrderBy(item => item.GetSortContent(col.columnName));
            }

            List<IBundlesBuildReportItem> finalTreeRoots = new List<IBundlesBuildReportItem>(m_TreeItems.Count);
            foreach (var item in sortedTreeRootEnumerable)
                finalTreeRoots.Add(item);
            if (col.direction == SortDirection.Ascending)
                finalTreeRoots.Reverse();

            return finalTreeRoots;
        }
    }

    internal struct ContentViewColumnData
    {
        public string Name;
        public string Title;
        public Action<VisualElement, int> BindCellCallback;
        public Action<VisualElement> BindHeaderCallback;

        public ContentViewColumnData(string name, ContentView view, bool isNameColumn, string title = "N/a")
        {
            Name = name;
            Title = title;
            BindCellCallback = ((element, index) =>
            {
                view.CreateTreeViewCell(element, index, name, isNameColumn, view.GetType());
            });
            BindHeaderCallback = ((element) =>
            {
                view.CreateTreeViewHeader(element, name, isNameColumn);
            });
        }
    }

    // Nested interface that can be either a bundle or asset.
    public interface IBundlesBuildReportItem
    {
        public string Name { get; }

        void CreateGUI(VisualElement rootVisualElement);

        string GetCellContent(string colName);

        string GetSortContent(string colName);

    }

    internal interface IBundlesBuildReportBundle
    {
        public BuildLayout.Bundle Bundle { get; }
    }

    internal interface IBundlesBuildReportAsset
    {
        public BuildLayout.ExplicitAsset ExplicitAsset { get; }
        public BuildLayout.DataFromOtherAsset DataFromOtherAsset { get; }
        public List<BuildLayout.Bundle> Bundles { get; }
        public ulong SizeWDependencies { get; }
    }
}
#endif
