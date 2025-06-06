#if UNITY_2022_2_OR_NEWER
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bundles.Editor
{
    internal class DetailsSummaryView
    {
        private BuildReportWindow m_Window;
        protected VisualElement m_DetailsSummary;

        internal DetailsSummaryView(VisualElement root, BuildReportWindow window)
        {
            m_Window = window;

            m_DetailsSummary = root.Q<VisualElement>(BuildReportUtility.DetailsSummaryPane);
            m_DetailsSummary.style.paddingLeft =
                m_DetailsSummary.style.paddingRight = new StyleLength(new Length(15f, LengthUnit.Pixel));
        }

        public void ClearSummary()
        {
            m_DetailsSummary.Clear();
        }


        public void UpdateSummary(object item)
        {
            ClearSummary();

            if (DetailsUtility.IsBundle(item))
                DisplayBundleSummary(DetailsUtility.GetBundle(item));
            else
            {
                var asset = DetailsUtility.GetAsset(item);
                if (asset != null)
                    DisplayExplicitAssetSummary(new BundlesViewBuildReportAsset(asset));
                else
                {
                    var otherAsset = DetailsUtility.GetOtherAssetData(item);
                    if (otherAsset != null)
                        DisplayDataFromOtherAssetSummary(new BundlesViewBuildReportAsset(otherAsset));
                }
            }
        }

        private void DisplayBundleSummary(BuildLayout.Bundle bundle)
        {
            if (bundle == null)
                return;

            DetailsSummaryBuilder builder = new DetailsSummaryBuilder()
                .With(BuildReportUtility.GetIcon(BuildReportUtility.GetAssetBundleIconPath()), (string) bundle.Name)
                .With("Uncompressed", $"{ BuildReportUtility.GetDenominatedBytesString(bundle.UncompressedFileSize)}")
                .With("Bundle fizesize", $"{BuildReportUtility.GetDenominatedBytesString(bundle.FileSize)}")
                .With("Total size (+ refs)", $"{BuildReportUtility.GetDenominatedBytesString(bundle.FileSize + bundle.DependencyFileSize + bundle.ExpandedDependencyFileSize)}")
                .With("Load Path", $"{bundle.LoadPath}");

            m_DetailsSummary.Add(builder.Build());

            m_DetailsSummary.Add(CreateButtonRow(
                BuildReportUtility.CreateButton("Search in this view", () =>
                {
                    string newSearchValue = (string) bundle.Name;
                    m_Window.m_ActiveContentView.m_SearchField.Q<TextField>().value = newSearchValue;
                }),
                BuildReportUtility.CreateButton("Select in Group", () =>
                {
                    m_Window.NavigateToView(BuildReportWindow.ContentViewType.GroupsView);
                    m_Window.SelectItemInView(BuildReportUtility.ComputeDataHash(bundle.Name.Value), true);
                })));
        }

        public void DisplayExplicitAssetSummary(IBundlesBuildReportAsset reportAsset)
        {
            DetailsSummaryBuilder builder = new DetailsSummaryBuilder()
                .With(BuildReportUtility.GetIcon(reportAsset.ExplicitAsset.AssetPath), reportAsset.ExplicitAsset.Address)
                .With("Asset Path", reportAsset.ExplicitAsset.AssetPath)
                .With("Uncompressed", $"{BuildReportUtility.GetDenominatedBytesString(reportAsset.ExplicitAsset.File.UncompressedSize)}")
                .With("Total Size (+ refs)", $"{BuildReportUtility.GetDenominatedBytesString(reportAsset.SizeWDependencies)}");

            if (reportAsset.Bundles != null)
            {
                foreach (BuildLayout.Bundle bundle in reportAsset.Bundles)
                {
                    builder.With("Bundle", (string) bundle.Name)
                           .With("Load Path", bundle.Name.Value);
                }
            }

            m_DetailsSummary.Add(builder.Build());
            m_DetailsSummary.Add(CreateButtonRow(
                BuildReportUtility.CreateButton("Select in Editor", () =>
                {
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(reportAsset.ExplicitAsset.AssetPath);
                }),
                BuildReportUtility.CreateButton("Select in Group", () =>
                {
                    m_Window.NavigateToView(BuildReportWindow.ContentViewType.GroupsView);
                    m_Window.SelectItemInView(BuildReportUtility.ComputeDataHash(reportAsset.ExplicitAsset.Address));
                }),
                BuildReportUtility.CreateButton("Search in this view", () =>
                {
                    string newSearchValue = reportAsset.ExplicitAsset.Address;
                    m_Window.m_ActiveContentView.m_SearchField.Q<TextField>().value = newSearchValue;
                })));
            m_DetailsSummary.Add(CreateButtonRow(BuildReportUtility.CreateButton("Select in Bundle", () =>
                {
                    m_Window.NavigateToView(BuildReportWindow.ContentViewType.BundleView);
                    m_Window.SelectItemInView(BuildReportUtility.ComputeDataHash((string) reportAsset.ExplicitAsset.Bundle.Name, reportAsset.ExplicitAsset.Address));
                })));
        }

        public void DisplayDataFromOtherAssetSummary(IBundlesBuildReportAsset reportAsset)
        {
            DetailsSummaryBuilder builder = new DetailsSummaryBuilder()
                .With(BuildReportUtility.GetIcon(reportAsset.DataFromOtherAsset.AssetPath), reportAsset.DataFromOtherAsset.AssetPath)
                .With("Uncompressed", $"{BuildReportUtility.GetDenominatedBytesString(reportAsset.DataFromOtherAsset.File.UncompressedSize)}")
                .With("Total Size (+ refs)", $"{BuildReportUtility.GetDenominatedBytesString(reportAsset.SizeWDependencies)}")
                .With("Included in Bundle Count", reportAsset.Bundles.Count.ToString());
            if (reportAsset.Bundles.Count > 1)
                builder.With("Total size of all duplications", $"{BuildReportUtility.GetDenominatedBytesString(reportAsset.DataFromOtherAsset.SerializedSize * (ulong)reportAsset.Bundles.Count) }");

            m_DetailsSummary.Add(builder.Build());
            m_DetailsSummary.Add(CreateButtonRow(
                BuildReportUtility.CreateButton("Search in this view", () =>
                {
                    string newSearchValue = reportAsset.DataFromOtherAsset.AssetPath;
                    m_Window.m_ActiveContentView.m_SearchField.Q<TextField>().value = newSearchValue;
                }),
                BuildReportUtility.CreateButton("Select in Editor", () =>
                {
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(reportAsset.DataFromOtherAsset.AssetPath);
                })));
            m_DetailsSummary.Add(CreateHelpTextBox("This asset was pulled into the AssetBundle because one or more Bundles assets have references to it."));
        }

        private static VisualElement CreateButtonRow(params Button[] buttons)
        {
            VisualElement container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginTop = new StyleLength(new Length(18f, LengthUnit.Pixel));

            foreach (var button in buttons)
                container.Add(button);

            return container;
        }

        private static VisualElement CreateHelpTextBox(string helpText)
        {
            Foldout foldout = new Foldout();
            foldout.style.marginTop = new Length(10f, LengthUnit.Pixel);
            foldout.style.marginBottom = new Length(25f, LengthUnit.Pixel);
            foldout.style.height = new Length(20f, LengthUnit.Pixel);
            foldout.style.flexDirection = FlexDirection.Column;
            foldout.text = "Help";

            VisualElement helpElement = new VisualElement();
            foldout.Add(helpElement);

            Label label = new Label();
            helpElement.Add(label);

            label.text = helpText;
            label.style.paddingTop = new Length(25, LengthUnit.Pixel);
            label.style.width = new Length(100, LengthUnit.Percent);
            label.style.maxWidth = new Length(100, LengthUnit.Percent);
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.whiteSpace = WhiteSpace.Normal;
            return foldout;
        }
    }
}
#endif
