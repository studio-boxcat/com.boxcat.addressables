#if UNITY_2022_2_OR_NEWER
using UnityEditor.AddressableAssets.Build.Layout;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal interface IBuildReportConsumer
    {
        void Consume(BuildLayout buildReport);
    }

}
#endif
