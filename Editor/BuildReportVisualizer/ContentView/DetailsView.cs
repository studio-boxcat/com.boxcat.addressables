#if UNITY_2022_2_OR_NEWER
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Bundles.Editor
{
    internal enum DetailsViewTab
    {
        ReferencesTo,
        ReferencedBy
    }

    internal class DetailsView
    {
        private DetailsViewTab m_ActiveContentsTab;
        private BuildReportWindow m_Window;

        private DetailsContentView m_Contents;
        private DetailsSummaryView m_Summary;

        private object m_DetailsActiveObject;

        internal DetailsView(BuildReportWindow window)
        {
            m_Window = window;
            m_ActiveContentsTab = DetailsViewTab.ReferencesTo;
        }

        public void CreateGUI(VisualElement rootVisualElement)
        {
            m_Summary = new DetailsSummaryView(rootVisualElement, m_Window);
            m_Contents = new DetailsContentView(rootVisualElement, m_Window);

            rootVisualElement.Q<RibbonButton>("ReferencesToTab").clicked += () =>
            {
                m_ActiveContentsTab = DetailsViewTab.ReferencesTo;
                DetailsStack.Clear();

                DisplayContents(m_DetailsActiveObject);

            };

            rootVisualElement.Q<RibbonButton>("ReferencedByTab").clicked += () =>
            {
                m_ActiveContentsTab = DetailsViewTab.ReferencedBy;
                DetailsStack.Clear();

                DisplayContents(m_DetailsActiveObject);

            };
        }

        public void OnSelected(IEnumerable<object> items)
        {
            ClearGUI();
            DetailsStack.Clear();

            foreach (object item in items)
            {
                DisplayItemSummary(item);
                DisplayContents(item);
                m_DetailsActiveObject = item;
            }
        }

        public void DisplayItemSummary(object item)
        {
            m_Summary.UpdateSummary(item);
        }

        public void DisplayContents(object contents)
        {
            m_Contents.DisplayContents(contents, m_ActiveContentsTab);
        }

        public void ClearGUI()
        {
            m_Summary.ClearSummary();
            m_Contents.ClearContents();
            DisplayContents(null);
        }
    }
}
#endif
