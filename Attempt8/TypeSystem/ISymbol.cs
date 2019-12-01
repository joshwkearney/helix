using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12.TypeSystem {
    public interface ISymbol {
        ISymbol BaseType { get; }
    }
}