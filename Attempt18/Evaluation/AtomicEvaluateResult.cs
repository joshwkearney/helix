using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt18.Evaluation {
    public class AtomicEvaluateResult : IEvaluateResult {
        public object Value { get; }

        public AtomicEvaluateResult(object value) {
            this.Value = value;
        }

        public IEvaluateResult Copy() {
            return this;
        }
    }
}
