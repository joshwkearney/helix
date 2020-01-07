using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Experimental.Features.Variables {
    public class VariableInitSyntax<T> : ISyntax<T> {
        public string VariableName { get; }

        public ISyntax<T> Value { get; }

        public VariableInitKind Kind { get; }

        public T Tag { get; }

        public VariableInitSyntax(T tag, string name, VariableInitKind kind, ISyntax<T> value) {
            this.Tag = tag;
            this.VariableName = name;
            this.Kind = kind;
            this.Value = value;
        }
    }
}
