using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bundles.Editor
{
    /// <summary>
    /// Represents the data acquired after analyzing Bundles assets.
    /// </summary>
    [Serializable]
    internal class BundlesAnalyzeResultData : ISerializationCallbackReceiver
    {
        [Serializable]
        private class RuleToResults
        {
            [SerializeField]
            public string RuleName;

            [SerializeField]
            public List<AnalyzeRule.AnalyzeResult> Results;

            public RuleToResults(string ruleName, List<AnalyzeRule.AnalyzeResult> results)
            {
                RuleName = ruleName;
                Results = results;
            }
        }

        [SerializeField]
        private List<RuleToResults> m_RuleToResults = new List<RuleToResults>();

        internal Dictionary<string, List<AnalyzeRule.AnalyzeResult>> Data =
            new Dictionary<string, List<AnalyzeRule.AnalyzeResult>>();

        /// <summary>
        /// Retrieves serialized data after a domain reload.
        /// </summary>
        public void OnAfterDeserialize()
        {
            for (int i = 0; i < m_RuleToResults.Count; i++)
                Data.Add(m_RuleToResults[i].RuleName, m_RuleToResults[i].Results);
        }

        /// <summary>
        /// Converts our data to a serialized structure before a domain reload.
        /// </summary>
        public void OnBeforeSerialize()
        {
            m_RuleToResults.Clear();

            foreach (var key in Data.Keys)
                m_RuleToResults.Add(new RuleToResults(key, Data[key]));
        }

        internal void Clear(AnalyzeRule rule)
        {
            Clear(rule.ruleName);
        }

        internal void Clear(string ruleName)
        {
            if (Data.ContainsKey(ruleName))
                Data[ruleName].Clear();
        }
    }
}
