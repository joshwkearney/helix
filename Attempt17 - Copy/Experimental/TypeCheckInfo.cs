using Attempt17.Types;
using System.Collections.Immutable;

namespace Attempt17.Experimental {
    public class TypeCheckInfo {
        public LanguageType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> CapturedVariables { get; }

        public TypeCheckInfo(LanguageType retType, ImmutableHashSet<IdentifierPath> capturedVariables) {
            this.ReturnType = retType;
            this.CapturedVariables = capturedVariables;
        }

        public TypeCheckInfo(LanguageType retType) : this(retType, ImmutableHashSet<IdentifierPath>.Empty) { }
    }
}