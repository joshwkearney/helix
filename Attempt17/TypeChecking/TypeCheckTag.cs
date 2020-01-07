using Attempt17.Types;
using System.Collections.Immutable;

namespace Attempt17.TypeChecking {
    public class TypeCheckTag {
        public LanguageType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> CapturedVariables { get; }

        public TypeCheckTag(LanguageType retType, ImmutableHashSet<IdentifierPath> capturedVariables) {
            this.ReturnType = retType;
            this.CapturedVariables = capturedVariables;
        }

        public TypeCheckTag(LanguageType retType) : this(retType, ImmutableHashSet<IdentifierPath>.Empty) { }
    }
}