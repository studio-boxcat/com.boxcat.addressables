#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets.AsyncOperations;
using UnityEngine.AddressableAssets.Util;
using UnityEngine.Assertions;

namespace UnityEditor.AddressableAssets
{
    class EditorAssetOp<TObject> : IAssetOp<TObject> where TObject : UnityEngine.Object
    {
        readonly string _path;
        readonly DateTime _loadTime;

        TObject _result;
        Action<TObject> _onComplete;

        public EditorAssetOp(string path)
        {
            _path = path;

            var loadDelay = GetDelay();
            if (loadDelay == 0)
            {
                LoadImmediate();
                Assert.IsNotNull(_result, $"Failed to load asset: path={path}");
                return;
            }

            _loadTime = DateTime.Now.AddSeconds(loadDelay);
            Task.Delay((int) (loadDelay * 1000)).ContinueWith(_ => EditorApplication.delayCall += LoadImmediate);
        }

        public override string ToString() => $"EditorAssetOp:{_path} ({(_result != default ? "Loaded" : "Loading")})";

        public bool TryGetResult(out TObject result)
        {
            if (_result is not null)
            {
                result = _result;
                return true;
            }

            // Even if _result is null, we still want to return true if the load time has passed.
            if (DateTime.Now >= _loadTime)
            {
                LoadImmediate();
                Assert.IsNotNull(_result, $"Failed to load asset at path: {_path}");
                result = _result;
                return true;
            }

            result = null;
            return false;
        }

        public TObject WaitForCompletion()
        {
            if (_result is not null)
                return _result;
            LoadImmediate();
            Assert.IsNotNull(_result, $"Failed to load asset at path: {_path}");
            return _result;
        }

        void LoadImmediate()
        {
            // LoadImmediate will be called twice if the Result property is called before the task is complete.
            if (_result is not null)
                return;

            try
            {
                _result = AssetDatabase.LoadAssetAtPath<TObject>(_path);
            }
            catch (Exception e)
            {
                L.E("[EditorAssetOp] Failed to load asset: path=" + _path);
                L.E(e);
                throw;
            }

            var onComplete = _onComplete;
            _onComplete = null;
            onComplete?.Invoke(_result);
        }

        public void AddOnComplete(Action<TObject> onComplete)
        {
            if (TryGetResult(out var result))
            {
                Assert.IsNotNull(result, $"Failed to load asset: path={_path}");
                onComplete.SafeInvoke(result);
                return;
            }

            _onComplete += onComplete;
        }

        public void AddOnComplete(Action<TObject, object> onComplete, object payload)
            => AddOnComplete(obj => onComplete(obj, payload));

        public void AddOnComplete(Action<IAssetOp<TObject>, TObject, object> onComplete, object payload)
            => AddOnComplete(obj => onComplete(this, obj, payload));

        static float GetDelay()
        {
            var noDelay = UnityEngine.Random.value < 0.05f; // 5% chance of no delay.
            if (noDelay) return 0f;
            var loadDelay = UnityEngine.Random.Range(0, 0.3f); // 0s - 0.3s delay.
            return loadDelay;
        }
    }
}

#endif