#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bundles.Editor
{
    internal class DetailsContentView
    {
        protected VisualTreeAsset m_DetailsContentDrillableListItem;
        protected List<DetailsListItem> m_ContentItems;

        protected Label m_ActiveContentsName;
        protected ListView m_ContentItemsListView;
        private BuildReportWindow m_Window;
        protected VisualElement m_ContentsPane;
        protected Image m_ActiveContentIcon;
        private VisualElement m_Toolbar;
        private ContextualMenuManipulator contextMenuManipulator;

        private const float m_ItemHeight = 24f;

        private RibbonButton m_RefToButton;
        private RibbonButton m_RefByButton;

        private Button m_BackButton;

        protected Dictionary<VisualElement, List<Action>> m_ButtonCallBackTracker = new Dictionary<VisualElement, List<Action>>();

        public DetailsContentView(VisualElement root, BuildReportWindow window)
        {
            m_Window = window;
            m_ContentsPane = root.Q<VisualElement>(BuildReportUtility.DetailsContentsList);
            m_ActiveContentsName = root.Q<Label>(BuildReportUtility.BreadcrumbToolbarName);
            m_ActiveContentIcon = root.Q<Image>(BuildReportUtility.BreadcrumbToolbarIcon);
            m_Toolbar = root.Q<VisualElement>(BuildReportUtility.BreadcrumbToolbar);
            var stylesheetPath = BuildReportUtility.GetDetailsViewStylesheetPath();
            m_Toolbar.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(stylesheetPath));

            m_ActiveContentsName.style.textOverflow = TextOverflow.Ellipsis;
            m_ActiveContentsName.style.overflow = Overflow.Hidden;
            m_ActiveContentsName.style.unityFontStyleAndWeight = FontStyle.Bold;
            contextMenuManipulator = GenerateContextMenu();
            m_ActiveContentsName.AddManipulator(contextMenuManipulator);

            m_DetailsContentDrillableListItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildReportUtility.DrillableListViewItemPath);

            m_RefByButton = root.Q<RibbonButton>(BuildReportUtility.ReferencedByTab);
            m_RefToButton = root.Q<RibbonButton>(BuildReportUtility.ReferencesToTab);

            m_ContentItems = new List<DetailsListItem>();

            m_ContentItemsListView = new ListView(m_ContentItems, m_ItemHeight, RefToMakeItem, RefToBindItem);
            m_ContentItemsListView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            m_ContentItemsListView.style.marginLeft = new StyleLength(new Length(4f, LengthUnit.Pixel));
            m_ContentItemsListView.visible = false;

            m_BackButton = root.Q<Button>(BuildReportUtility.BreadcrumbToolbarBackButton);
            BuildReportUtility.SetVisibility(m_BackButton, false);
            m_BackButton.style.backgroundImage = new StyleBackground(BuildReportUtility.GetIcon(BuildReportUtility.GetBackIconPath()) as Texture2D);
            m_BackButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_BackButton.clicked += () =>
            {
                DetailsStack.Pop();
            };

            m_ContentsPane.Add(m_ContentItemsListView);

            DetailsStack.OnPop += (item) =>
            {
                DisplayContents(item);
                RefreshBackButton();
            };

            DetailsStack.OnPush += (item) =>
            {
                RefreshBackButton();
            };
        }

        public void DisplayContents(object item, DetailsViewTab tab)
        {
            RefreshBackButton();

            DetailsContents contentsToDisplay = null;

            if (item is DetailsContents)
                contentsToDisplay = item as DetailsContents;
            else if (DetailsUtility.IsBundle(item))
            {
                var bundle = DetailsUtility.GetBundle(item);
                UpdateTabButtons(GetRefByCount(bundle), GetRefToCount(bundle));
                contentsToDisplay = GetContents(bundle, tab);
            }
            else
            {
                var asset = DetailsUtility.GetAsset(item);
                if (asset != null)
                {
                    UpdateTabButtons(GetRefByCount(asset), GetRefToCount(asset));
                    contentsToDisplay = GetContents(asset, tab);
                }
                else
                {
                    var otherAsset = DetailsUtility.GetOtherAssetData(item);
                    if (otherAsset != null)
                    {
                        UpdateTabButtons(GetRefByCount(otherAsset), GetRefToCount(otherAsset));
                        contentsToDisplay = GetContents(otherAsset, tab);
                    }
                }
            }

            DisplayContents(contentsToDisplay);
        }

        public void ClearContents()
        {
            m_ContentItems.Clear();
            m_ContentItemsListView.RefreshItems();
        }

        private DetailsContents m_ActiveContents;
        private void DisplayContents(DetailsContents contents)
        {
            ClearContents();

            m_ActiveContents = contents;
            RefreshBackButton();

            if (contents == null)
            {
                UpdateTabButtons(0,0);
                m_ContentItemsListView.RefreshItems();
                m_ContentItemsListView.visible = false;
                m_ActiveContentsName.text = "";
                m_ActiveContentIcon.image = null;
                return;
            }


            m_ActiveContentsName.text = contents.Title;
            m_ActiveContentIcon.image = BuildReportUtility.GetIcon(contents.AssetPath);
            if (contents.AssetPath != BuildReportUtility.GetAssetBundleIconPath())
            {
                m_ActiveContentsName.tooltip = "Asset Path: " + contents.AssetPath;
                m_ActiveContentsName.displayTooltipWhenElided = false;
            }
            else
            {
                m_ActiveContentsName.tooltip = null;
                m_ActiveContentsName.displayTooltipWhenElided = true;
            }


            foreach (var item in contents.DrillableItems)
                m_ContentItems.Add(item);

            m_ContentItemsListView.RefreshItems();
            m_ContentItemsListView.style.maxHeight = m_ContentItems.Count * m_ItemHeight;
            if (!m_ContentItemsListView.visible)
                m_ContentItemsListView.visible = true;

        }

        private ContextualMenuManipulator GenerateContextMenu()
        {
            var manip = new ContextualMenuManipulator((ContextualMenuPopulateEvent evt) =>
            {
                evt.menu.AppendAction("Search in this window", (e) =>
                {
                    string newSearchValue = m_ActiveContentsName.text;
                    string isInBundleString = "(in this Bundle)";
                    if (newSearchValue.EndsWith(isInBundleString))
                        newSearchValue = newSearchValue.Substring(0, newSearchValue.Length - isInBundleString.Length - 1);
                    m_Window.m_ActiveContentView.m_SearchField.Q<TextField>().value = newSearchValue;
                });
            });

            return manip;
        }

        private List<BuildLayout.ExplicitAsset> GetReferencingAssetsFromBundle(BuildLayout.Bundle referencingBundle, BuildLayout.Bundle bundle)
        {
            var referencingAssets = new List<BuildLayout.ExplicitAsset>();

            foreach (var bd in referencingBundle.BundleDependencies)
            {
                if (bd.DependencyBundle == bundle)
                {
                    foreach (var assetDep in bd.AssetDependencies)
                        referencingAssets.Add(assetDep.rootAsset);
                    return referencingAssets;
                }
            }

            return referencingAssets;
        }


        private DetailsContents GetContents(BuildLayout.Bundle bundle, DetailsViewTab tab)
        {
            DetailsContents value = new DetailsContents((string) bundle.Name, BuildReportUtility.GetAssetBundleIconPath());
            switch(tab)
            {
                case DetailsViewTab.ReferencedBy:
                    foreach (var referencingBundle in bundle.DependentBundles)
                    {
                        var referencingAssetList = GetReferencingAssetsFromBundle(referencingBundle, bundle);
                        if (referencingAssetList.Count > 0)
                        {
                            value.DrillableItems.Add(new DetailsListItem((string) referencingBundle.Name, BuildReportUtility.GetAssetBundleIconPath(), () => ShowAssetsThatLinkToBundle(DetailsViewTab.ReferencedBy,
                                referencingBundle,
                                GetReferencingAssetsFromBundle(referencingBundle, bundle)), BuildReportUtility.GetForwardIconPath()));
                        }
                        else
                        {
                            value.DrillableItems.Add(new DetailsListItem((string) referencingBundle.Name, BuildReportUtility.GetAssetBundleIconPath(), null, null));
                        }
                    }

                    if (value.DrillableItems.Count == 0)
                        value.DrillableItems.Add(new DetailsListItem($"No AssetBundles have this listed as a dependency", BuildReportUtility.GetAssetBundleIconPath(), null, null));

                    break;

                case DetailsViewTab.ReferencesTo:
                    foreach (var file in bundle.Files)
                    {
                        var idToIndexMap = new Dictionary<AssetId, int>();
                        int index = 0;
                        foreach (var asset in file.Assets)
                        {
                            Action drillDownEvent = null;
                            if (asset.ExternallyReferencedAssets.Count > 0)
                                drillDownEvent = () => ShowReferencesToForAsset(value, asset, true);

                            var drillableItem = new DetailsListItem(asset.GetName(), asset.AssetPath, drillDownEvent, drillDownEvent == null ? null : BuildReportUtility.GetForwardIconPath());
                            idToIndexMap.Add(asset.Guid, index++);
                            value.DrillableItems.Add(drillableItem);
                        }

                        foreach (var implicitAsset in file.OtherAssets)
                            value.DrillableItems.Add(new DetailsListItem(implicitAsset.AssetPath,
                                implicitAsset.AssetPath,
                                () =>
                                {
                                    List<int> referencingItems = new List<int>();
                                    foreach(var refAsset in implicitAsset.ReferencingAssets)
                                        referencingItems.Add(idToIndexMap[refAsset.Guid]);

                                    m_ContentItemsListView.SetSelection(referencingItems);
                                },BuildReportUtility.GetHelpIconPath(), FontStyle.Italic));
                    }
                    break;
            }

            return value;
        }

        private void ShowReferencesByForAsset(DetailsContents value, BuildLayout.ExplicitAsset asset, bool shouldCallDisplayContents)
        {
            if (shouldCallDisplayContents)
                value = new DetailsContents(asset.Address, asset.AssetPath);
            foreach (var refAsset in asset.ReferencingAssets)
            {
                if (refAsset.Bundle == asset.Bundle)
                    value.DrillableItems.Add(new DetailsListItem(refAsset.GetName(), refAsset.AssetPath, null, null));
                else
                {
                    value.DrillableItems.Add(new DetailsListItem((string) refAsset.Bundle.Name, BuildReportUtility.GetAssetBundleIconPath(), () => ShowAssetsThatLinkToBundle(DetailsViewTab.ReferencedBy,
                        refAsset.Bundle,
                        GetAssetsThatLinkFromBundleMap(asset.Bundle, refAsset)[refAsset.Bundle]), BuildReportUtility.GetForwardIconPath()));
                }
            }

            if (shouldCallDisplayContents)
            {
                DetailsStack.Push(m_ActiveContents);
                DisplayContents(value);
            }
        }

        private void ShowReferencesToForAsset(DetailsContents value, BuildLayout.ExplicitAsset asset, bool shouldCallDisplayContents)
        {
            if (shouldCallDisplayContents)
                value = new DetailsContents(asset.GetName(), asset.AssetPath);
            foreach (var internalAsset in asset.InternalReferencedExplicitAssets)
                value.DrillableItems.Add(new DetailsListItem(internalAsset.GetName(), internalAsset.AssetPath, null, null));

            foreach (var externalAsset in asset.ExternallyReferencedAssets)
                value.DrillableItems.Add(new DetailsListItem((string) externalAsset.Bundle.Name, BuildReportUtility.GetAssetBundleIconPath(), () => ShowAssetsThatLinkToBundle(
                    DetailsViewTab.ReferencesTo,
                    externalAsset.Bundle,
                    GetAssetsThatLinkToBundleMap(asset.Bundle, asset)[externalAsset.Bundle]), BuildReportUtility.GetForwardIconPath()));

            foreach (var implicitAsset in asset.InternalReferencedOtherAssets)
                value.DrillableItems.Add(new DetailsListItem(implicitAsset.AssetPath, implicitAsset.AssetPath, null, null, FontStyle.Italic));

            if (shouldCallDisplayContents)
            {
                DetailsStack.Push(m_ActiveContents);
                DisplayContents(value);
            }
        }

        private Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>> GetAssetsThatLinkToBundleMap(BuildLayout.Bundle bundle, BuildLayout.ExplicitAsset asset)
        {
            Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>> bundlesLinkedToAsset = new Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>>();
            foreach(var bDep in bundle.BundleDependencies)
            {
                foreach (var depAsset in bDep.AssetDependencies)
                {
                    if (depAsset.rootAsset == asset)
                    {
                        var depBundle = depAsset.dependencyAsset.Bundle;
                        if (!bundlesLinkedToAsset.ContainsKey(depBundle))
                            bundlesLinkedToAsset[depBundle] = new List<BuildLayout.ExplicitAsset>();

                        bundlesLinkedToAsset[depBundle].Add(depAsset.dependencyAsset);
                    }
                }
            }
            return bundlesLinkedToAsset;
        }

        private Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>> GetAssetsThatLinkFromBundleMap(BuildLayout.Bundle bundle, BuildLayout.ExplicitAsset asset)
        {
            Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>> bundlesLinkedToAsset = new Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>>();

            //oof - this sucks, surely there's another way
            foreach(var bDep in bundle.DependentBundles)
            {
                foreach (var depFile in bDep.Files)
                {
                    foreach (var depAsset in depFile.Assets)
                    {
                        if (depAsset == asset)
                        {
                            var depBundle = depAsset.Bundle;
                            if (!bundlesLinkedToAsset.ContainsKey(depBundle))
                                bundlesLinkedToAsset[depBundle] = new List<BuildLayout.ExplicitAsset>();

                            bundlesLinkedToAsset[depBundle].Add(depAsset);
                        }
                    }
                }
            }
            return bundlesLinkedToAsset;
        }

        private void ShowAssetsThatLinkToBundle(DetailsViewTab tab, BuildLayout.Bundle bundle, List<BuildLayout.ExplicitAsset> linkedAssets)
        {
            DetailsContents dc = new DetailsContents((string) bundle.Name, BuildReportUtility.GetAssetBundleIconPath());
            foreach (var asset in linkedAssets)
            {
                Action onDrillDown = null;
                if (tab == DetailsViewTab.ReferencesTo && asset.ExternallyReferencedAssets.Count > 0)
                    onDrillDown = () => ShowReferencesToForAsset(dc, asset, true);
                if (tab == DetailsViewTab.ReferencedBy && asset.ReferencingAssets.Count > 0)
                    onDrillDown = () => ShowReferencesByForAsset(dc, asset, true);
                dc.DrillableItems.Add(new DetailsListItem(asset.GetName(), asset.AssetPath, onDrillDown, onDrillDown == null ? null : BuildReportUtility.GetForwardIconPath()));
            }

            if (bundle.AssetCount - linkedAssets.Count > 0)
                dc.DrillableItems.Add(new DetailsListItem($"({bundle.AssetCount - linkedAssets.Count}) other assets", null));

            DetailsStack.Push(m_ActiveContents);
            DisplayContents(dc);
        }

        private DetailsContents GetContents(BuildLayout.ExplicitAsset asset, DetailsViewTab tab)
        {
            DetailsContents value = new DetailsContents(asset.Address, asset.AssetPath);

            switch (tab)
            {
                case DetailsViewTab.ReferencedBy:
                    ShowReferencesByForAsset(value, asset, false);
                    break;

                case DetailsViewTab.ReferencesTo:
                    ShowReferencesToForAsset(value, asset, false);
                    break;
            }

            return value;
        }

        private DetailsContents GetContents(BuildLayout.DataFromOtherAsset asset, DetailsViewTab tab)
        {
            DetailsContents value = new DetailsContents($"{asset.AssetPath} (in this Bundle)", asset.AssetPath);

            switch(tab)
            {
                case DetailsViewTab.ReferencedBy:
                    foreach (var refAsset in asset.ReferencingAssets)
                        value.DrillableItems.Add(new DetailsListItem(refAsset.GetName(), refAsset.AssetPath, null, null));
                    break;

                case DetailsViewTab.ReferencesTo:
                    //Do nothing
                    break;
            }

            return value;
        }

        private VisualElement RefToMakeItem()
        {
            var vta = GUIUtility.Clone(m_DetailsContentDrillableListItem);
            m_ButtonCallBackTracker.Add(vta, new List<Action>());
            Button button = vta.Q<Button>(BuildReportUtility.DrillableListViewButton);
            BuildReportUtility.SetVisibility(button, false);
            return vta;
        }

        private void RefToBindItem(VisualElement e, int i)
        {
            var drillDownEvent = m_ContentItems[i].DrillDownEvent;

            Image icon = e.Q<Image>(BuildReportUtility.DrillableListViewItemIcon);
            icon.image = null;
            if (!string.IsNullOrEmpty(m_ContentItems[i].ImagePath) && BuildReportUtility.GetIcon(m_ContentItems[i].ImagePath) is Texture iconTexture && iconTexture != null)
            {
                icon.image = iconTexture;
                icon.RemoveFromClassList(BuildReportUtility.TreeViewItemNoIcon);
            }
            else
            {
                icon.AddToClassList(BuildReportUtility.TreeViewItemNoIcon);
            }

            Button button = e.Q<Button>(BuildReportUtility.DrillableListViewButton);

            var label = e.Q<Label>(BuildReportUtility.DrillableListViewItemName);
            string buttonIconPath = m_ContentItems[i].ButtonImagePath;
            if(!string.IsNullOrEmpty(buttonIconPath))
            {
                if (buttonIconPath == BuildReportUtility.GetHelpIconPath())
                    button.tooltip = "Selects the asset(s) that pulled this non-bundles asset into the bundle";
                else
                    button.tooltip = null;
                button.style.backgroundImage = new StyleBackground(BuildReportUtility.GetIcon(buttonIconPath) as Texture2D);
                button.style.maxHeight = button.style.maxWidth = new Length(16f, LengthUnit.Pixel);
                button.text = "";
                label.style.minWidth = new Length(88f, LengthUnit.Percent);
            }
            label.text = m_ContentItems[i].Text;
            if (m_ContentItems[i].ImagePath != BuildReportUtility.GetAssetBundleIconPath())
                label.tooltip = "Asset Path: " + m_ContentItems[i].ImagePath;
            label.style.unityFontStyleAndWeight = m_ContentItems[i].StyleForText;

            if (m_ContentItems[i].CanUseContextMenu)
            {
                label.AddManipulator(new ContextualMenuManipulator((ContextualMenuPopulateEvent evt) =>
                {
                    evt.menu.AppendAction("Search in this window", (e) =>
                    {
                        string newSearchValue = label.text;
                        m_Window.m_ActiveContentView.m_SearchField.Q<TextField>().value = newSearchValue;
                    });
                }));
            }

            if (drillDownEvent == null)
            {
                label.style.maxWidth = label.style.width = new Length(88f, LengthUnit.Percent);
                BuildReportUtility.SetVisibility(button, false);
            }
            else
            {
                label.style.maxWidth = label.style.width = new Length(64f, LengthUnit.Percent);
                BuildReportUtility.SetVisibility(button, true);
                foreach (var callback in m_ButtonCallBackTracker[e])
                    button.clicked -= callback;
                m_ButtonCallBackTracker[e].Clear();

                button.clicked += drillDownEvent;
                m_ButtonCallBackTracker[e].Add(drillDownEvent);
            }
        }

        private void RefreshBackButton()
        {
            if (DetailsStack.Count > 0)
                BuildReportUtility.SetVisibility(m_BackButton, true);
            else
                BuildReportUtility.SetVisibility(m_BackButton, false);
        }

        private void UpdateTabButtons(int refBy, int refTo)
        {
            if (refBy == 0 && refTo == 0)
            {
                m_RefByButton.text = "Referenced By";
                m_RefToButton.text = "References To";
            }
            else
            {
                m_RefByButton.text = $"Referenced By ({refBy})";
                m_RefToButton.text = $"References To ({refTo})";
            }
        }

        private int GetRefByCount(BuildLayout.Bundle bundle)
        {
            return bundle.DependentBundles.Count;
        }

        private int GetRefByCount(BuildLayout.ExplicitAsset asset)
        {
            return asset.ReferencingAssets.Count;
        }

        private int GetRefByCount(BuildLayout.DataFromOtherAsset asset)
        {
            return asset.ReferencingAssets.Count;
        }

        private int GetRefToCount(BuildLayout.Bundle bundle)
        {
            int total = 0;
            foreach (var file in bundle.Files)
                total += file.Assets.Count + file.OtherAssets.Count;

            return total;
        }

        private int GetRefToCount(BuildLayout.ExplicitAsset asset)
        {
            return asset.InternalReferencedExplicitAssets.Count + asset.ExternallyReferencedAssets.Count + asset.InternalReferencedOtherAssets.Count;
        }

        private int GetRefToCount(BuildLayout.DataFromOtherAsset asset)
        {
            return 0;
        }
    }
}
#endif
