using Attempt17.CodeGeneration;
using Attempt17.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Types {
    public class VoidType : LanguageType {
        public static VoidType Instance { get; } = new VoidType();

        private VoidType() { }

        public override bool Equals(object other) => other is VoidType;

        public override string GenerateCType() {
            return "short";
        }

        public override int GetHashCode() => 5;

        public override bool IsDefinedWithin(Scope scope) => true;

        public override string ToString() => "void";
    }
}