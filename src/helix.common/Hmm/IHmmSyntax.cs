using helix.common;
using helix.common.Hmm;
using Helix.Analysis.Types;
using Helix.Parsing;

namespace Helix.HelixMinusMinus {
    public interface IHmmSyntax {
        public TokenLocation Location { get; }

        public void Accept(IHmmVisitor visitor);

        public string ToString() {
            var visitor = new HmmSyntaxToStringVisitor();

            this.Accept(visitor);
            return visitor.Result1;
        }
    }

    public interface IHmmExpression : IHmmSyntax {
        public string Result { get; }
    }

    public record HmmTypeDeclaration : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public required string Name { get; init; }

        public required TypeDeclarationKind Kind { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitTypeDeclaration(this);
    }

    public enum TypeDeclarationKind {
        Struct, Union, Function
    }

    public record HmmFunctionForwardDeclaration : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public required string Name { get; init; }

        public required FunctionType Signature { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitFunctionForwardDeclaration(this);
    }

    public record HmmStructDeclaration : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public required StructType Signature { get; init; }

        public required string Name { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitStructDeclaration(this);
    }

    public record HmmUnionDeclaration : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public required StructType Signature { get; init; }

        public required string Name { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitUnionDeclaration(this);
    }

    public record HmmArrayLiteral : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required IReadOnlyList<string> Args { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitArrayLiteral(this);
    }

    public record HmmAssignment : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public required string Variable { get; init; }

        public required string Value { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitAssignment(this);
    }

    public record HmmAsSyntax : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required string Operand { get; init; }

        public required IHelixType Type { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitAsSyntax(this);
    }

    public record HmmBinaryOperator : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required BinaryOperationKind Operator { get; init; }

        public required string Left { get; init; }

        public required string Right { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitBinaryOperator(this);
    }

    public record HmmBoolLiteral : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required bool Value { get; init; }

        public required string Result { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitBoolLiteral(this);
    }

    public record HmmBreakSyntax : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitBreak(this);
    }

    public record HmmContinueSyntax : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitContinue(this);
    }

    public record HmmFunctionDeclaration : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public required FunctionType Function { get; init; }

        public required string Name { get; init; }

        public required List<IHmmSyntax> Body { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitFunctionDeclaration(this);
    }

    public record HmmIfExpression : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Condition { get; init; }

        public required IReadOnlyList<IHmmSyntax> AffirmativeBody { get; init; }

        public required IReadOnlyList<IHmmSyntax> NegativeBody { get; init; }

        public required string Affirmative { get; init; }

        public required string Negative { get; init; }

        public required string Result { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitIfExpression(this);
    }

    public record HmmInvokeSyntax : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required string Target { get; init; }

        public IReadOnlyList<string> Arguments { get; init; } = [];

        public void Accept(IHmmVisitor visitor) => visitor.VisitInvoke(this);
    }

    public record HmmIsSyntax : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required string Operand { get; init; }

        public required string Field { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitIs(this);
    }

    public record HmmLoopSyntax : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public required IReadOnlyList<IHmmSyntax> Body { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitLoop(this);
    }

    public record HmmMemberAccess : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required string Operand { get; init; }

        public required string FieldName { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitMemberAccess(this);
    }

    public record HmmNewSyntax : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required IHelixType Type { get; init; }

        public IReadOnlyList<HmmNewFieldAssignment> Assignments { get; init; } = [];

        TokenLocation IHmmSyntax.Location => throw new NotImplementedException();

        public void Accept(IHmmVisitor visitor) => visitor.VisitNew(this);
    }

    public record HmmNewFieldAssignment {
        public required string Value { get; init; }

        public Option<string> Field { get; init; } = Option.None;
    }

    public record HmmReturnSyntax : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public required string Operand { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitReturn(this);
    }

    public record HmmUnaryOperator : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required UnaryOperatorKind Operator { get; init; }

        public required string Operand { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitUnaryOperator(this);
    }

    public record HmmVariableAccess : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public required string Value { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitVariableAccess(this);
    }

    public record HmmVariableStatement : IHmmSyntax {
        public required TokenLocation Location { get; init; }

        public required string Variable { get; init; }

        public required string Value { get; init; }

        public required bool IsMutable { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitVariableStatement(this);
    }

    public record HmmVoidLiteral : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required string Result { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitVoidLiteral(this);
    }

    public record HmmWordLiteral : IHmmExpression {
        public required TokenLocation Location { get; init; }

        public required long Value { get; init; }

        public required string Result { get; init; }

        public void Accept(IHmmVisitor visitor) => visitor.VisitWordLiteral(this);
    }
}