using Helix.Syntax;
using Helix.Analysis.Flow;
using Helix.Analysis.Types;
using Helix.Features;
using Helix.Features.Aggregates;
using Helix.Features.Functions;
using Helix.Generation;
using Helix.Parsing;
using Helix.Collections;
using System.Collections.Immutable;
using Helix.Analysis.Predicates;
using Helix.Features.Types;

namespace Helix.Analysis.TypeChecking {
    public delegate void DeclarationCG(ICWriter writer);

    public enum VariableCaptureKind {
        ValueCapture, LocationCapture
    }

    public record struct VariableCapture(IdentifierPath VariablePath, VariableCaptureKind Kind, PointerType Signature) { }

    public class TypeFrame : ITypedFrame {
        private int tempCounter = 0;

        // Frame-specific things
        public ImmutableDictionary<IdentifierPath, ISyntaxTree> SyntaxValues { get; set; }

        public ImmutableDictionary<IdentifierPath, HelixType> NominalSignatures { get; set; }

        // Global things
        public IDictionary<HelixType, DeclarationCG> TypeDeclarations { get; }

        public IDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

        public IDictionary<ISyntaxTree, IReadOnlyList<VariableCapture>> CapturedVariables { get; }

        public IDictionary<ISyntaxTree, ISyntaxPredicate> Predicates { get; }

        public TypeFrame() {
            this.SyntaxValues = ImmutableDictionary<IdentifierPath, ISyntaxTree>.Empty;

            this.SyntaxValues = this.SyntaxValues.Add(
                new IdentifierPath("void"),
                new TypeSyntax(default, PrimitiveType.Void));

            this.SyntaxValues = this.SyntaxValues.Add(
                new IdentifierPath("int"),
                new TypeSyntax(default, PrimitiveType.Int));

            this.SyntaxValues = this.SyntaxValues.Add(
                new IdentifierPath("bool"),
                new TypeSyntax(default, PrimitiveType.Bool));

            this.TypeDeclarations = new Dictionary<HelixType, DeclarationCG>();

            this.ReturnTypes = new Dictionary<ISyntaxTree, HelixType>();
            this.CapturedVariables = new Dictionary<ISyntaxTree, IReadOnlyList<VariableCapture>>();
            this.Predicates = new Dictionary<ISyntaxTree, ISyntaxPredicate>();
            this.NominalSignatures = ImmutableDictionary<IdentifierPath, HelixType>.Empty;
        }

        public TypeFrame(TypeFrame prev) {
            this.SyntaxValues = prev.SyntaxValues;

            this.TypeDeclarations = prev.TypeDeclarations;

            this.ReturnTypes = prev.ReturnTypes;
            this.CapturedVariables = prev.CapturedVariables;
            this.Predicates = prev.Predicates;
            this.NominalSignatures = prev.NominalSignatures;
        }

        public string GetVariableName() {
            return "$t_" + this.tempCounter++;
        }        
    }
}