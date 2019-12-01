using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt12 {
    public class InterpretedValue {
        public object Value { get; }

        public InterpretedValue(object value) {
            this.Value = value;
        }
    }

    public class ConstantInterpretedValue : InterpretedValue {
        public ISyntaxTree Syntax { get; }

        public ConstantInterpretedValue(object value, ISyntaxTree syntax) : base(value) {
            this.Syntax = syntax;
        }
    }
}