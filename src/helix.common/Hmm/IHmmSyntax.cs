using Helix.Common.Tokens;
using Helix.Common.Types;
using System.Reflection.Emit;

namespace Helix.Common.Hmm {
    public interface IHmmSyntax {
        public TokenLocation Location { get; }

        public T Accept<T>(IHmmVisitor<T> visitor);

        public string ToString() {
            var visitor = new HmmStringifier();

            return this.Accept(visitor);
        }
    }

    public interface IHmmExpression : IHmmSyntax {
        public string Result { get; }
    }

    public record HmmDereference : IHmmExpression {
        public required string Result { get; init; }

        public required string Operand { get; init; }

        public required TokenLocation Location { get; init; }

        public required bool IsLValue { get; init; } = false;

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitDereference(this);
    }

    public record HmmIndex : IHmmExpression {
        public required string Result { get; init; }

        public required string Operand { get; init; }

        public required string Index { get; init; }

        public required TokenLocation Location { get; init; }

        public required bool IsLValue { get; init; } = false;

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitIndex(this);
    }

    public record HmmAddressOf : IHmmExpression {
        public required string Result { get; init; }

        public required string Operand { get; init; }

        public required TokenLocation Location { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitAddressOf(this);
    }

    public record HmmTypeDeclaration : IHmmSyntax, IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required string Name { get; init; }

        public required TypeDeclarationKind Kind { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitTypeDeclaration(this);

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitTypeDeclaration(this);
    }

    public enum TypeDeclarationKind {
        Struct, Union, Function
    }

    public record HmmFunctionForwardDeclaration : IHmmSyntax, IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required string Name { get; init; }

        public required FunctionSignature Signature { get; init; }

        public required IHelixType Type { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitFunctionForwardDeclaration(this);

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitFunctionForwardDeclaration(this);
    }

    public record HmmStructDeclaration : IHmmSyntax, IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required IHelixType Type { get; init; }

        public required StructSignature Signature { get; init; }

        public required string Name { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitStructDeclaration(this);

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitStructDeclaration(this);
    }

    public record HmmUnionDeclaration : IHmmSyntax, IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required IHelixType Type { get; init; }

        public required UnionSignature Signature { get; init; }

        public required string Name { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitUnionDeclaration(this);

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitUnionDeclaration(this);
    }

    public record HmmArrayLiteral : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required IReadOnlyList<string> Args { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitArrayLiteral(this);
    }

    public record HmmAssignment : IHmmSyntax, IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required string Variable { get; init; }

        public required string Value { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitAssignment(this);

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitAssignment(this);
    }

    public record HmmAsSyntax : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required string Operand { get; init; }

        public required IHelixType Type { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitAsSyntax(this);
    }

    public record HmmBinarySyntax : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required BinaryOperationKind Operator { get; init; }

        public required string Left { get; init; }

        public required string Right { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitBinarySyntax(this);
    }

    public record HmmBreakSyntax : IHmmSyntax, IHirSyntax {
        public required TokenLocation Location { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitBreak(this);

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitBreak(this);
    }

    public record HmmContinueSyntax : IHmmSyntax, IHirSyntax {
        public required TokenLocation Location { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitContinue(this);

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitContinue(this);
    }

    public record HmmFunctionDeclaration : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public required FunctionSignature Signature { get; init; }

        public required string Name { get; init; }

        public required IReadOnlyList<IHmmSyntax> Body { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitFunctionDeclaration(this);
    }

    public record HmmIfExpression : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Condition { get; init; }

        public required IReadOnlyList<IHmmSyntax> AffirmativeBody { get; init; }

        public required IReadOnlyList<IHmmSyntax> NegativeBody { get; init; }

        public required string Affirmative { get; init; }

        public required string Negative { get; init; }

        public required string Result { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitIfExpression(this);
    }

    public record HmmInvokeSyntax : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required string Target { get; init; }

        public IReadOnlyList<string> Arguments { get; init; } = [];

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitInvoke(this);
    }

    public record HmmIsSyntax : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required string Operand { get; init; }

        public required string Field { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitIs(this);
    }

    public record HmmLoopSyntax : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public required IReadOnlyList<IHmmSyntax> Body { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitLoop(this);
    }

    public record HmmMemberAccess : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required string Operand { get; init; }

        public required string Member { get; init; }

        public required bool IsLValue { get; init; } = false;

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitMemberAccess(this);
    }

    public record HmmNewSyntax : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required IHelixType Type { get; init; }

        public IReadOnlyList<HmmNewFieldAssignment> Assignments { get; init; } = [];

        TokenLocation IHmmSyntax.Location => throw new NotImplementedException();

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitNew(this);
    }

    public record HmmNewFieldAssignment {
        public required string Value { get; init; }

        public Option<string> Field { get; init; } = Option.None;
    }

    public record HmmReturnSyntax : IHmmSyntax, IHirSyntax {
        public required TokenLocation Location { get; init; }

        public required string Operand { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitReturn(this);

        public T Accept<T>(IHirVisitor<T> visitor) => visitor.VisitReturn(this);
    }

    public record HmmUnarySyntax : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required UnaryOperatorKind Operator { get; init; }

        public required string Operand { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitUnaryOperator(this);
    }

    public record HmmVariableStatement : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public required string Variable { get; init; }

        public required string Value { get; init; }

        public required bool IsMutable { get; init; }

        public T Accept<T>(IHmmVisitor<T> visitor) => visitor.VisitVariableStatement(this);
    }
}