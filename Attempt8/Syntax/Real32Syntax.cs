using Attempt12.TypeSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12.Analyzing {
    public struct Real32Syntax : ISyntax {
        public float Value { get; }

        public AnalyticScope Scope { get; }

        public ISymbol TypeSymbol => PrimitiveTypes.Float32Type;

        public Real32Syntax(float value, AnalyticScope scope) {
            this.Value = value;
            this.Scope = scope;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}