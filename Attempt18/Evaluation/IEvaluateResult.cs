using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt19.Evaluation {
    public interface IEvaluateResult {
        public object Value { get; }

        public IEvaluateResult Copy();
    }
}