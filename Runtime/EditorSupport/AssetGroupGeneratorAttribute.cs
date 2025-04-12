#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace UnityEngine.AddressableAssets
{
    public readonly struct AssetGroupGenerationDef
    {
        [CanBeNull] public readonly string GroupName;
        public readonly (string Address, string Path)[] Assets;
        public readonly byte? BundleMinor; // BundleMajor + BundleMinor = BundleId

        public AssetGroupGenerationDef(string groupName, byte bundleMinor, params string[] assetPaths)
        {
            GroupName = groupName;
            Assets = assetPaths
                .Select((path, i) => (i.ToStringSmallNumber(), path))
                .ToArray();
            BundleMinor = bundleMinor;
        }

        public AssetGroupGenerationDef(string groupName, byte bundleMinor, IEnumerable<string> assetPaths)
            : this(groupName, bundleMinor, assetPaths.ToArray()) { }

        public AssetGroupGenerationDef(string groupName, params (string Address, string Path)[] assets)
        {
            GroupName = groupName;
            Assets = assets;
            BundleMinor = null;
        }
    }

    [AttributeUsage(AttributeTargets.Method), MeansImplicitUse]
    public class AssetGroupGeneratorAttribute : Attribute
    {
        public readonly AssetBundleMajor? BundleMajor; // if set, can access bundle directly

        public AssetGroupGeneratorAttribute()
        {
            BundleMajor = null;
        }

        public AssetGroupGeneratorAttribute(AssetBundleMajor bundleMajor)
        {
            BundleMajor = bundleMajor;
        }
    }
}
#endif