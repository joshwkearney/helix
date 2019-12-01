using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Generation {
    public interface IExpressionCodeGenerator {
        CCode Generate(ISyntax syntax);
    }

    public abstract class ExpressionCodeGenerator<TSyntax> : IExpressionCodeGenerator where TSyntax : ISyntax {
        public ISyntaxVisitor<IExpressionCodeGenerator> CodeGenerator { get; }

        public TypeGenerator TypeGenerator { get; }

        public ExpressionCodeGenerator(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) {
            this.CodeGenerator = cg;
            this.TypeGenerator = typeGen;
        }

        public abstract CCode Generate(TSyntax syntax);

        public CCode Generate(ISyntax syntax) => this.Generate((TSyntax)syntax);
    }
}
