using Helix.Syntax;
using Helix.Analysis.Flow;
using Helix.Analysis.Types;
using System.Collections.Immutable;

namespace Helix.Analysis.TypeChecking {
    public enum VariableCaptureKind {
        ValueCapture, LocationCapture
    }

    public record struct VariableCapture(IdentifierPath VariablePath, VariableCaptureKind Kind, PointerType Signature) { }

    public class TypeFrame {
        private int tempCounter = 0;

        // Frame-specific things
        public IdentifierPath Scope { get; }

        public ImmutableDictionary<IdentifierPath, LocalInfo> Locals { get; set; }
        
        // Global things
        public Dictionary<IdentifierPath, HelixType> NominalSignatures { get; }
        
        public TypeFrame() {
            this.Locals = ImmutableDictionary<IdentifierPath, LocalInfo>.Empty;

            this.Locals = this.Locals.Add(
                new IdentifierPath("void"),
                new LocalInfo(PrimitiveType.Void));

            this.Locals = this.Locals.Add(
                new IdentifierPath("word"),
                new LocalInfo(PrimitiveType.Word));

            this.Locals = this.Locals.Add(
                new IdentifierPath("bool"),
                new LocalInfo(PrimitiveType.Bool));

            this.NominalSignatures = new Dictionary<IdentifierPath, HelixType>();
            this.Scope = new IdentifierPath();
        }

        private TypeFrame(TypeFrame prev) {
            this.Scope = prev.Scope;
            this.NominalSignatures = prev.NominalSignatures;
            this.Locals = prev.Locals;
        }

        public TypeFrame(TypeFrame prev, string scopeSegment) : this(prev) {
            this.Scope = prev.Scope.Append(scopeSegment);
        }

        public string GetVariableName() {
            return "$t_" + this.tempCounter++;
        }
    }
}