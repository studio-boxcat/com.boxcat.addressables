using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.AddressableAssets.Util;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// Contains editor data for the addressables system.
    /// </summary>
    public class AddressableAssetSettings : ScriptableObject, ISelfValidator
    {
        internal class Cache<T1, T2>
        {
            private readonly AddressableAssetSettings m_Settings;
            private readonly Dictionary<T1, T2> m_Data = new();
            private int m_Version;

            public Cache(AddressableAssetSettings settings)
            {
                m_Settings = settings;
            }

            public bool TryGetCached(T1 key, out T2 result)
            {
                if (m_Version != m_Settings.version)
                {
                    result = default;
                    return false;
                }

                return m_Data.TryGetValue(key, out result);
            }

            public void Add(T1 key, T2 value)
            {
                if (m_Version != m_Settings.version)
                {
                    m_Data.Clear();
                    m_Version = m_Settings.version;
                }

                m_Data.Add(key, value);
            }
        }

        /// <summary>
        /// Options for labeling all the different generated events.
        /// </summary>
        public enum ModificationEvent
        {
            /// <summary>
            /// Use to indicate that a group was added to the settings object.
            /// </summary>
            GroupAdded,

            /// <summary>
            /// Use to indicate that a group was removed from the the settings object.
            /// </summary>
            GroupRemoved,

            /// <summary>
            /// Use to indicate that a group in the settings object was renamed.
            /// </summary>
            GroupRenamed,

            GroupModified,

            /// <summary>
            /// Use to indicate that an asset entry was created.
            /// </summary>
            EntryCreated,

            /// <summary>
            /// Use to indicate that an asset entry was added to a group.
            /// </summary>
            EntryAdded,

            /// <summary>
            /// Use to indicate that an asset entry moved from one group to another.
            /// </summary>
            EntryMoved,

            /// <summary>
            /// Use to indicate that an asset entry was removed from a group.
            /// </summary>
            EntryRemoved,

            /// <summary>
            /// Use to indicate that an asset entry was modified.
            /// </summary>
            EntryModified,

            /// <summary>
            /// Use to indicate that a batch of asset entries was modified. Note that the posted object will be null.
            /// </summary>
            BatchModification,
        }

        /// <summary>
        /// Event for handling settings changes.  The object passed depends on the event type.
        /// </summary>
        public Action<AddressableAssetSettings, ModificationEvent, object> OnModification { get; set; }

        [FormerlySerializedAs("m_defaultGroup")]
        [SerializeField]
        AddressableAssetGroup m_DefaultGroup;

        [SerializeField]
#if UNITY_2021_1_OR_NEWER
        bool m_NonRecursiveBuilding = true;
#else
        bool m_NonRecursiveBuilding = false;
#endif

        [SerializeField]
#if UNITY_2021_1_OR_NEWER
        bool m_ContiguousBundles = true;
#else
        bool m_ContiguousBundles = false;
#endif

        /// <summary>
        /// If set, packs assets in bundles contiguously based on the ordering of the source asset which results in improved asset loading times. Disable this if you've built bundles with a version of Addressables older than 1.12.1 and you want to minimize bundle changes.
        /// </summary>
        public bool ContiguousBundles => m_ContiguousBundles;

        /// <summary>
        /// If set, Calculates and build asset bundles using Non-Recursive Dependency calculation methods. This approach helps reduce asset bundle rebuilds and runtime memory consumption.
        /// </summary>
        public bool NonRecursiveBuilding => m_NonRecursiveBuilding;

        int m_Version;

        /// <summary>
        /// Hash of the current settings.  This value is recomputed if anything changes.
        /// </summary>
        public int version => m_Version;

        [SerializeField]
        List<AddressableAssetGroup> m_GroupAssets = new();

        /// <summary>
        /// List of asset groups.
        /// </summary>
        public List<AddressableAssetGroup> groups => m_GroupAssets;

        /// <summary>
        /// Remove an asset entry.
        /// </summary>
        /// <param name="guid">The  guid of the asset.</param>
        /// <param name="postEvent">Send modifcation event.</param>
        /// <returns>True if the entry was found and removed.</returns>
        public bool RemoveAssetEntry(AssetGUID guid, bool postEvent = true)
            => RemoveAssetEntry(FindAssetEntry(guid), postEvent);

        /// <summary>
        /// Remove an asset entry.
        /// </summary>
        /// <param name="entry">The entry to remove.</param>
        /// <param name="postEvent">Send modifcation event.</param>
        /// <returns>True if the entry was found and removed.</returns>
        internal bool RemoveAssetEntry(AddressableAssetEntry entry, bool postEvent = true)
        {
            if (entry == null)
                return false;
            if (entry.parentGroup != null)
                entry.parentGroup.RemoveAssetEntry(entry, postEvent);
            return true;
        }

        /// <summary>
        /// Create a new AddressableAssetSettings object.
        /// </summary>
        /// <param name="configFolder">The folder to store the settings object.</param>
        /// <param name="configName">The name of the settings object.</param>
        /// <returns></returns>
        public static AddressableAssetSettings Create(string configFolder, string configName)
        {
            var path = configFolder + "/" + configName + ".asset";
            var aa = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);
            if (aa != null) return aa;

            aa = CreateInstance<AddressableAssetSettings>();
            aa.name = configName;
            // TODO: Uncomment after initial opt-in testing period
            //aa.ContiguousBundles = true;

            Directory.CreateDirectory(configFolder);
            AssetDatabase.CreateAsset(aa, path);
            aa = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);
            AssetDatabase.SaveAssets();

            return aa;
        }

        /// <summary>
        /// Find asset group by functor.
        /// </summary>
        /// <param name="func">The functor to call on each group.  The first group that evaluates to true is returned.</param>
        /// <returns>The group found or null.</returns>
        public AddressableAssetGroup FindGroup(Func<AddressableAssetGroup, bool> func)
        {
            return groups.Find(g => g != null && func(g));
        }

        /// <summary>
        /// Find asset group by name.
        /// </summary>
        /// <param name="groupName">The name of the group.</param>
        /// <returns>The group found or null.</returns>
        public AddressableAssetGroup FindGroup(string groupName)
        {
            return FindGroup(g => g != null && g.Name == groupName);
        }

        /// <summary>
        /// The default group.  This group is used when marking assets as addressable via the inspector.
        /// </summary>
        public AddressableAssetGroup DefaultGroup
        {
            get
            {
                if (m_DefaultGroup == null)
                    m_DefaultGroup = m_GroupAssets[0];
                return m_DefaultGroup;
            }
            set
            {
                if (value == null)
                    L.E("Unable to set null as the Default Group.  Default Groups must not be ReadOnly.");
                else if (m_DefaultGroup != value)
                {
                    m_DefaultGroup = value;
                    SetDirty(ModificationEvent.BatchModification, null, false, true);
                }
            }
        }

        private Cache<AssetGUID, AddressableAssetEntry> m_FindAssetEntryCache = null;

        /// <summary>
        /// Find and asset entry by guid.
        /// </summary>
        /// <param name="guid">The asset guid.</param>
        /// <returns>The found entry or null.</returns>
        public AddressableAssetEntry FindAssetEntry(AssetGUID guid)
        {
            m_FindAssetEntryCache ??= new Cache<AssetGUID, AddressableAssetEntry>(this);

            if (m_FindAssetEntryCache.TryGetCached(guid, out var foundEntry))
                return foundEntry;

            foreach (var group in m_GroupAssets)
            {
                if (group == null) continue;
                foundEntry = group.GetAssetEntry(guid);
                if (foundEntry == null) continue;
                m_FindAssetEntryCache.Add(guid, foundEntry);
                return foundEntry;
            }

            return null;
        }

        /// <summary>
        /// Move an existing entry to a group.
        /// </summary>
        /// <param name="entry">The entry to move.</param>
        /// <param name="targetParent">The group to add the entry to.</param>
        /// <param name="postEvent">Send modification event.</param>
        public static void MoveEntry(AddressableAssetEntry entry, AddressableAssetGroup targetParent, bool postEvent = true)
        {
            if (targetParent == null || entry == null)
                return;

            if (entry.parentGroup != null && entry.parentGroup != targetParent)
                entry.parentGroup.RemoveAssetEntry(entry, postEvent);

            targetParent.AddAssetEntry(entry, postEvent);
        }

        /// <summary>
        /// Create a new entries for each asset, or if one exists in a different group, move it into the targetParent group.
        /// </summary>
        /// <param name="guids">The asset guid's to move.</param>
        /// <param name="targetParent">The group to add the entries to.</param>
        /// <param name="createdEntries">List to add new entries to. If null, the list will be ignored.</param>
        /// <param name="movedEntries">List to add moved entries to. If null, the list will be ignored.</param>
        /// <param name="postEvent">Send modification event.</param>
        /// <exception cref="ArgumentException"></exception>
        internal void CreateOrMoveEntries(IEnumerable<AssetGUID> guids,
            AddressableAssetGroup targetParent,
            List<AddressableAssetEntry> createdEntries = null,
            List<AddressableAssetEntry> movedEntries = null,
            bool postEvent = true)
        {
            if (targetParent == null)
                throw new ArgumentException("targetParent must not be null");

            foreach (var guid in guids)
            {
                var entry = FindAssetEntry(guid);
                if (entry != null)
                {
                    MoveEntry(entry, targetParent, false);
                    movedEntries?.Add(entry);
                }
                else
                {
                    entry = CreateAndAddEntryToGroup(guid, targetParent, false);
                    if (entry != null && createdEntries != null)
                        createdEntries.Add(entry);
                }
            }

            if (postEvent)
                SetDirty(ModificationEvent.BatchModification, guids, true, true);
        }

        private AddressableAssetEntry CreateAndAddEntryToGroup(AssetGUID guid, AddressableAssetGroup targetParent, bool postEvent = true)
        {
            var entry = targetParent.GetAssetEntry(guid) ?? new AddressableAssetEntry(guid, targetParent);
            targetParent.AddAssetEntry(entry, postEvent);
            SetDirty(ModificationEvent.EntryCreated, entry, postEvent);
            return entry;
        }

        /// <summary>
        /// Create a new asset group.
        /// </summary>
        /// <param name="postEvent">Post modification event.</param>
        /// <returns>The newly created group.</returns>
        public AddressableAssetGroup CreateGroup(bool postEvent)
        {
            // Prepare the AssetGroups folder & path to save the new group.
            var root = ResolveConfigFolder() + "/AssetGroups";
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);
            var path = AssetDatabase.GenerateUniqueAssetPath(root + "/New Group.asset");
            var groupName = Path.GetFileNameWithoutExtension(path);

            // Create the new group and add it to the settings.
            var group = CreateInstance<AddressableAssetGroup>();
            AssetDatabase.CreateAsset(group, path);
            group.SetUp(this, groupName);
            groups.Add(group);

            // Mark the settings as dirty and post the event.
            SetDirty(ModificationEvent.GroupAdded, group, postEvent, true);
            return group;
        }

        internal bool IsNotUniqueGroupName(string groupName)
        {
            bool foundExisting = false;
            foreach (var g in groups)
            {
                if (g != null && g.Name == groupName)
                {
                    foundExisting = true;
                    break;
                }
            }

            return foundExisting;
        }

        internal void RemoveGroupInternal(AddressableAssetGroup g, bool deleteAsset, bool postEvent)
        {
            groups.Remove(g);
            SetDirty(ModificationEvent.GroupRemoved, g, postEvent, true);
            if (g == null || !deleteAsset) return;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(g, out var guidOfGroup, out long _) is false) return;

            var groupPath = AssetDatabase.GUIDToAssetPath(guidOfGroup);
            if (!string.IsNullOrEmpty(groupPath))
                AssetDatabase.DeleteAsset(groupPath);
        }

        /// <summary>
        /// The folder of the settings asset.
        /// </summary>
        public string ResolveConfigFolder()
        {
            var path = AssetDatabase.GetAssetPath(this);
            return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Marks the object as modified.
        /// </summary>
        /// <param name="modificationEvent">The event type that is changed.</param>
        /// <param name="eventData">The object data that corresponds to the event.</param>
        /// <param name="postEvent">If true, the event is propagated to callbacks.</param>
        /// <param name="settingsModified">If true, the settings asset will be marked as dirty.</param>
        public void SetDirty(ModificationEvent modificationEvent, object eventData, bool postEvent, bool settingsModified = false)
        {
            Assert.IsNotNull(this, "Object is destroyed.");

            if (postEvent)
                OnModification?.Invoke(this, modificationEvent, eventData);

            if (settingsModified)
                EditorUtility.SetDirty(this);

            m_Version++;
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            // Check for duplicate addresses.
            {
                var addresses = new Dictionary<string, AddressableAssetEntry>();
                foreach (var g in groups)
                {
                    var entries = g.entries;
                    foreach (var e in entries)
                    {
                        // An empty address only marks the group for inclusion.
                        if (string.IsNullOrEmpty(e.address))
                            continue;

                        // If the address is unique, add it to the dictionary.
                        if (addresses.TryAdd(e.address, e))
                            continue;

                        var existingEntry = addresses[e.address];
                        result.AddError("Address " + e.address + " is not unique.  It is used by " + e.parentGroup.Name + " and " + existingEntry.parentGroup.Name);
                    }
                }
            }

            // Check for scene registered as EditorBuildSettings.scenes.
            {
                var scenes = BuiltinSceneCache.scenes;
                var sceneGuids = new HashSet<GUID>(scenes.Where(s => s.enabled).Select(s => s.guid));

                foreach (var e in groups.SelectMany(g => g.entries))
                {
                    if (sceneGuids.Contains((GUID) e.guid))
                        result.AddError("A scene from the EditorBuildScenes list has been marked as addressable: " + e.address);
                }
            }
        }
    }
}