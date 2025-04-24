using Helix.Analysis.Predicates;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Syntax;

public interface ISyntax {
    public TokenLocation Location { get; }

    public HelixType ReturnType { get; }
        
    public ISyntaxPredicate Predicate { get; }
        
    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        throw new Exception("Compiler bug");
    }

    public ISyntax ToRValue(TypeFrame types) {
        throw TypeException.RValueRequired(this.Location);
    }

    /// <summary>
    /// An LValue is a special type of syntax tree that is used to represent
    /// a location where values can be stored. LValues return and generate 
    /// pointer types but have lifetimes that match the inner type of the
    /// pointer. This is done so as to not rely on C's lvalue semantics. The
    /// lifetimes of an lvalue represent the region where the memory storage
    /// has been allocated, which any assigned values must outlive
    /// </summary>
    public ISyntax ToLValue(TypeFrame types) {
        throw TypeException.LValueRequired(this.Location);
    }
}