using Trophy.Analysis;
using Trophy.CodeGeneration.CSyntax;
using System.Collections.Generic;
using System.Linq;

namespace Trophy.Features.Containers {
    public class StructDeclarationC : IDeclarationC {
        private readonly AggregateSignature sig;
        private readonly IdentifierPath structPath;
        private readonly IReadOnlyList<IDeclarationC> decls;

        public StructDeclarationC(AggregateSignature sig, IdentifierPath structPath, IReadOnlyList<IDeclarationC> decls) {
            this.sig = sig;
            this.structPath = structPath;
            this.decls = decls;
        }

        public void GenerateCode(ICWriter declWriter) {
            // Write forward declaration
            declWriter.WriteForwardDeclaration(CDeclaration.StructPrototype("$" + this.structPath));

            // Write full struct
            declWriter.WriteForwardDeclaration(CDeclaration.Struct(
                "$" + this.structPath,
                this.sig.Members
                    .Select(x => new CParameter(declWriter.ConvertType(x.MemberType), x.MemberName))
                    .ToArray()));

            declWriter.WriteForwardDeclaration(CDeclaration.EmptyLine());

            // Write nested declarations
            foreach (var decl in this.decls) {
                decl.GenerateCode(declWriter);
            }
        }
    }
}
