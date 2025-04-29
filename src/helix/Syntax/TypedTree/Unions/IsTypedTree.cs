using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Unions {

    public record IsTypedTree : ITypedTree {
        public required TokenLocation Location { get; init; }

        public required HelixType ReturnType { get; init; }
        
        public required IdentifierPath VariablePath { get; init; }

        public required string MemberName { get; init; }

        public required UnionType UnionSignature { get; init; }

        public bool AlwaysJumps => false;

        public ICSyntax GenerateCode(TypeFrame flow, ICStatementWriter writer) {
            var varName = writer.GetVariableName(this.VariablePath);

            var index = this.UnionSignature
                .Members
                .Select(x => x.Name)
                .IndexOf(x => x == this.MemberName);

            return new CBinaryExpression {
                Operation = BinaryOperationKind.EqualTo,
                Left = new CMemberAccess {
                    Target = new CVariableLiteral(varName),
                    MemberName = "tag"
                },
                Right = new CIntLiteral(index)
            };
        }
    }
}