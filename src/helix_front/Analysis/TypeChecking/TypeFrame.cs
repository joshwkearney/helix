using Helix.Syntax;
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

        public ImmutableDictionary<IdentifierPath, HelixType> Locals { get; set; }

        // Global things
        public Dictionary<IdentifierPath, HelixType> NominalSignatures { get; }

        public Dictionary<ISyntaxTree, SyntaxTag> SyntaxTags { get; }

        public TypeFrame() {
            this.Locals = ImmutableDictionary<IdentifierPath, HelixType>.Empty;

            this.Locals = this.Locals.Add(
                new IdentifierPath("void"),
                PrimitiveType.Void);

            this.Locals = this.Locals.Add(
                new IdentifierPath("word"),
                PrimitiveType.Word);

            this.Locals = this.Locals.Add(
                new IdentifierPath("bool"),
                PrimitiveType.Bool);

            this.NominalSignatures = new Dictionary<IdentifierPath, HelixType>();
            this.Scope = new IdentifierPath();
            this.SyntaxTags = new Dictionary<ISyntaxTree, SyntaxTag>();
        }

        private TypeFrame(TypeFrame prev) {
            this.Scope = prev.Scope;

            this.SyntaxTags = prev.SyntaxTags;
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