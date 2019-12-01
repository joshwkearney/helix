using Attempt12.TypeSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12.Analyzing {
    public interface ISyntax {
        AnalyticScope Scope { get; }

        ISymbol TypeSymbol { get; }

        void Accept(ISyntaxVisitor visitor);
    }
}