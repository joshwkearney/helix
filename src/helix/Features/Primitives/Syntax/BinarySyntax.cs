using Helix.Analysis.Predicates;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;
namespace Helix.Features.Primitives;

public record BinarySyntax : ISyntax {
    public required TokenLocation Location { get; init; }

    public ISyntax Left { get; init; }
        
    public ISyntax Right { get; init; }
        
    public BinaryOperationKind Operator { get; init; }

    public required HelixType ReturnType { get; init; }
        
    public ISyntaxPredicate Predicate => this.Left.Predicate.And(this.Right.Predicate);

    public ISyntax ToRValue(TypeFrame types) => this;

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        return new CBinaryExpression() {
            Left = this.Left.GenerateCode(types, writer),
            Right = this.Right.GenerateCode(types, writer),
            Operation = this.Operator
        };
    }
}