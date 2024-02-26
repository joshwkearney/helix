using helix.common;
using Helix;
using Helix.Analysis.Types;
using Helix.Parsing;

namespace helix_frontend.ParseTree {
    public interface IParseTree {
        public TokenLocation Location { get; }

        public T Accept<T>(IParseTreeVisitor<T> visitor);
    }

    public record LoopSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Body { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitLoop(this);
    }

    public record AssignmentStatement : IParseTree {
        public required IParseTree Target { get; init; }

        public required IParseTree Assign { get; init; }

        public required TokenLocation Location { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitAssignment(this);
    }

    public record VariableAccess : IParseTree {
        public required TokenLocation Location { get; init; }

        public required string VariableName { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitVariableAccess(this);
    }

    public record VariableStatement : IParseTree {
        public required TokenLocation Location { get; init; }

        public required string VariableName { get; init; }

        public required IParseTree Value { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitVariableStatement(this);
    }

    public record VariableNameTypePair {
        public required string Name { get; init; }

        public Option<IHelixType> Type { get; init; } = Option.None;
    }

    public record FunctionDeclaration : IParseTree {
        public required TokenLocation Location { get; init; }

        public required FunctionType Signature { get; init; }

        public required string Name { get; init; }

        public required IParseTree Body { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitFunctionDeclaration(this);
    }

    public record StructDeclaration : IParseTree {
        public required TokenLocation Location { get; init; }

        public required StructType Signature { get; init; }

        public required string Name { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitStructDeclaration(this);

    }

    public record UnionDeclaration : IParseTree {
        public required TokenLocation Location { get; init; }

        public required UnionType Signature { get; init; }

        public required string Name { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitUnionDeclaration(this);
    }

    public record BinarySyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Left { get; init; }

        public required IParseTree Right { get; init; }

        public required BinaryOperationKind Operator { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitBinarySyntax(this);
    }

    public record BoolLiteral : IParseTree {
        public required TokenLocation Location { get; init; }

        public required bool Value { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitBoolLiteral(this);
    }

    public record IfSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Condition { get; init; }

        public required IParseTree Affirmative { get; init; }

        public Option<IParseTree> Negative { get; init; } = Option.None;

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitIf(this);
    }

    public record UnarySyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Operand { get; init; }

        public required UnaryOperatorKind Operator { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitUnarySyntax(this);
    }

    public record AsSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Operand { get; init; }

        public required IHelixType Type { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitAs(this);
    }

    public record IsSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Operand { get; init; }

        public required string Field { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitIs(this);
    }

    public record InvokeSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Target { get; init; }

        public required IReadOnlyList<IParseTree> Args { get; init; }
        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitInvoke(this);
    }

    public record MemberAccessSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Target { get; init; }

        public required string Field { get; init; }
        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitMemberAccess(this);
    }

    public record WordLiteral : IParseTree {
        public required TokenLocation Location { get; init; }

        public required long Value { get; init; }
        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitWordLiteral(this);
    }

    public record VoidLiteral : IParseTree {
        public required TokenLocation Location { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitVoidLiteral(this);
    }

    public record BlockSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IReadOnlyList<IParseTree> Statements { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitBlock(this);
    }

    public record NewSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IHelixType Type { get; init; }

        public IReadOnlyList<NewFieldAssignment> Assignments { get; init; } = [];

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitNew(this);
    }

    public record NewFieldAssignment {
        public required IParseTree Value { get; init; }

        public required Option<string> Name { get; init; }
    }

    public record ArrayLiteral : IParseTree {
        public required TokenLocation Location { get; init; }

        public IReadOnlyCollection<IParseTree> Args { get; init; } = [];

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitArrayLiteral(this);
    }

    public record BreakSyntax : IParseTree {
        public required TokenLocation Location { get; init; }
        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitBreak(this);
    }

    public record ContinueSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitContinue(this);
    }

    public record ReturnSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Payload { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitReturn(this);
    }

    public record WhileSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Condition { get; init; }

        public required IParseTree Body { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitWhile(this);
    }

    public record ForSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required string Variable { get; init; }

        public required IParseTree InitialValue { get; init; }

        public required IParseTree FinalValue { get; init; }

        public required bool Inclusive { get; init; }

        public required IParseTree Body { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitFor(this);
    }
}
