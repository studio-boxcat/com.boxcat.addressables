using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[assembly: UxmlNamespacePrefix("Bundles.Editor", "BundlesGUI")]
namespace Bundles.Editor
{
    internal class RibbonButton : Button
    {
        private bool m_Toggled;
        public bool Toggled
        {
            get { return m_Toggled; }
            set
            {
                m_Toggled = value;
                if (m_Toggled)
                    AddToClassList(StyleClassToggled);
                else
                    RemoveFromClassList(StyleClassToggled);
            }
        }

        private string m_CachedOriginalTooltip;

        public const string StyleClass = "ribbon__button";
        public const string StyleClassToggled = "ribbon__button--toggled";
        public RibbonButton()
        {
            AddToClassList(StyleClass);
            Init();
        }

        /// <summary>
        /// Sets the Buttons enabled state and if a disablingTooltipReason is passed, it is set as the tool-tip on disabling and will restore the old tooltip on re-enabling later.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="disablingTooltipReason">Provide a non-null, non-empty-string reason even when enabling to confirm that the tool-tip should be restored to what it was before toggling the enabled status off.</param>
        public void SetButtonEnabled(bool enabled, string disablingTooltipReason = null)
        {
            if (!string.IsNullOrEmpty(disablingTooltipReason))
            {
                if (!enabled)
                {
                    m_CachedOriginalTooltip = tooltip;
                    tooltip = disablingTooltipReason;
                }
                else if (!enabledSelf)
                {
                    tooltip = m_CachedOriginalTooltip;
                }
            }

            SetEnabled(enabled);
        }

        private void Init()
        {
            m_CachedOriginalTooltip = tooltip;
        }

        /// <summary>
        /// Instantiates a <see cref="Ribbon"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<RibbonButton, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="RibbonButton"/>.
        /// </summary>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text", defaultValue = "Button" };
            private UxmlBoolAttributeDescription m_Toggled = new UxmlBoolAttributeDescription { name = "toggled", defaultValue = false };
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var button = ((RibbonButton)ve);
                var text = m_Text.GetValueFromBag(bag, cc);
                button.text = text;

                var toggled = m_Toggled.GetValueFromBag(bag, cc);
                if (toggled)
                    button.AddToClassList(StyleClassToggled);
                else
                    button.RemoveFromClassList(StyleClassToggled);
                button.Toggled = toggled;

                ((RibbonButton)ve).Init();
            }
        }
    }

}
