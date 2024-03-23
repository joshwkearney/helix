using Helix.Common.Hir;
using Helix.Common.Tokens;
using Helix.Common.Types;
using System.Reflection.Emit;

namespace Helix.Common.Hmm {
    public interface IHirSyntax {
        public TokenLocation Location { get; }

        public T Accept<T>(IHirVisitor<T> visitor);

        public string ToString() {
            var visitor = new HirStringifier();

            return this.Accept(visitor);
        }
    }

    public record HirIntrinsicUnionMemberAccess : IHirSyntax {
        public required string Result { get; init; }

        public required IHelixType ResultType { get; init; }

        public required string Operand { get; init; }

        public required string UnionMember { get; init; }

        public TokenLocation Location { get; init; }

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitIntrinsicUnionMemberAccess(this);
    }

    public record HirDereference : IHirSyntax {
        public required string Result { get; init; }

        public required IHelixType ResultType { get; init; }

        public required string Operand { get; init; }

        public required TokenLocation Location { get; init; }

        public required bool IsLValue { get; init; } = false;

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitDereference(this);
    }

    public record HirIndex : IHirSyntax {
        public required string Result { get; init; }

        public required IHelixType ResultType { get; init; }

        public required string Operand { get; init; }

        public required string Index { get; init; }

        public required TokenLocation Location { get; init; }

        public required bool IsLValue { get; init; } = false;

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitIndex(this);
    }

    public record HirAddressOf : IHirSyntax {
        public required string Result { get; init; }

        public required IHelixType ResultType { get; init; }

        public required string Operand { get; init; }

        public required TokenLocation Location { get; init; }

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitAddressOf(this);
    }

    public record HirArrayLiteral : IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required IHelixType ResultType { get; init; }

        public required IReadOnlyList<string> Args { get; init; }

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitArrayLiteral(this);
    }

    public record HirBinarySyntax : IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required IHelixType ResultType { get; init; }

        public required BinaryOperationKind Operator { get; init; }

        public required string Left { get; init; }

        public required string Right { get; init; }

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitBinarySyntax(this);
    }

    public record HirFunctionDeclaration : IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required FunctionType Signature { get; init; }

        public required string Name { get; init; }

        public required IReadOnlyList<IHirSyntax> Body { get; init; }

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitFunctionDeclaration(this);
    }

    public record HirIfExpression : IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required string Condition { get; init; }

        public required IReadOnlyList<IHirSyntax> AffirmativeBody { get; init; }

        public required IReadOnlyList<IHirSyntax> NegativeBody { get; init; }

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitIfExpression(this);
    }

    public record HirInvokeSyntax : IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required IHelixType ResultType { get; init; }

        public required string Target { get; init; }

        public IReadOnlyList<string> Arguments { get; init; } = [];

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitInvoke(this);
    }

    public record HirIsSyntax : IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required IHelixType ResultType { get; init; }

        public required string Operand { get; init; }

        public required string Field { get; init; }

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitIs(this);
    }

    public record HirLoopSyntax : IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required IReadOnlyList<IHirSyntax> Body { get; init; }

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitLoop(this);
    }

    public record HirMemberAccess : IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required IHelixType ResultType { get; init; }

        public required string Operand { get; init; }

        public required string Member { get; init; }

        public required bool IsLValue { get; init; } = false;

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitMemberAccess(this);
    }

    public record HirNewSyntax : IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required IHelixType ResultType { get; init; }

        public IReadOnlyList<HmmNewFieldAssignment> Assignments { get; init; } = [];

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitNew(this);
    }

    public record HirNewFieldAssignment {
        public required string Value { get; init; }

        public required string Field { get; init; }
    }

    public record HirUnaryOperator : IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required IHelixType ResultType { get; init; }

        public required UnaryOperatorKind Operator { get; init; }

        public required string Operand { get; init; }

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitUnaryOperator(this);
    }

    public record HirVariableStatement : IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required string Variable { get; init; }

        public required IHelixType VariableType { get; init; }

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitVariableStatement(this);
    }
}