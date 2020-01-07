using Attempt17.CodeGeneration;
using Attempt17.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Types {
    public class VariableType : LanguageType {
        public LanguageType InnerType { get; }

        public VariableType(LanguageType innerType) {
            this.InnerType = innerType;
        }

        public override bool Equals(object other) {
            if (other is null) {
                return false;
            }

            if (other is VariableType varType) {
                return this.InnerType == varType.InnerType;
            }

            return false;
        }

        public override int GetHashCode() => 7 * this.InnerType.GetHashCode();

        public override string ToString() => "var " + this.InnerType.ToString();

        public override bool IsDefinedWithin(Scope scope) => this.InnerType.IsDefinedWithin(scope);

        public override string GenerateCType() {
            return this.InnerType.GenerateCType() + "*";
        }
    }
}