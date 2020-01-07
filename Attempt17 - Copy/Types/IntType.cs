using Attempt17.CodeGeneration;
using Attempt17.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Types {
    public class IntType : LanguageType {
        public static IntType Instance { get; } = new IntType();

        private IntType() { }

        public override bool Equals(object other) => other is IntType;

        public override string GenerateCType() {
            return "long long";
        }

        public override int GetHashCode() => 7;

        public override bool IsDefinedWithin(Scope scope) => true;

        public override string ToString() => "int";
    }
}