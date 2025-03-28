using JetBrains.Annotations;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets.AsyncOperations;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.AddressableAssets.Util;
using UnityEngine.SceneManagement;

namespace UnityEngine.AddressableAssets
{
    /// <summary>
    /// Entry point for Addressable API, this provides a simpler interface than using ResourceManager directly as it assumes string address type.
    /// </summary>
    public static class Addressables
    {
        [CanBeNull]
        private static IAddressablesImpl _implCache;
        private static IAddressablesImpl _impl
        {
            get
            {
                if (_implCache is not null) return _implCache;
#if UNITY_EDITOR
                _implCache = EditorAddressablesImplFactory.Create();
                if (_implCache is not null) return _implCache;
#endif
                return _implCache = new AddressablesImpl(LoadCatalog());
            }
        }

        [MustUseReturnValue]
        public static IAssetOp<TObject> LoadAssetAsync<TObject>(string key) where TObject : Object
        {
            L.I($"[Addressables] Load Start: {key} ({typeof(TObject).Name}) ~ {_impl.GetType().Name}");
            var op = _impl.LoadAssetAsync<TObject>(key);
#if DEBUG
            op.AddOnComplete(static (o, payload) => L.I($"[Addressables] Load Done: {payload} - {o.name}"), key);
#endif
            return op;
        }

        [MustUseReturnValue]
        public static TObject LoadAsset<TObject>(string key) where TObject : Object
        {
            L.I($"[Addressables] Load: {key} ({typeof(TObject).Name}) ~ {_impl.GetType().Name}");
            return _impl.LoadAsset<TObject>(key);
        }

        public static IAssetOp<Scene> LoadSceneAsync(string key)
        {
            L.I($"[Addressables] Load Start: {key} (Scene) ~ {_impl.GetType().Name}");
            var op = _impl.LoadSceneAsync(key);
#if DEBUG
            op.AddOnComplete(static (o, payload) => L.I($"[Addressables] Load Done: {payload} - {o.name}"), key);
#endif
            return op;
        }

        private static ResourceCatalog LoadCatalog()
        {
#if DEBUG
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif

            var url = PathConfig.RuntimePath_CatalogBin;
#if UNITY_EDITOR || !UNITY_ANDROID
            url = "file://" + url;
#endif
            L.I("[Addressables] LoadCatalog: " + url);
            var req = UnityWebRequest.Get(url).SendWebRequest();
            req.WaitForComplete();
            Assert.AreEqual(req.webRequest.result, UnityWebRequest.Result.Success, "Failed to load catalog.");
            var catalog = new ResourceCatalog(req.webRequest.downloadHandler.data);

#if DEBUG
            sw.Stop();
            L.I("[Addressables] LoadCatalog: " + sw.ElapsedMilliseconds + "ms");
#endif

            return catalog;
        }

#if UNITY_EDITOR
        static Addressables()
        {
            UnityEditor.EditorApplication.playModeStateChanged += change =>
            {
                if (change is UnityEditor.PlayModeStateChange.EnteredEditMode
                    or UnityEditor.PlayModeStateChange.ExitingEditMode)
                {
                    Purge();
                }
            };
        }

        internal static void Purge()
        {
            if (_implCache is null)
                return;

            L.I("[Addressables] Purge");
            _implCache = null;

            AssetBundleLoader.Debug_UnloadAllAssetBundles();
        }
#endif
    }
}