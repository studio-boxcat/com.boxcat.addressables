using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;
using UnityEngine.U2D;

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// Contains data for an addressable asset entry.
    /// </summary>
    [Serializable]
    public class AddressableAssetEntry : ISerializationCallbackReceiver
    {
        [FormerlySerializedAs("m_guid")]
        [SerializeField]
        string m_GUID;

        [FormerlySerializedAs("m_address")]
        [SerializeField]
        string m_Address;

        [FormerlySerializedAs("m_readOnly")]
        [SerializeField]
        bool m_ReadOnly;

        internal virtual bool HasSettings()
        {
            return false;
        }

        /// <summary>
        /// List of AddressableAssetEntries that are considered sub-assets of a main Asset.  Typically used for Folder entires.
        /// </summary>
        [NonSerialized]
        public List<AddressableAssetEntry> SubAssets = new List<AddressableAssetEntry>();

        [NonSerialized]
        AddressableAssetGroup m_ParentGroup;

        /// <summary>
        /// The asset group that this entry belongs to.  An entry can only belong to a single group at a time.
        /// </summary>
        public AddressableAssetGroup parentGroup
        {
            get { return m_ParentGroup; }
            set { m_ParentGroup = value; }
        }

        /// <summary>
        /// The id for the bundle file.
        /// </summary>
        public string BundleFileId { get; set; }

        /// <summary>
        /// The asset guid.
        /// </summary>
        public string guid
        {
            get { return m_GUID; }
        }

        /// <summary>
        /// The address of the entry.  This is treated as the primary key in the ResourceManager system.
        /// </summary>
        public string address
        {
            get { return m_Address; }
            set { SetAddress(value); }
        }

        /// <summary>
        /// Set the address of the entry.
        /// </summary>
        /// <param name="addr">The address.</param>
        /// <param name="postEvent">Post modification event.</param>
        public void SetAddress(string addr, bool postEvent = true)
        {
            if (m_Address != addr)
            {
                m_Address = addr;
                if (string.IsNullOrEmpty(m_Address))
                    m_Address = AssetPath;
                if (m_GUID.Length > 0 && m_Address.Contains('[') && m_Address.Contains(']'))
                    Debug.LogErrorFormat("Address '{0}' cannot contain '[ ]'.", m_Address);
                SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, this, postEvent);
            }
        }

        /// <summary>
        /// Read only state of the entry.
        /// </summary>
        public bool ReadOnly
        {
            get { return m_ReadOnly; }
            set
            {
                if (m_ReadOnly != value)
                {
                    m_ReadOnly = value;
                    SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, this, true);
                }
            }
        }

        /// <summary>
        /// Is a sub asset.  For example an asset in an addressable folder.
        /// </summary>
        public bool IsSubAsset { get; set; }

        /// <summary>
        /// Stores a reference to the parent entry. Only used if the asset is a sub asset.
        /// </summary>
        public AddressableAssetEntry ParentEntry { get; set; }

        bool m_CheckedIsScene;
        bool m_IsScene;

        /// <summary>
        /// Is this entry for a scene.
        /// </summary>
        public bool IsScene
        {
            get
            {
                if (!m_CheckedIsScene)
                {
                    m_CheckedIsScene = true;
                    m_IsScene = AssetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
                }

                return m_IsScene;
            }
        }

        internal Type m_cachedMainAssetType = null;

        /// <summary>
        /// The System.Type of the main Object referenced by an AddressableAssetEntry
        /// </summary>
        public Type MainAssetType
        {
            get
            {
                if (m_cachedMainAssetType == null)
                {
                    m_cachedMainAssetType = AssetDatabase.GetMainAssetTypeAtPath(AssetPath);
                    if (m_cachedMainAssetType == null)
                        return typeof(object); // do not cache a bad type lookup.
                }

                return m_cachedMainAssetType;
            }
        }

        /// <summary>
        /// Creates a list of keys that can be used to load this entry.
        /// </summary>
        /// <returns>The list of keys.  This will contain the address, the guid as a Hash128 if valid, all assigned labels, and the scene index if applicable.</returns>
        [CanBeNull]
        internal string CreateKeyList(bool includeAddress)
        {
            return includeAddress ? address : null;
        }

        internal AddressableAssetEntry(string guid, string address, AddressableAssetGroup parent, bool readOnly)
        {
            if (guid.Length > 0 && address.Contains('[') && address.Contains(']'))
                Debug.LogErrorFormat("Address '{0}' cannot contain '[ ]'.", address);
            m_GUID = guid;
            m_Address = address;
            m_ReadOnly = readOnly;
            parentGroup = parent;
        }

        Hash128 m_CurrentHash;
        internal Hash128 currentHash
        {
            get
            {
                if (!m_CurrentHash.isValid)
                {
                    m_CurrentHash.Append(m_GUID);
                    m_CurrentHash.Append(m_Address);
                    var flags = (m_ReadOnly ? 1 : 0) | (IsSubAsset ? 8 : 0);
                    m_CurrentHash.Append(flags);
                }
                return m_CurrentHash;
            }
        }

        internal void SetDirty(AddressableAssetSettings.ModificationEvent e, object o, bool postEvent)
        {
            m_CurrentHash = default;
            if (parentGroup != null)
                parentGroup.SetDirty(e, o, postEvent, true);
        }

        internal void SetCachedPath(string newCachedPath)
        {
            if (newCachedPath != m_cachedAssetPath)
            {
                m_cachedAssetPath = newCachedPath;
                m_MainAsset = null;
                m_TargetAsset = null;
            }
        }

        internal void SetSubObjectType(Type type)
        {
            m_cachedMainAssetType = type;
        }

        internal string m_cachedAssetPath = null;

        /// <summary>
        /// The path of the asset.
        /// </summary>
        public string AssetPath
        {
            get
            {
                if (string.IsNullOrEmpty(m_cachedAssetPath))
                {
                    if (string.IsNullOrEmpty(guid))
                        SetCachedPath(string.Empty);
                    else
                        SetCachedPath(AssetDatabase.GUIDToAssetPath(guid));
                }

                return m_cachedAssetPath;
            }
        }

        private UnityEngine.Object m_MainAsset;

        /// <summary>
        /// The main asset object for this entry.
        /// </summary>
        public UnityEngine.Object MainAsset
        {
            get
            {
                if (m_MainAsset == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_MainAsset, out string guid, out long localId))
                {
                    AddressableAssetEntry e = this;
                    while (string.IsNullOrEmpty(e.AssetPath))
                    {
                        if (e.ParentEntry == null)
                            return null;
                        e = e.ParentEntry;
                    }

                    m_MainAsset = AssetDatabase.LoadMainAssetAtPath(e.AssetPath);
                }

                return m_MainAsset;
            }
        }

        private UnityEngine.Object m_TargetAsset;

        /// <summary>
        /// The asset object for this entry.
        /// </summary>
        public UnityEngine.Object TargetAsset
        {
            get
            {
                if (m_TargetAsset == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_TargetAsset, out string guid, out long localId))
                {
                    if (!string.IsNullOrEmpty(AssetPath) || !IsSubAsset)
                    {
                        m_TargetAsset = MainAsset;
                        return m_TargetAsset;
                    }

                    if (ParentEntry == null || !string.IsNullOrEmpty(AssetPath) || string.IsNullOrEmpty(ParentEntry.AssetPath))
                        return null;

                    var mainAsset = ParentEntry.MainAsset;
                    if (ResourceManagerConfig.ExtractKeyAndSubKey(address, out string mainKey, out string subObjectName))
                    {
                        if (mainAsset != null && mainAsset.GetType() == typeof(SpriteAtlas))
                        {
                            m_TargetAsset = (mainAsset as SpriteAtlas).GetSprite(subObjectName);
                            return m_TargetAsset;
                        }

                        var subObjects = AssetDatabase.LoadAllAssetRepresentationsAtPath(ParentEntry.AssetPath);
                        foreach (var s in subObjects)
                        {
                            if (s != null && s.name == subObjectName)
                            {
                                m_TargetAsset = s;
                                break;
                            }
                        }
                    }
                }

                return m_TargetAsset;
            }
        }

        /// <summary>
        /// The asset load path.  This is used to determine the internal id of resource locations.
        /// </summary>
        /// <param name="isBundled">True if the asset will be contained in an asset bundle.</param>
        /// <param name="otherLoadPaths">The internal ids of the asset, typically shortened versions of the asset's GUID.</param>
        /// <returns>Return the runtime path that should be used to load this entry.</returns>
        public string GetAssetLoadPath(HashSet<string> otherLoadPaths)
        {
            return GetShortestUniqueString(guid, otherLoadPaths);

            static string GetShortestUniqueString(string guid, HashSet<string> otherLoadPaths)
            {
                var g = guid;
                if (otherLoadPaths == null)
                    return g;
                var len = 1;
                var p = g.Substring(0, len);
                while (otherLoadPaths.Contains(p))
                    p = g.Substring(0, ++len);
                otherLoadPaths.Add(p);
                return p;
            }
        }

        static string GetResourcesPath(string path)
        {
            path = path.Replace('\\', '/');
            int ri = path.LastIndexOf("/resources/", StringComparison.OrdinalIgnoreCase);
            if (ri >= 0)
                path = path.Substring(ri + "/resources/".Length);
            int i = path.LastIndexOf('.');
            if (i > 0)
                path = path.Substring(0, i);
            return path;
        }

        /// <summary>
        /// Gathers all asset entries.  Each explicit entry may contain multiple sub entries. For example, addressable folders create entries for each asset contained within.
        /// </summary>
        /// <param name="assets">The generated list of entries.  For simple entries, this will contain just the entry itself if specified.</param>
        /// <param name="includeSelf">Determines if the entry should be contained in the result list or just sub entries.</param>
        /// <param name="recurseAll">Determines if full recursion should be done when gathering entries.</param>
        /// <param name="includeSubObjects">Determines if sub objects such as sprites should be included.</param>
        /// <param name="entryFilter">Optional predicate to run against each entry, only returning those that pass.  A null filter will return all entries</param>
        public void GatherAllAssets(List<AddressableAssetEntry> assets, bool includeSelf, bool recurseAll, bool includeSubObjects, Func<AddressableAssetEntry, bool> entryFilter = null)
        {
            if (assets == null)
                assets = new List<AddressableAssetEntry>();

            if (string.IsNullOrEmpty(AssetPath))
                return;

            if (AssetDatabase.IsValidFolder(AssetPath))
                throw new NotSupportedException(AssetPath);

            if (entryFilter == null || entryFilter(this))
            {
                if (includeSelf)
                    assets.Add(this);
                if (includeSubObjects)
                {
                    var mainType = AssetDatabase.GetMainAssetTypeAtPath(AssetPath);
                    if (mainType == typeof(SpriteAtlas))
                        GatherSpriteAtlasEntries(assets);
                    else
                        GatherSubObjectEntries(assets);
                }
            }
        }

        void GatherSubObjectEntries(List<AddressableAssetEntry> assets)
        {
            var objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetPath);
            for (int i = 0; i < objs.Length; i++)
            {
                var o = objs[i];
                var namedAddress = string.Format("{0}[{1}]", address, o == null ? "missing reference" : o.name);
                if (o == null)
                {
                    Debug.LogWarning(string.Format("NullReference in entry {0}\nAssetPath: {1}\nAddressableAssetGroup: {2}", address, AssetPath, parentGroup.Name));
                    assets.Add(new AddressableAssetEntry("", namedAddress, parentGroup, true));
                }
                else
                {
                    var newEntry = parentGroup.Settings.CreateEntry("", namedAddress, parentGroup, true);
                    newEntry.IsSubAsset = true;
                    newEntry.ParentEntry = this;
                    newEntry.SetSubObjectType(o.GetType());
                    assets.Add(newEntry);
                }
            }
        }

        void GatherSpriteAtlasEntries(List<AddressableAssetEntry> assets)
        {
            var settings = parentGroup.Settings;
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetPath);
            var sprites = new Sprite[atlas.spriteCount];
            atlas.GetSprites(sprites);

            for (int i = 0; i < atlas.spriteCount; i++)
            {
                var spriteName = sprites[i] == null ? "missing reference" : sprites[i].name;
                if (sprites[i] == null)
                {
                    Debug.LogWarning(string.Format("NullReference in entry {0}\nAssetPath: {1}\nAddressableAssetGroup: {2}", address, AssetPath, parentGroup.Name));
                    assets.Add(new AddressableAssetEntry("", spriteName, parentGroup, true));
                }
                else
                {
                    if (spriteName.EndsWith("(Clone)", StringComparison.Ordinal))
                        spriteName = spriteName.Replace("(Clone)", "");

                    var namedAddress = string.Format("{0}[{1}]", address, spriteName);
                    var newEntry = settings.CreateEntry("", namedAddress, parentGroup, true);
                    newEntry.IsSubAsset = true;
                    newEntry.ParentEntry = this;
                    assets.Add(newEntry);
                }
            }
        }

        /// <summary>
        /// Gets an entry for this folder entry
        /// </summary>
        /// <param name="subAssetGuid"></param>
        /// <param name="subAssetPath"></param>
        /// <returns></returns>
        /// <remarks>Assumes that this asset entry is a valid folder asset</remarks>
        internal AddressableAssetEntry GetFolderSubEntry(string subAssetGuid, string subAssetPath)
        {
            string assetPath = AssetPath;
            if (string.IsNullOrEmpty(assetPath) || !subAssetPath.StartsWith(assetPath, StringComparison.Ordinal))
                return null;
            var settings = parentGroup.Settings;

            AddressableAssetEntry assetEntry = settings.FindAssetEntry(subAssetGuid);
            if (assetEntry != null)
            {
                if (assetEntry.IsSubAsset && assetEntry.ParentEntry == this)
                    return assetEntry;
                return null;
            }

            string relativePath = subAssetPath.Remove(0, assetPath.Length + 1);
            string[] splitRelativePath = relativePath.Split('/');
            string folderPath = assetPath;
            for (int i = 0; i < splitRelativePath.Length - 1; ++i)
            {
                folderPath = folderPath + "/" + splitRelativePath[i];
                string folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
                if (!AddressableAssetUtility.IsPathValidForEntry(folderPath))
                    return null;
                var folderEntry = settings.CreateSubEntryIfUnique(folderGuid, address + "/" + folderPath.Remove(assetPath.Length), this);
                if (folderEntry != null)
                {
                    throw new NotSupportedException(folderPath);
                }
                else
                    return null;
            }

            assetEntry = settings.CreateSubEntryIfUnique(subAssetGuid, address + "/" + relativePath, this);

            if (assetEntry == null || assetEntry.IsSubAsset == false)
                return null;
            return assetEntry;
        }

        static IEnumerable<string> GetResourceDirectories()
        {
            string[] resourcesGuids = AssetDatabase.FindAssets("Resources", new string[] {"Assets", "Packages"});
            foreach (string resourcesGuid in resourcesGuids)
            {
                string resourcesAssetPath = AssetDatabase.GUIDToAssetPath(resourcesGuid);
                if (resourcesAssetPath.EndsWith("/resources", StringComparison.OrdinalIgnoreCase) && AssetDatabase.IsValidFolder(resourcesAssetPath) && Directory.Exists(resourcesAssetPath))
                {
                    yield return resourcesAssetPath;
                }
            }
        }

        internal AddressableAssetEntry GetImplicitEntry(string implicitAssetGuid, string implicitAssetPath)
        {
            return GetFolderSubEntry(implicitAssetGuid, implicitAssetPath);
        }

        string GetRelativePath(string file, string path)
        {
            return file.Substring(path.Length);
        }

        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// Implementation of ISerializationCallbackReceiver.  Converts data from serializable form after deserialization.
        /// </summary>
        public void OnAfterDeserialize()
        {
            m_cachedMainAssetType = null;
            m_cachedAssetPath = null;
            m_CheckedIsScene = false;
            m_MainAsset = null;
            m_TargetAsset = null;
            SubAssets?.Clear();
        }

        /// <summary>
        /// Returns the address of the AddressableAssetEntry.
        /// </summary>
        /// <returns>The address of the AddressableAssetEntry</returns>
        public override string ToString()
        {
            return m_Address;
        }

        /// <summary>
        /// Create all entries for this addressable asset.  This will expand subassets (Sprites, Meshes, etc) and also different representations.
        /// </summary>
        /// <param name="entries">The list of entries to fill in.</param>
        /// <param name="dependencies">Keys of dependencies</param>
        /// <param name="depInfo">Map of guids to AssetLoadInfo for object identifiers in an Asset.  If null, ContentBuildInterface gathers object ids automatically.</param>
        /// <param name="includeAddress">Flag indicating if address locations should be included</param>
        /// <param name="assetsInBundle">The internal ids of the asset, typically shortened versions of the asset's GUID.</param>
        public void CreateCatalogEntries(List<ContentCatalogDataEntry> entries, IEnumerable<string> dependencies,
            Dictionary<GUID, AssetLoadInfo> depInfo, bool includeAddress, HashSet<string> assetsInBundle)
        {
            if (string.IsNullOrEmpty(AssetPath))
                return;

            string assetPath = GetAssetLoadPath(assetsInBundle);
            string key = CreateKeyList(includeAddress);
            if (key is null)
                return;

            //The asset may have previously been invalid. Since then, it may have been re-imported.
            //This can occur in particular when using ScriptedImporters with complex, multi-step behavior.
            //Double-check the type here in case the asset has been imported correctly after we cached its type.
            if (MainAssetType == typeof(DefaultAsset))
                m_cachedMainAssetType = null;

            Type mainType = AddressableAssetUtility.MapEditorTypeToRuntimeType(MainAssetType, false);
            if (mainType == null || mainType == typeof(DefaultAsset))
            {
                var t = MainAssetType;
                Debug.LogWarningFormat("Type {0} is in editor assembly {1}.  Asset location with internal id {2} will be stripped and not included in the build.", t.Name, t.Assembly.FullName, assetPath);
                return;
            }

            if (!IsScene)
            {
                ObjectIdentifier[] ids = depInfo != null
                    ? depInfo[new GUID(guid)].includedObjects.ToArray()
                    : ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(new GUID(guid), EditorUserBuildSettings.activeBuildTarget);

                foreach (var t in GatherMainAndReferencedSerializedTypes(ids))
                    entries.Add(new ContentCatalogDataEntry(t, assetPath, key, dependencies));
            }
            else if (mainType != null && mainType != typeof(DefaultAsset))
            {
                entries.Add(new ContentCatalogDataEntry(mainType, assetPath, key, dependencies));
            }
        }

        static internal IEnumerable<Type> GatherMainAndReferencedSerializedTypes(ObjectIdentifier[] ids)
        {
            if (ids.Length > 0)
            {
                Type[] typesForObjs = ContentBuildInterface.GetTypeForObjects(ids);
                HashSet<Type> typesSeen = new HashSet<Type>();
                foreach (var objType in typesForObjs)
                {
                    if (!typeof(UnityEngine.Object).IsAssignableFrom(objType))
                        continue;
                    if (typeof(Component).IsAssignableFrom(objType))
                        continue;
                    Type rtType = AddressableAssetUtility.MapEditorTypeToRuntimeType(objType, false);
                    if (rtType != null && rtType != typeof(DefaultAsset) && !typesSeen.Contains(rtType))
                    {
                        yield return rtType;
                        typesSeen.Add(rtType);
                    }
                }
            }
        }

        static internal List<Type> GatherMainObjectTypes(ObjectIdentifier[] ids)
        {
            List<Type> typesSeen = new List<Type>();
            foreach (ObjectIdentifier id in ids)
            {
                Type objType = ContentBuildInterface.GetTypeForObject(id);
                if (typeof(Component).IsAssignableFrom(objType))
                    continue;
                Type rtType = AddressableAssetUtility.MapEditorTypeToRuntimeType(objType, false);
                if (rtType != null && rtType != typeof(DefaultAsset) && !typesSeen.Contains(rtType))
                    typesSeen.Add(rtType);
            }

            return typesSeen;
        }
    }
}
