using Attempt16.Syntax;
using Attempt16.Types;
using System;

namespace Attempt16.Analysis {
    public class TypeUnifier {
        private readonly UnificationProvider provider = new UnificationProvider();

        public ISyntax UnifyTo(ISyntax syntax, ILanguageType type) {
            if (syntax.ReturnType.Equals(type)) {
                return syntax;
            }

            return type.Accept(this.provider).Unify(syntax, type);
        }

        private class UnificationProvider : ITypeVisitor<IUnificationBehavior> {
            public IUnificationBehavior VisitFunctionType(SingularFunctionType type) {
                throw new Exception();
            }

            public IUnificationBehavior VisitIntType(IntType type) {
                throw new Exception();
            }

            public IUnificationBehavior VisitStructType(SingularStructType type) {
                throw new Exception();
            }

            public IUnificationBehavior VisitVariableType(VariableType type) {
                throw new Exception();
            }

            public IUnificationBehavior VisitVoidType(VoidType type) {
                throw new Exception();
            }
        }
    }
}