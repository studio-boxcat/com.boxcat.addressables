#if UNITY_2022_2_OR_NEWER
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bundles.Editor
{
    internal class BuildReportWindow : EditorWindow, IBuildReportConsumer
    {
        [SerializeField]
        private VisualTreeAsset m_WindowTreeAsset;

        [SerializeField]
        private VisualTreeAsset m_ReportListItemTreeAsset;

        [SerializeField]
        private VisualTreeAsset m_DetailsPanelSummaryNavigableItemTreeAsset;

        [SerializeField]
        private VisualTreeAsset m_DetailsPanelSummaryNavigableBundleTreeAsset;

        internal ContentView m_ActiveContentView;

        private BuildReportListView m_ReportListView;
        private MainToolbar m_MainToolbar;
        private MainPanelSummaryTab m_MainPanelSummaryTab;
        private BuildReportHelperConsumer m_HelperConsumer;

        private bool m_Initialized = false;
        private static BuildLayout m_BuildReport;

        private VisualElement m_MainPanel;
        private VisualElement[] m_Tabs = new VisualElement[3];
        private Ribbon m_TabsRibbon;

        private int m_CurrentTab;
        internal string m_SearchValue = "";

        private DropdownField m_ContentViewTypeDropdown;
        private DropdownField m_PotentialIssuesViewTypeDropdown;
        private int m_PreviousContentDropDownValue;
        private int m_PreviousPotentialIssuesDropDownValue;

        public enum RibbonTabType
        {
            SummaryTab = 0,
            ContentTab,
            PotentialIssues
        }

        [SerializeField]
        private ContentViewType m_ActiveContentViewType = ContentViewType.BundleView;

        [SerializeField]
        private PotentialIssuesType m_ActivePotentialIssuesViewType = PotentialIssuesType.DuplicatedAssetsView;

        public enum ContentViewType
        {
            BundleView = 0,
            AssetsView,
            GroupsView
        }

        public enum PotentialIssuesType
        {
            DuplicatedAssetsView
        }

        private static List<string> s_ContentViewTypes = new List<string>()
        {
            "AssetBundles",
            "Assets",
            "Groups"
        };

        private static List<string> s_PotentialIssuesViewTypes = new List<string>()
        {
            "Duplicated Assets"
        };

        private DetailsView m_DetailsView;

        private void OnEnable()
        {
            m_BuildReport = null;

            if (m_ReportListView == null)
                m_ReportListView = new BuildReportListView(this, m_ReportListItemTreeAsset);

            if (m_MainToolbar == null)
                m_MainToolbar = new MainToolbar(m_ReportListView);

            if (m_DetailsView == null)
                m_DetailsView = new DetailsView(this);

            m_HelperConsumer = new BuildReportHelperConsumer();

            if (m_MainPanelSummaryTab == null)
                m_MainPanelSummaryTab = new MainPanelSummaryTab(this, m_HelperConsumer);

            m_ActiveContentView = new BundlesContentView(m_HelperConsumer, m_DetailsView);

            BuildLayoutGenerationTask.s_LayoutCompleteCallback = (path, layout) => m_ReportListView.AddReport(path, layout);
        }

        [Shortcut("Bundles/Bundles Report")]
        public static void ShowWindow()
        {
            // Opens the window, otherwise focuses it if it's already open.
            var window = GetWindow<BuildReportWindow>();

            // Adds a title to the window.
            window.titleContent = new GUIContent("Bundles Report");

            // Sets a minimum size to the window.
            window.minSize = new Vector2(280, 50);

            window.m_ReportListView.LoadNewestReport();
        }

        public void Consume(BuildLayout report)
        {
            m_BuildReport = report;
            if (!m_Initialized)
            {
                //Add default views to the window
                m_Initialized = true;
            }

            m_HelperConsumer.Consume(m_BuildReport);
            m_MainPanelSummaryTab.Consume(m_BuildReport);
            m_ActiveContentView?.Consume(m_BuildReport);
            m_CachedPotentialIssuesViews.Clear();
            m_CachedContentViews.Clear();
        }

        public void ClearViews()
        {
            m_MainPanelSummaryTab?.ClearGUI();
            m_ActiveContentView?.ClearGUI();
            m_DetailsView?.ClearGUI();
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            if (root == null)
                return;

            m_WindowTreeAsset.CloneTree(root);

            // Implement toolbar buttons
            m_MainToolbar.CreateGUI(root);

            // Create panels
            m_ReportListView.CreateGUI(root);
            m_ActiveContentView.CreateGUI(root);
            m_DetailsView.CreateGUI(root);
            m_MainPanelSummaryTab.CreateGUI(root);

            m_ActiveContentView.ItemsSelected += m_DetailsView.OnSelected;

            // Implement view type dropdown
            m_ContentViewTypeDropdown = rootVisualElement.Q<DropdownField>(BuildReportUtility.ContentViewTypeDropdown);
            m_ContentViewTypeDropdown.choices = s_ContentViewTypes;
            m_ContentViewTypeDropdown.index = (int)m_ActiveContentViewType;
            m_PreviousContentDropDownValue = m_ContentViewTypeDropdown.index;
            m_ContentViewTypeDropdown.RegisterValueChangedCallback(OnViewDropDownChanged);

            m_PreviousPotentialIssuesDropDownValue = 0;
            m_PotentialIssuesViewTypeDropdown = rootVisualElement.Q<DropdownField>(BuildReportUtility.PotentialIssuesDropdown);
            m_PotentialIssuesViewTypeDropdown.choices = s_PotentialIssuesViewTypes;
            m_PotentialIssuesViewTypeDropdown.RegisterValueChangedCallback(OnViewDropDownChanged);

            // Create tabbed views
            m_MainPanel = root.Q<VisualElement>(BuildReportUtility.MainPanel);
            m_TabsRibbon = root.Q<Ribbon>(BuildReportUtility.TabsRibbon);
            m_CurrentTab = m_TabsRibbon.InitialOption;
            m_Tabs[0] = root.Q<VisualElement>(BuildReportUtility.SummaryTab);
            m_Tabs[1] = root.Q<VisualElement>(BuildReportUtility.ContentTab);
            m_Tabs[2] = root.Q<VisualElement>(BuildReportUtility.PotentialIssuesTab);
            m_TabsRibbon.Clicked += (index) => ChangeTab(index);

            // Hide other tabs
            for (int i = 0; i < m_Tabs.Length; i++)
            {
                if (i != m_TabsRibbon.InitialOption)
                    m_Tabs[i].RemoveFromHierarchy();
            }
        }

        private void OnViewDropDownChanged(ChangeEvent<string> evt)
        {
            if(m_CurrentTab == (int)RibbonTabType.ContentTab)
                OnViewDropDownChanged(s_ContentViewTypes.IndexOf(evt.newValue));
            else if(m_CurrentTab == (int)RibbonTabType.PotentialIssues)
                OnViewDropDownChanged(s_PotentialIssuesViewTypes.IndexOf(evt.newValue));
        }

        private void OnViewDropDownChanged(int newIndex)
        {
            var treeView = m_ActiveContentView.ContentTreeView;
            if (treeView != null)
                treeView.RemoveFromHierarchy();

            if (m_CurrentTab == (int)RibbonTabType.ContentTab)
            {
                string prevSearchValue = m_ActiveContentView.m_SearchValue;
                m_PreviousContentDropDownValue = newIndex;
                m_ActiveContentViewType = (ContentViewType)newIndex;
                m_ActiveContentView = GetContentView(m_ActiveContentViewType);
                m_ActiveContentView.m_SearchField.Q<TextField>().value = prevSearchValue;
            }
            else if (m_CurrentTab == (int)RibbonTabType.PotentialIssues)
            {
                string prevSearchValue = m_ActiveContentView.m_SearchValue;
                m_ActivePotentialIssuesViewType = (PotentialIssuesType)newIndex;
                m_ActiveContentView = GetPotentialIssuesView(m_ActivePotentialIssuesViewType);
                m_ActiveContentView.m_SearchField.Q<TextField>().value = prevSearchValue;
            }
        }

        private Dictionary<ContentViewType, ContentView> m_CachedContentViews = new();
        private Dictionary<PotentialIssuesType, ContentView> m_CachedPotentialIssuesViews = new();

        private ContentView GetContentView(ContentViewType type)
        {
            if (m_CachedContentViews.ContainsKey(type))
                return m_CachedContentViews[type].UseCachedView(rootVisualElement);

            ContentView view = null;

            //Add more content views here
            switch(type)
            {
                case ContentViewType.AssetsView:
                    view = new AssetsContentView(m_HelperConsumer, m_DetailsView);
                    break;
                case ContentViewType.GroupsView:
                    view = new GroupsContentView(m_HelperConsumer, m_DetailsView);
                    break;
                case ContentViewType.BundleView:
                default:
                    view = new BundlesContentView(m_HelperConsumer, m_DetailsView);
                    break;

            }
            view.CreateGUI(rootVisualElement);
            view.Consume(m_BuildReport);
            view.ItemsSelected += m_DetailsView.OnSelected;
            m_CachedContentViews.Add(type, view);
            return view;
        }

        private ContentView GetPotentialIssuesView(PotentialIssuesType type)
        {
            if (m_CachedPotentialIssuesViews.ContainsKey(type))
                return m_CachedPotentialIssuesViews[type].UseCachedView(rootVisualElement);

            ContentView view = null;

            //Add more potential issues views here
            switch(type)
            {
                case PotentialIssuesType.DuplicatedAssetsView:
                default:
                    view = new DuplicatedAssetsContentView(m_HelperConsumer, m_DetailsView);
                    break;
            }

            view.CreateGUI(rootVisualElement);
            view.Consume(m_BuildReport);
            view.ItemsSelected += m_DetailsView.OnSelected;
            m_CachedPotentialIssuesViews.Add(type, view);
            return view;
        }

        private void ChangeTab(int index)
        {
            if (index == m_CurrentTab)
                return;
            if ((RibbonTabType)index == RibbonTabType.SummaryTab || (RibbonTabType)index == RibbonTabType.PotentialIssues)
                m_DetailsView.ClearGUI();

            m_Tabs[m_CurrentTab].RemoveFromHierarchy();
            m_MainPanel.Add(m_Tabs[index]);
            m_MainPanel.MarkDirtyRepaint();
            m_CurrentTab = index;

            if ((RibbonTabType)index == RibbonTabType.PotentialIssues)
                OnViewDropDownChanged(m_PreviousPotentialIssuesDropDownValue);
            else if ((RibbonTabType)index == RibbonTabType.ContentTab)
                OnViewDropDownChanged(m_PreviousContentDropDownValue);
        }

        public void NavigateToView(ContentViewType type)
        {
            int newIndex = (int)type;
            ChangeTab((int)RibbonTabType.ContentTab);
            m_TabsRibbon.ButtonClicked((int)RibbonTabType.ContentTab);
            m_ContentViewTypeDropdown.SetValueWithoutNotify(s_ContentViewTypes[newIndex]);
            OnViewDropDownChanged(newIndex);
        }

        public void NavigateToView(PotentialIssuesType type)
        {
            int newIndex = (int)type;
            ChangeTab((int)RibbonTabType.PotentialIssues);
            m_TabsRibbon.ButtonClicked((int)RibbonTabType.PotentialIssues);
            m_PotentialIssuesViewTypeDropdown.SetValueWithoutNotify(s_PotentialIssuesViewTypes[newIndex]);
            OnViewDropDownChanged(newIndex);
        }

        public IBundlesBuildReportItem SelectItemInView(Hash128 hash, bool expand = false)
        {
            ContentView.TreeDataReportItem item = m_ActiveContentView.DataHashtoReportItem[hash];
            m_ActiveContentView.ContentTreeView.SetSelectionById(item.Id);
            m_ActiveContentView.ContentTreeView.ScrollToItemById(item.Id);
            if (expand)
                m_ActiveContentView.ContentTreeView.ExpandItem(item.Id);

            m_DetailsView.OnSelected(new List<object>() { item.ReportItem });

            return item.ReportItem;
        }
    }
}
#endif
