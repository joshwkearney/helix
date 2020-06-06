using Attempt19.CodeGeneration;
using Attempt19.TypeChecking;

namespace Attempt19 {
    public delegate Syntax NameDeclarator(IParsedData data, IdentifierPath scope, NameCache names);

    public delegate Syntax NameResolver(IParsedData data, NameCache names);

    public delegate Syntax TypeDeclarator(IParsedData data, TypeCache types);

    public delegate Syntax TypeResolver(IParsedData data, TypeCache types, ITypeUnifier unifier);

    public delegate CBlock CodeGenerator(ITypeCheckedData data, ICodeGenerator gen);

    public class SyntaxOp {
        public static SyntaxOp FromNameDeclarator(NameDeclarator decl) {
            return new SyntaxOp(decl);
        }

        public static SyntaxOp FromNameResolver(NameResolver resolver) {
            return new SyntaxOp(resolver);
        }

        public static SyntaxOp FromTypeDeclarator(TypeDeclarator decl) {
            return new SyntaxOp(decl);
        }

        public static SyntaxOp FromTypeResolver(TypeResolver resolver) {
            return new SyntaxOp(resolver);
        }

        public static SyntaxOp FromCodeGenerator(CodeGenerator gen) {
            return new SyntaxOp(gen);
        }

        private readonly object op;

        private SyntaxOp(object op) {
            this.op = op;
        }

        public IOption<NameDeclarator> AsNameDeclatator() {
            return Option.Some(this.op as NameDeclarator).Where(x => x != null);
        }

        public IOption<NameResolver> AsNameResolver() {
            return Option.Some(this.op as NameResolver).Where(x => x != null);
        }

        public IOption<TypeDeclarator> AsTypeDeclarator() {
            return Option.Some(this.op as TypeDeclarator).Where(x => x != null);
        }

        public IOption<TypeResolver> AsTypeResolver() {
            return Option.Some(this.op as TypeResolver).Where(x => x != null);
        }

        public IOption<CodeGenerator> AsCodeGenerator() {
            return Option.Some(this.op as CodeGenerator).Where(x => x != null);
        }
    }
}