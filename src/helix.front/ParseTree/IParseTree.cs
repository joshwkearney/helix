using Helix.Common;
using Helix.Common.Tokens;
using Helix.Common.Types;

namespace Helix.Frontend.ParseTree {
    internal interface IParseTree {
        public TokenLocation Location { get; }

        public T Accept<T>(IParseTreeVisitor<T> visitor);
    }

    internal record LoopSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Body { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitLoop(this);
    }

    internal record AssignmentStatement : IParseTree {
        public required IParseTree Target { get; init; }

        public required IParseTree Assign { get; init; }

        public required TokenLocation Location { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitAssignment(this);
    }

    internal record VariableAccess : IParseTree {
        public required TokenLocation Location { get; init; }

        public required string VariableName { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitVariableAccess(this);
    }

    internal record VariableStatement : IParseTree {
        public required TokenLocation Location { get; init; }

        public required string VariableName { get; init; }

        public required IParseTree Value { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitVariableStatement(this);
    }

    internal record VariableNameTypePair {
        public required string Name { get; init; }

        public Option<IHelixType> Type { get; init; } = Option.None;
    }

    internal record FunctionDeclaration : IParseTree {
        public required TokenLocation Location { get; init; }

        public required FunctionSignature Signature { get; init; }

        public required string Name { get; init; }

        public required IParseTree Body { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitFunctionDeclaration(this);
    }

    internal record StructDeclaration : IParseTree {
        public required TokenLocation Location { get; init; }

        public required StructSignature Signature { get; init; }

        public required string Name { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitStructDeclaration(this);

    }

    internal record UnionDeclaration : IParseTree {
        public required TokenLocation Location { get; init; }

        public required UnionSignature Signature { get; init; }

        public required string Name { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitUnionDeclaration(this);
    }

    internal record BinarySyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Left { get; init; }

        public required IParseTree Right { get; init; }

        public required BinaryOperationKind Operator { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitBinarySyntax(this);
    }

    internal record BoolLiteral : IParseTree {
        public required TokenLocation Location { get; init; }

        public required bool Value { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitBoolLiteral(this);
    }

    internal record IfSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Condition { get; init; }

        public required IParseTree Affirmative { get; init; }

        public Option<IParseTree> Negative { get; init; } = Option.None;

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitIf(this);
    }

    internal record UnarySyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Operand { get; init; }

        public required UnaryOperatorKind Operator { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitUnarySyntax(this);
    }

    internal record AsSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Operand { get; init; }

        public required IHelixType Type { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitAs(this);
    }

    internal record IsSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Operand { get; init; }

        public required string Field { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitIs(this);
    }

    internal record InvokeSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Target { get; init; }

        public required IReadOnlyList<IParseTree> Args { get; init; }
        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitInvoke(this);
    }

    internal record MemberAccessSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Target { get; init; }

        public required string Field { get; init; }
        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitMemberAccess(this);
    }

    internal record WordLiteral : IParseTree {
        public required TokenLocation Location { get; init; }

        public required long Value { get; init; }
        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitWordLiteral(this);
    }

    internal record VoidLiteral : IParseTree {
        public required TokenLocation Location { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitVoidLiteral(this);
    }

    internal record BlockSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IReadOnlyList<IParseTree> Statements { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitBlock(this);
    }

    internal record NewSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IHelixType Type { get; init; }

        public IReadOnlyList<NewFieldAssignment> Assignments { get; init; } = [];

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitNew(this);
    }

    internal record NewFieldAssignment {
        public required IParseTree Value { get; init; }

        public required Option<string> Name { get; init; }
    }

    internal record ArrayLiteral : IParseTree {
        public required TokenLocation Location { get; init; }

        public IReadOnlyCollection<IParseTree> Args { get; init; } = [];

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitArrayLiteral(this);
    }

    internal record BreakSyntax : IParseTree {
        public required TokenLocation Location { get; init; }
        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitBreak(this);
    }

    internal record ContinueSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitContinue(this);
    }

    internal record ReturnSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Payload { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitReturn(this);
    }

    internal record WhileSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required IParseTree Condition { get; init; }

        public required IParseTree Body { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitWhile(this);
    }

    internal record ForSyntax : IParseTree {
        public required TokenLocation Location { get; init; }

        public required string Variable { get; init; }

        public required IParseTree InitialValue { get; init; }

        public required IParseTree FinalValue { get; init; }

        public required bool Inclusive { get; init; }

        public required IParseTree Body { get; init; }

        public T Accept<T>(IParseTreeVisitor<T> visitor) => visitor.VisitFor(this);
    }
}
