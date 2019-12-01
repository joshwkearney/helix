using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Generation {
    class X {
        public Y y;
    }

    class Y {
        public X x;
    }

    public class DeclarationCGProvider : IDeclarationVisitor<IDeclarationCodeGenerator> {
        private readonly FunctionDeclarationCGBehavior funcDeclBehavior;
        private readonly StructDeclarationCGBehavior structDeclBehavior;

        public DeclarationCGProvider(ISyntaxVisitor<IExpressionCodeGenerator> codegen, TypeGenerator typegen) {
            this.funcDeclBehavior = new FunctionDeclarationCGBehavior(codegen, typegen);
            this.structDeclBehavior = new StructDeclarationCGBehavior(codegen, typegen);

            X x = new X();
            x.y = new Y();
            x.y.x = x;
        }

        public IDeclarationCodeGenerator VisitFunctionDeclaration(FunctionDeclaration decl) => this.funcDeclBehavior;

        public IDeclarationCodeGenerator VisitStructDeclaration(StructDeclaration decl) => this.structDeclBehavior;
    }
}