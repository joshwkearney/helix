using Trophy.Analysis;
using Trophy.CodeGeneration.CSyntax;
using System.Collections.Generic;
using System.Linq;

namespace Trophy.Features.Containers {
    public class UnionDeclarationC : IDeclarationC {
        private static int counter = 0;

        public AggregateSignature sig;
        public IdentifierPath unionPath;
        public IReadOnlyList<IDeclarationC> decls;

        public UnionDeclarationC(AggregateSignature sig, IdentifierPath unionPath, IReadOnlyList<IDeclarationC> decls) {
            this.sig = sig;
            this.unionPath = unionPath;
            this.decls = decls;
        }

        public void GenerateCode(ICWriter writer) {
            var unionName = "UnionType_" + counter++;
            var structName = "$" + this.unionPath;
            var mems = this.sig.Members
                    .Select(x => new CParameter(writer.ConvertType(x.MemberType), x.MemberName))
                    .ToArray();

            // Write the union declarations
            writer.WriteForwardDeclaration(CDeclaration.UnionPrototype(unionName));
            writer.WriteForwardDeclaration(CDeclaration.Union(unionName, mems));
            writer.WriteForwardDeclaration(CDeclaration.EmptyLine());

            // Write the struct declarations
            writer.WriteForwardDeclaration(CDeclaration.StructPrototype(structName));
            writer.WriteForwardDeclaration(CDeclaration.Struct(structName, new[] { 
                new CParameter(CType.Integer, "tag"),
                new CParameter(CType.NamedType(unionName), "data")
            }));
            writer.WriteForwardDeclaration(CDeclaration.EmptyLine());
        }
    }
}
