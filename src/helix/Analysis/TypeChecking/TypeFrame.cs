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

namespace Helix.Analysis.TypeChecking
{
    public delegate void DeclarationCG(ICWriter writer);

    public enum VariableCaptureKind {
        ValueCapture, LocationCapture
    }

    public record struct VariableCapture(IdentifierPath VariablePath, VariableCaptureKind Kind) { }

    public class TypeFrame : ITypedFrame {
        private int tempCounter = 0;

        // Frame-specific things
        public ImmutableDictionary<IdentifierPath, ISyntaxTree> SyntaxValues { get; set; }

        // Global things
        public IDictionary<IdentifierPath, VariableSignature> Variables { get; }

        public IDictionary<IdentifierPath, FunctionSignature> Functions { get; }

        public IDictionary<IdentifierPath, StructSignature> Structs { get; }

        public IDictionary<IdentifierPath, StructSignature> Unions { get; }

        public IDictionary<HelixType, DeclarationCG> TypeDeclarations { get; }

        public IDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

        public IDictionary<ISyntaxTree, IReadOnlyList<VariableCapture>> CapturedVariables { get; }

        public IDictionary<ISyntaxTree, ISyntaxPredicate> Predicates { get; }

        public TypeFrame() {
            this.Variables = new Dictionary<IdentifierPath, VariableSignature>();

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

            this.Functions = new Dictionary<IdentifierPath, FunctionSignature>();
            this.Structs = new Dictionary<IdentifierPath, StructSignature>(); 
            this.Unions = new Dictionary<IdentifierPath, StructSignature>();
            this.TypeDeclarations = new Dictionary<HelixType, DeclarationCG>();

            this.ReturnTypes = new Dictionary<ISyntaxTree, HelixType>();
            this.CapturedVariables = new Dictionary<ISyntaxTree, IReadOnlyList<VariableCapture>>();
            this.Predicates = new Dictionary<ISyntaxTree, ISyntaxPredicate>();
        }

        public TypeFrame(TypeFrame prev) {
            this.SyntaxValues = prev.SyntaxValues;

            this.Variables = prev.Variables;
            this.Functions = prev.Functions;
            this.Structs = prev.Structs;
            this.Unions = prev.Unions;
            this.TypeDeclarations = prev.TypeDeclarations;

            this.ReturnTypes = prev.ReturnTypes;
            this.CapturedVariables = prev.CapturedVariables;
            this.Predicates = prev.Predicates;
        }

        public string GetVariableName() {
            return "$t_" + this.tempCounter++;
        }

        public bool TryResolvePath(IdentifierPath scope, string name, out IdentifierPath path) {
            while (true) {
                path = scope.Append(name);
                if (this.SyntaxValues.ContainsKey(path)) {
                    return true;
                }

                if (scope.Segments.Any()) {
                    scope = scope.Pop();
                }
                else {
                    return false;
                }
            }
        }

        public IdentifierPath ResolvePath(IdentifierPath scope, string path) {
            if (this.TryResolvePath(scope, path, out var value)) {
                return value;
            }

            throw new InvalidOperationException(
                $"Compiler error: The path '{path}' does not contain a value.");
        }

        public bool TryResolveName(IdentifierPath scope, string name, out ISyntaxTree value) {
            if (!this.TryResolvePath(scope, name, out var path)) {
                value = null;
                return false;
            }

            return this.SyntaxValues.TryGetValue(path, out value);
        }

        public ISyntaxTree ResolveName(IdentifierPath scope, string name) {
            return this.SyntaxValues[this.ResolvePath(scope, name)];
        }

        public void DeclareVariableSignatures(IdentifierPath basePath, HelixType baseType, bool isWritable) {
            foreach (var (compPath, compType) in baseType.GetMembers(this)) {
                var path = basePath.Append(compPath);
                var sig = new VariableSignature(path, compType, isWritable);

                // Add this variable's lifetime
                this.Variables[path] = sig;
            }
        }
    }
}