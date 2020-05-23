using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt18.Evaluation {
    public interface IEvaluateResult {
        public object Value { get; }

        public IEvaluateResult Copy();
    }
}