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

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer);

    public ISyntax ToRValue(TypeFrame types);

    /// <summary>
    /// An LValue is a special type of syntax tree that is used to represent
    /// a location where values can be stored. LValues return and generate 
    /// pointer types but have lifetimes that match the inner type of the
    /// pointer. This is done so as to not rely on C's lvalue semantics.
    /// </summary>
    public ISyntax ToLValue(TypeFrame types) {
        throw TypeException.LValueRequired(this.Location);
    }
}