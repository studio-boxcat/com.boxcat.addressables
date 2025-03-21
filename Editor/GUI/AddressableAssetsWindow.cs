using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.AddressableAssets.GUI
{
    internal class AddressableAssetsWindow : EditorWindow
    {
        [SerializeField]
        internal AddressableAssetsSettingsGroupEditor m_GroupEditor;

        [MenuItem("Window/Asset Management/Addressables/Settings", priority = 2051)]
        internal static void ShowSettingsInspector()
        {
            var setting = AddressableCatalog.Default;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
            EditorGUIUtility.PingObject(setting);
            Selection.activeObject = setting;
        }

        [MenuItem("Window/Asset Management/Addressables/Groups", priority = 2050)]
        internal static void Init()
        {
            var window = GetWindow<AddressableAssetsWindow>();
            window.titleContent = new GUIContent("Addressables Groups");
            window.minSize = new Vector2(430, 250);
            window.Show();
        }

        internal void SelectAssetsInGroupEditor(IList<AddressableAssetEntry> entries)
        {
            m_GroupEditor ??= new AddressableAssetsSettingsGroupEditor(this);
            m_GroupEditor.SelectEntries(entries);
        }

        internal void SelectGroupInGroupEditor(AddressableAssetGroup group, bool fireSelectionChanged)
        {
            m_GroupEditor ??= new AddressableAssetsSettingsGroupEditor(this);
            m_GroupEditor.SelectGroup(group, fireSelectionChanged);
        }

        public void OnEnable()
        {
            m_GroupEditor?.OnEnable();
        }

        public void OnDisable()
        {
            m_GroupEditor?.OnDisable();
        }

        public void OnGUI()
        {
            m_GroupEditor ??= new AddressableAssetsSettingsGroupEditor(this);
            if (m_GroupEditor.OnGUI(new Rect(0, 0, position.width, position.height)))
                Repaint();
        }
    }
}