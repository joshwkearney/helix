using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Syntax {
    public interface ISyntaxTree {
        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children { get; }

        public bool IsPure { get; }

        public Option<HelixType> AsType(ITypedFrame types) => Option.None;

        public ISyntaxTree CheckTypes(TypeFrame types);

        // Mixins
        public void AnalyzeFlow(FlowFrame flow) {
            throw new Exception("Compiler bug");
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            throw new Exception("Compiler bug");
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
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
        public ISyntaxTree ToLValue(TypeFrame types) {
            throw TypeException.LValueRequired(this.Location);
        }
    }
}
