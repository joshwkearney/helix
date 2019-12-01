using Attempt12.TypeSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12.Analyzing {
    public struct Int32Syntax : ISyntax {
        public int Value { get; }

        public AnalyticScope Scope { get; }

        public ISymbol TypeSymbol => PrimitiveTypes.Int32Type;

        public Int32Syntax(int value, AnalyticScope scope) {
            this.Value = value;
            this.Scope = scope;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}