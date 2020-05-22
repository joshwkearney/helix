﻿using Attempt17.NewSyntax;

namespace Attempt18.Types {
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

        public override T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitVariableType(this);
        }

        public override string ToFriendlyString() {
            return "var_" + this.InnerType.ToFriendlyString();
        }

        public override TypeCopiability GetCopiability() {
            return TypeCopiability.Conditional;
        }
    }
}