using Attempt20.Analysis;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace Attempt20.Features.Containers {
    public class UnionTypeCheckedDeclaration : IDeclaration {
        private static int counter = 0;

        public TokenLocation Location { get; set; }

        public StructSignature Signature { get; set; }

        public IdentifierPath StructPath { get; set; }

        public IReadOnlyList<IDeclaration> Declarations { get; set; }

        public void GenerateCode(ICWriter writer) {
            var unionName = $"$UnionType_" + counter++;
            var structName = this.StructPath.ToString();
            var mems = this.Signature.Members
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
