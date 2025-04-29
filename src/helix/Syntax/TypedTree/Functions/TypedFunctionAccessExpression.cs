using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Functions;

public record TypedFunctionAccessExpression : ITypedExpression {
    public required TokenLocation Location { get; init; }

    public required IdentifierPath FunctionPath { get; init; }
        
    public bool AlwaysJumps => false;

    public HelixType ReturnType => new NominalType(this.FunctionPath, NominalTypeKind.Function);

    public bool IsPure => true;

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        return new CVariableLiteral(writer.GetVariableName(this.FunctionPath));
    }
}