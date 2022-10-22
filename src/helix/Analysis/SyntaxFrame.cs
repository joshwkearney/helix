using Helix.Analysis.Types;
using Helix.Features;
using Helix.Features.Aggregates;
using Helix.Features.Functions;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Parsing;

namespace Helix.Analysis {
    public delegate void DeclarationCG(ICWriter writer);

    public class IfBranches {
        public ISyntaxTree TrueBranch { get; set; }

        public ISyntaxTree FalseBranch { get; set; }

        public HelixType ReturnType { get; set; }

        public SyntaxFrame TrueFrame { get; set; }

        public SyntaxFrame FalseFrame { get; set; }
    }

    public class SyntaxFrame {
        private int tempCounter = 0;

        // Frame-specific things
        public IDictionary<IdentifierPath, VariableSignature> Variables { get; }

        public IDictionary<IdentifierPath, ISyntaxTree> SyntaxValues { get; }

        public IDictionary<IdentifierPath, IfBranches> IfBranches { get; }

        // Global things
        public IDictionary<IdentifierPath, FunctionSignature> Functions { get; }

        public IDictionary<IdentifierPath, AggregateSignature> Aggregates { get; }

        public IDictionary<HelixType, DeclarationCG> TypeDeclarations { get; }

        public IDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

        // Represents variables captured by individual syntax elements
        // Used for escape analysis and eventually for closure conversion
        public IDictionary<ISyntaxTree, IReadOnlyList<IdentifierPath>> CapturedVariables { get; }

        public SyntaxFrame() {
            this.Variables = new Dictionary<IdentifierPath, VariableSignature>();
            this.IfBranches = new Dictionary<IdentifierPath, IfBranches>();
            this.CapturedVariables = new Dictionary<ISyntaxTree, IReadOnlyList<IdentifierPath>>();

            this.SyntaxValues = new Dictionary<IdentifierPath, ISyntaxTree>() {
                { new IdentifierPath("void"), new TypeSyntax(default, PrimitiveType.Void) },
                { new IdentifierPath("int"), new TypeSyntax(default, PrimitiveType.Int) },
                { new IdentifierPath("bool"), new TypeSyntax(default, PrimitiveType.Bool) }
            };

            this.Functions = new Dictionary<IdentifierPath, FunctionSignature>();
            this.Aggregates = new Dictionary<IdentifierPath, AggregateSignature>();

            this.TypeDeclarations = new Dictionary<HelixType, DeclarationCG>();
            this.ReturnTypes = new Dictionary<ISyntaxTree, HelixType>();
        }

        public SyntaxFrame(SyntaxFrame prev) {
            this.Variables = new StackedDictionary<IdentifierPath, VariableSignature>(prev.Variables);
            this.SyntaxValues = new StackedDictionary<IdentifierPath, ISyntaxTree>(prev.SyntaxValues);

            this.IfBranches = prev.IfBranches;
            this.Functions = prev.Functions;
            this.Aggregates = prev.Aggregates;

            this.TypeDeclarations = prev.TypeDeclarations;
            this.ReturnTypes = prev.ReturnTypes;
            this.CapturedVariables = prev.CapturedVariables;
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
    }
}