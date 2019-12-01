//using Attempt15.Types;
//using System;

//namespace Attempt15.Analysis {
//    public class TypeSyntaxInterpreter : ITypeSyntaxVisitor<ILanguageType> {
//        private readonly Scope scope;

//        public TypeSyntaxInterpreter(Scope scope) {
//            this.scope = scope;
//        }

//        public ILanguageType VisitRefTypeSyntax(RefTypeSyntax syntax) {
//            var inner = syntax.Target.Accept(this);

//            if (inner is ReferenceType || inner is VariableType) {
//                throw new Exception();
//            }

//            return new ReferenceType(inner);
//        }

//        public ILanguageType VisitVarTypeSyntax(VarTypeSyntax syntax) {
//            return new VariableType(syntax.Target.Accept(this));
//        }

//        public ILanguageType VisitTypeVariableLiteral(TypeVariableLiteral syntax) {
//            if (syntax.Value == "int") {
//                return new IntType();
//            }
//            else if (syntax.Value == "void") {
//                return new VoidType();
//            }

//            if (this.scope.TypeVariables.TryGetValue(syntax.Value, out var type)) {
//                return type;
//            }

//            throw new Exception();
//        }

//        public ILanguageType VisitTypeLiteralSyntax(TypeLiteralSyntax syntax) {
//            return syntax.Type;
//        }
//    }
//}