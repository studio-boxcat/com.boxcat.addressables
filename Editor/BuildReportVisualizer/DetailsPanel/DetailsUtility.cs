#if UNITY_2022_2_OR_NEWER
namespace Bundles.Editor
{
    internal static class DetailsUtility
    {
        public static BuildLayout.ExplicitAsset GetAsset(object item)
        {
            if (item is IBundlesBuildReportAsset)
                return (item as IBundlesBuildReportAsset).ExplicitAsset;
            else if (item is BuildLayout.ExplicitAsset)
                return item as BuildLayout.ExplicitAsset;
            return null;
        }

        public static BuildLayout.DataFromOtherAsset GetOtherAssetData(object item)
        {
            if (item is IBundlesBuildReportAsset)
                return (item as IBundlesBuildReportAsset).DataFromOtherAsset;
            else if (item is BuildLayout.DataFromOtherAsset)
                return item as BuildLayout.DataFromOtherAsset;
            return null;
        }

        public static bool IsBundle(object item)
        {
            if (item is IBundlesBuildReportBundle || item is BuildLayout.Bundle)
                return true;

            return false;
        }

        public static BuildLayout.Bundle GetBundle(object item)
        {
            BuildLayout.Bundle bundle = null;

            if (item is IBundlesBuildReportBundle)
                bundle = (item as IBundlesBuildReportBundle).Bundle;
            else if (item is BuildLayout.Bundle)
                bundle = item as BuildLayout.Bundle;

            return bundle;
        }
    }
}
#endif
