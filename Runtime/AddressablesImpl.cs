using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace UnityEngine.AddressableAssets
{
    /// <summary>
    /// Entry point for ResourceManager API
    /// </summary>
    internal class AddressablesImpl : IAddressablesImpl
    {
        private readonly ResourceCatalog _catalog;
        private readonly AssetBundleLoader _loader;
        private readonly BundledAssetProvider _bundledAssetProvider = new();
        private readonly SceneProvider _sceneProvider = new();
        private readonly List<AssetOpBlock> _opBlockPool = new();


        public AddressablesImpl(string catalogUri)
        {
            _catalog = ResourceCatalog.Load(catalogUri);
            _loader = new AssetBundleLoader(_catalog.GetBundleCount(), _catalog.IndexToId);
            _loader.LoadMonoScriptBundle(); // load mono script bundle immediately
        }

        public IAssetOp<TObject> LoadAssetAsync<TObject>(Address address) where TObject : Object
        {
            var b = GetOpBlock(address, _bundledAssetProvider);
            return new AssetOp<TObject>(b);
        }

        public TObject LoadAsset<TObject>(Address address) where TObject : Object
        {
            var bundleIndex = _catalog.GetContainingBundle(address);
            return LoadAsset<TObject>(bundleIndex, address.Hex());
        }

        public IAssetOp<TObject> LoadAssetAsync<TObject>(AssetLocation loc) where TObject : Object
        {
            var b = GetOpBlock(loc, _bundledAssetProvider);
            return new AssetOp<TObject>(b);
        }

        public TObject LoadAsset<TObject>(AssetLocation loc) where TObject : Object
        {
            var bundleIndex = _catalog.GetBundleIndex(loc.BundleId);
            return LoadAsset<TObject>(bundleIndex, loc.AssetIndex.Name());
        }

        public IAssetOp<Scene> LoadSceneAsync(Address address)
        {
            var b = GetOpBlock(address, _sceneProvider);
            return new AssetOp<Scene>(b);
        }

        private TObject LoadAsset<TObject>(AssetBundleIndex bundleIndex, string assetName) where TObject : Object
        {
            if (_loader.TryGetResolvedBundle(bundleIndex, out var bundle) is false)
            {
                bundle = _loader.ResolveImmediate(
                    bundleIndex,
                    _catalog.GetDependencies(bundleIndex),
                    _catalog.IndexToId);
            }
            return bundle.LoadAsset<TObject>(assetName);
        }

        private AssetOpBlock GetOpBlock(AssetBundleIndex bundleIndex, string assetName, IResourceProvider provider)
        {
            var count = _opBlockPool.Count;

            AssetOpBlock b;
            if (count is 0)
            {
                // returned by AssetOpBlock.Return()
                b = new AssetOpBlock(_catalog, _loader, _opBlockPool);
            }
            else
            {
                b = _opBlockPool[count - 1];
                _opBlockPool.RemoveAt(count - 1);
            }

            b.Init(assetName, bundleIndex, provider);
            return b;
        }

        private AssetOpBlock GetOpBlock(Address address, IResourceProvider provider)
        {
            var bundleIndex = _catalog.GetContainingBundle(address);
            return GetOpBlock(bundleIndex, address.Hex(), provider);
        }

        private AssetOpBlock GetOpBlock(AssetLocation loc, IResourceProvider provider)
        {
            var bundleIndex = _catalog.GetBundleIndex(loc.BundleId);
            // L.I($"[Addressables] GetOpBlock: bundleId={loc.BundleId.Name()}, bundleIndex={bundleIndex.DebugString()}, assetName={loc.AssetIndex.Name()}");
            return GetOpBlock(bundleIndex, loc.AssetIndex.Name(), provider);
        }

#if UNITY_EDITOR
        public void Dispose()
        {
            _loader.Dispose();
            _catalog.Dispose(); // catalog must be disposed at the end. _loader internally holds a reference to it.
        }
#endif
    }
}