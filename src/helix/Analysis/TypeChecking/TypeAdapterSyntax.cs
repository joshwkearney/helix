using Helix.Analysis.Predicates;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Analysis.TypeChecking;

public class TypeAdapterSyntax : ISyntax {
    public required ISyntax Operand { get; init; }
    
    public required HelixType ReturnType { get; init; }
    
    public TokenLocation Location => this.Operand.Location;

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        return this.Operand.GenerateCode(types, writer);
    }
}