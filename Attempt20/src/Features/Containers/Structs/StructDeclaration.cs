using Attempt20.Analysis;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace Attempt20.Features.Containers {
    public class StructTypeCheckedDeclaration : IDeclaration {
        public TokenLocation Location { get; set; }

        public StructSignature Signature { get; set; }

        public IdentifierPath StructPath { get; set; }

        public IReadOnlyList<IDeclaration> Declarations { get; set; }

        public void GenerateCode(ICWriter declWriter) {
            // Write forward declaration
            declWriter.WriteForwardDeclaration(CDeclaration.StructPrototype(this.StructPath.ToString()));

            // Write full struct
            declWriter.WriteForwardDeclaration(CDeclaration.Struct(
                this.StructPath.ToString(),
                this.Signature.Members
                    .Select(x => new CParameter(declWriter.ConvertType(x.MemberType), x.MemberName))
                    .ToArray()));

            declWriter.WriteForwardDeclaration(CDeclaration.EmptyLine());

            // Write nested declarations
            foreach (var decl in this.Declarations) {
                decl.GenerateCode(declWriter);
            }
        }
    }
}
