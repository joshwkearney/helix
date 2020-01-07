using Attempt17.CodeGeneration;
using Attempt17.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Types {
    public abstract class LanguageType {
        public abstract bool IsDefinedWithin(Scope scope);

        public abstract string GenerateCType();

        public abstract override bool Equals(object other);

        public abstract override int GetHashCode();

        public abstract override string ToString();

        public static bool operator ==(LanguageType type1, LanguageType type2) {
            return type1.Equals(type2);
        }

        public static bool operator !=(LanguageType type1, LanguageType type2) {
            return !type1.Equals(type2);        
        }
    }
}