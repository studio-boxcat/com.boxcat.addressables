using UnityEngine.Assertions;

namespace UnityEngine.AddressableAssets
{
    internal class BundledAssetProvider : IResourceProvider
    {
        public AsyncOperation LoadAsync<T>(AssetBundle bundle, string assetName)
        {
#if DEBUG
            var startTime = Time.unscaledTime;
            L.I($"[Addressables] AssetBundle.LoadAssetAsync: name={assetName}, bundle={bundle.name}, startTime={startTime}");
#endif
            Assert.IsTrue(bundle.Contains(assetName),
                $"Bundle does not contain asset: bundle={bundle.name}, assetName={assetName}, allAssets={string.Join(", ", bundle.GetAllAssetNames())}");

            var op = bundle.LoadAssetAsync(assetName, typeof(T));
#if DEBUG
            op.completed += op2 =>
            {
                var deltaTime = Time.unscaledTime - startTime;
                L.I("[Addressables] AssetBundle.LoadAssetAsync completed: " +
                    $"name={assetName}, asset={((AssetBundleRequest) op2).asset}, bundle={bundle.name}, time={deltaTime}");
            };
#endif
            return op;
        }

        public object GetResult(AsyncOperation op)
        {
            return ((AssetBundleRequest) op).asset;
        }
    }
}