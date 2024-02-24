using Helix.Analysis.TypeChecking;
using Helix.Parsing;

namespace Helix.Syntax {
    public interface IImperativeStatement {
        public TokenLocation Location { get; }

        public void CheckTypes(TypeFrame types, ImperativeSyntaxWriter writer);

        public string[] Write();
    }

    public interface IVariableStatement : IImperativeStatement {
        public string VariableName { get; }

        public ImperativeExpression Value { get; }
    }
}
