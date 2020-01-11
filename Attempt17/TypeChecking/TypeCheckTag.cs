using Attempt17.Types;
using System.Collections.Immutable;

namespace Attempt17.TypeChecking {
    public class TypeCheckTag {
        public LanguageType ReturnType { get; }

        public ImmutableHashSet<VariableCapture> CapturedVariables { get; }

        public TypeCheckTag(LanguageType retType, ImmutableHashSet<VariableCapture> capturedVariables) {
            this.ReturnType = retType;
            this.CapturedVariables = capturedVariables;
        }

        public TypeCheckTag(LanguageType retType) : this(retType, ImmutableHashSet<VariableCapture>.Empty) { }
    }
}