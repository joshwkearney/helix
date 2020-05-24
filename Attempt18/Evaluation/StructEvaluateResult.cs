using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt19.Evaluation {
    public class StructEvaluateResult : IEvaluateResult {
        public IReadOnlyDictionary<string, IEvaluateResult> Members { get; }

        public object Value => this.Members;

        public StructEvaluateResult(IReadOnlyDictionary<string, IEvaluateResult> dict) {
            this.Members = dict;
        }

        public IEvaluateResult Copy() {
            var newDict = new Dictionary<string, IEvaluateResult>();
            
            foreach (var (key, value) in this.Members) {
                newDict.Add(key, value.Copy());
            }

            return new StructEvaluateResult(newDict);
        }
    }
}