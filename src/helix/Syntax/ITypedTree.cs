using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax;

public interface ITypedTree {
    public TokenLocation Location { get; }

    public HelixType ReturnType { get; }
    
    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer);

    public Immediate GenerateIR(IRWriter writer, IRFrame context) {
        throw new NotImplementedException();
    }

    /// <summary>
    /// An LValue is a special type of syntax tree that is used to represent
    /// a location where values can be stored. LValues return and generate 
    /// pointer types.  This is done so as to not rely on C's lvalue semantics.
    /// </summary>
    public ILValue ToLValue(TypeFrame types) {
        throw TypeException.LValueRequired(this.Location);
    }
}