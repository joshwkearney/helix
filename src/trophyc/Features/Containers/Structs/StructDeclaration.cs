using Trophy.Analysis;
using Trophy.Generation.CSyntax;
using System.Collections.Generic;
using System.Linq;

namespace Trophy.Features.Containers {
    public class StructDeclarationC : IDeclarationC {
        private bool generated = false;
        private readonly AggregateSignature sig;
        private readonly IdentifierPath structPath;
        private readonly IReadOnlyList<IDeclarationC> decls;

        public StructDeclarationC(AggregateSignature sig, IdentifierPath structPath, IReadOnlyList<IDeclarationC> decls) {
            this.sig = sig;
            this.structPath = structPath;
            this.decls = decls;
        }

        public void GenerateCode(ICWriter declWriter) {
            if (this.generated) {
                return;
            }

            // Write forward declaration
            declWriter.WriteDeclaration1(CDeclaration.StructPrototype("$" + this.structPath));
            declWriter.WriteDeclaration1(CDeclaration.EmptyLine());

            // Write full struct
            declWriter.WriteDeclaration2(CDeclaration.Struct(
                "$" + this.structPath,
                this.sig.Members
                    .Select(x => new CParameter(declWriter.ConvertType(x.MemberType), x.MemberName))
                    .ToArray()));

            declWriter.WriteDeclaration2(CDeclaration.EmptyLine());

            // Write nested declarations
            foreach (var decl in this.decls) {
                decl.GenerateCode(declWriter);
            }

            this.generated = true;
        }
    }
}
