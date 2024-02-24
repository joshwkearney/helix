using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Syntax {
    public interface IParseTree {
        public TokenLocation Location { get; }

        public Option<HelixType> AsType(TypeFrame types) => Option.None;

        public ImperativeExpression ToImperativeSyntax(ImperativeSyntaxWriter writer) => throw new NotImplementedException();
    }
}
