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
    public enum VariableCaptureKind {
        ValueCapture, LocationCapture
    }

    public record struct VariableCapture(IdentifierPath VariablePath, VariableCaptureKind Kind, PointerType Signature) { }

    public class TypeFrame : ITypeContext {
        private int tempCounter = 0;

        // Frame-specific things
        public IdentifierPath Scope { get; }

        public ImmutableDictionary<IdentifierPath, ISyntaxTree> SyntaxValues { get; set; }

        // Global things
        public Dictionary<IdentifierPath, HelixType> NominalSignatures { get; set; }

        public Dictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

        public Dictionary<ISyntaxTree, IReadOnlyList<VariableCapture>> CapturedVariables { get; }

        public Dictionary<ISyntaxTree, ISyntaxPredicate> Predicates { get; }

        // Explicit interface implementations
        IReadOnlyDictionary<IdentifierPath, ISyntaxTree> ITypeContext.GlobalSyntaxValues => this.SyntaxValues;

        IReadOnlyDictionary<IdentifierPath, HelixType> ITypeContext.GlobalNominalSignatures => this.NominalSignatures;

        IReadOnlyDictionary<ISyntaxTree, HelixType> ITypeContext.ReturnTypes => this.ReturnTypes;

        IReadOnlyDictionary<ISyntaxTree, IReadOnlyList<VariableCapture>> ITypeContext.CapturedVariables => this.CapturedVariables;

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

            this.Scope = new IdentifierPath();
            this.ReturnTypes = new Dictionary<ISyntaxTree, HelixType>();
            this.CapturedVariables = new Dictionary<ISyntaxTree, IReadOnlyList<VariableCapture>>();
            this.Predicates = new Dictionary<ISyntaxTree, ISyntaxPredicate>();
            this.NominalSignatures =  new Dictionary<IdentifierPath, HelixType>();
        }

        private TypeFrame(TypeFrame prev) {
            this.SyntaxValues = prev.SyntaxValues;

            this.Scope = prev.Scope;
            this.ReturnTypes = prev.ReturnTypes;
            this.CapturedVariables = prev.CapturedVariables;
            this.Predicates = prev.Predicates;
            this.NominalSignatures = prev.NominalSignatures;
        }

        public TypeFrame(TypeFrame prev, string scopeSegment) : this(prev) {
            this.Scope = prev.Scope.Append(scopeSegment);
        }

        public string GetVariableName() {
            return "$t_" + this.tempCounter++;
        }        

        public void MergeFrom(params TypeFrame[] subFrames) {
            var dict = new DefaultDictionary<IdentifierPath, HashSet<ISyntaxTree>>(_ => new HashSet<ISyntaxTree>());

            foreach (var frame in subFrames) {
                var locals = frame.SyntaxValues
                    .Where(x => x.Value.AsType(frame).HasValue)
                    .Where(x => {
                        if (!this.SyntaxValues.TryGetValue(x.Key, out var syntax)) {
                            return false;
                        }

                        if (!syntax.AsType(this).TryGetValue(out var type)) {
                            return false;
                        }

                        return x.Value.AsType(frame) != type;
                    })
                    .ToArray();

                foreach (var (key, value) in locals) {
                    dict[key].Add(value);
                }
            }

            foreach (var (key, valueSet) in dict) {
                if (valueSet.Count == 0) {
                    continue;
                }
                else if (valueSet.Count == 1) {
                    this.SyntaxValues = this.SyntaxValues.SetItem(key, valueSet.First());
                }
                else {
                    var oldSyntax = this.SyntaxValues[key];
                    var newType = this.SyntaxValues[key].AsType(this).GetValue().GetMutationSupertype(this);

                    this.SyntaxValues = this.SyntaxValues.SetItem(key, newType.ToSyntax(oldSyntax.Location));
                }
            }
        }
    }
}