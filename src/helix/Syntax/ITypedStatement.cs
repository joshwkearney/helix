using Helix.CodeGeneration;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.TypeChecking;

namespace Helix.Syntax;

public interface ITypedStatement {
    public TokenLocation Location { get; }
    
    public bool AlwaysJumps { get; }

    public void GenerateCode(TypeFrame types, ICStatementWriter writer);

    public void GenerateIR(IRWriter writer, IRFrame context) {
        throw new NotImplementedException();
    }
}