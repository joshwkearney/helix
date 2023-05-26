using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Collections;
using Helix.Features.Aggregates;
using Helix.Features.Variables;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Unions {
    public record IsParseSyntax : ISyntaxTree {
        public TokenLocation Location { get; init; }

        public ISyntaxTree Target { get; init; }

        public string MemberName { get; init; }

        public bool IsPure => this.Target.IsPure;

        public IEnumerable<ISyntaxTree> Children => this.Target.Children;

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.Target is not VariableAccessParseSyntax access) {
                throw new TypeException(
                    this.Target.Location, 
                    "Invalid 'is' syntax", 
                    "Only variable names referring to unions can be used with the 'is' keyword.");
            }

            // Make sure this name exists
            if (!types.TryResolvePath(this.Location.Scope, access.Name, out var path)) {
                throw TypeException.VariableUndefined(this.Location, access.Name);
            }

            // Make sure we have a variable
            if (!types.TryGetVariable(path, out var varSig)) {
                throw TypeException.VariableUndefined(this.Target.Location, access.Name);
            }

            // Make sure we have a variable pointing to a union
            if (varSig.InnerType.AsUnion(types).TryGetValue(out var unionSig)) {
                throw TypeException.ExpectedUnionType(this.Target.Location);
            }

            // Make sure this union actually contains this member
            if (!unionSig.Members.Any(x => x.Name == this.MemberName)) {
                throw TypeException.MemberUndefined(
                    this.Location, 
                    new NominalType(path, NominalTypeKind.Union), 
                    this.MemberName);
            }

            var predicate = new IsUnionMemberPredicate(
                path,
                new[] { this.MemberName }.ToValueSet(),
                unionSig);

            var returnType = new PredicateBool(predicate);

            var result = new IsSyntax() {
                Location = this.Location,
                MemberName = this.MemberName,
                UnionSignature = unionSig,
                VariablePath = path
            };

            result.SetReturnType(returnType, types);
            result.SetPredicate(types);
            result.SetCapturedVariables(path, VariableCaptureKind.ValueCapture, varSig, types);

            return result;
        }
    }

    public record IsSyntax : ISyntaxTree {
        public TokenLocation Location { get; init; }

        public IdentifierPath VariablePath { get; init; }

        public string MemberName { get; init; }

        public UnionType UnionSignature { get; init; }

        public IEnumerable<ISyntaxTree> Children => Array.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public ISyntaxTree CheckTypes(TypeFrame types) => this;

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            this.SetLifetimes(new LifetimeBundle(), flow);
        }

        public ICSyntax GenerateCode(FlowFrame flow, ICStatementWriter writer) {
            var varName = writer.GetVariableName(this.VariablePath);

            var index = this.UnionSignature
                .Members
                .Select(x => x.Name)
                .IndexOf(x => x == this.MemberName);

            return new CBinaryExpression() {
                Operation = Primitives.BinaryOperationKind.EqualTo,
                Left = new CMemberAccess() {
                    Target = new CVariableLiteral(varName),
                    MemberName = "tag"
                },
                Right = new CIntLiteral(index)
            };
        }
    }
}