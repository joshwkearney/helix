using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attempt4 {
    public interface IInterpretedValue : IAnalyzedSyntax {
        object Value { get; }
    }

    public interface IInterpretedValue<T> : IInterpretedValue {
        new T Value { get; }
    }
}