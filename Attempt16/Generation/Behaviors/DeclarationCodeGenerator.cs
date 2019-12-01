using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Generation {
    public interface IDeclarationCodeGenerator {
        CCode Generate(IDeclaration declaration);
    }

    public abstract class DeclarationCodeGenerator<TSyntax> : IDeclarationCodeGenerator where TSyntax : IDeclaration {
        public ISyntaxVisitor<IExpressionCodeGenerator> CodeGenerator { get; }

        public TypeGenerator TypeGenerator { get; }

        public DeclarationCodeGenerator(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) {
            this.CodeGenerator = cg;
            this.TypeGenerator = typeGen;
        }

        public abstract CCode Generate(TSyntax declaration);

        public CCode Generate(IDeclaration declaration) => this.Generate((TSyntax)declaration);
    }
}
