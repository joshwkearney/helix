using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Functions;

public record ReturnSyntax : ISyntax {
    public required TokenLocation Location { get; init; }

    public required ISyntax Operand { get; init; }

    public bool AlwaysJumps => true;
        
    public HelixType ReturnType => PrimitiveType.Void;

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        writer.WriteStatement(new CReturn() {
            Target = this.Operand.GenerateCode(types, writer)
        });

        return new CIntLiteral(0);
    }
}