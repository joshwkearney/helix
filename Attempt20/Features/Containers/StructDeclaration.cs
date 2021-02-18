using System;
using System.Linq;
using Attempt20.CodeGeneration;

namespace Attempt20.Features.Containers {
    public class StructParsedDeclaration : IParsedDeclaration {
        public TokenLocation Location { get; set; }

        public StructSignature Signature { get; set; }

        public void DeclareNames(INameRecorder names) {
            // Make sure this name isn't taken
            if (names.TryFindName(this.Signature.Name, out _, out _)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.Signature.Name);
            }

            // Declare this struct
            names.DeclareGlobalName(names.CurrentScope.Append(this.Signature.Name), NameTarget.Struct);

            var parNames = this.Signature.Members.Select(x => x.MemberName).ToArray();
            var unique = parNames.Distinct().ToArray();

            // Check for duplicate member names
            if (parNames.Length != unique.Length) {
                var dup = parNames.Except(unique).First();

                throw TypeCheckingErrors.IdentifierDefined(this.Location, dup);
            }
        }

        public void DeclareTypes(INameRecorder names, ITypeRecorder types) {
            types.DeclareStruct(names.CurrentScope.Append(this.Signature.Name), this.Signature);
        }

        public void ResolveNames(INameRecorder names) {
            // Resolve members
            var mems = this.Signature
                .Members
                .Select(x => new StructMember(x.MemberName, names.ResolveTypeNames(x.MemberType, this.Location)))
                .ToArray();

            this.Signature = new StructSignature(this.Signature.Name, mems);
        }

        public ITypeCheckedDeclaration ResolveTypes(INameRecorder names, ITypeRecorder types) {
            return new StructTypeCheckedDeclaration() {
                Location = this.Location,
                Signature = this.Signature,
                StructPath = names.CurrentScope.Append(this.Signature.Name)
            };
        }
    }

    public class StructTypeCheckedDeclaration : ITypeCheckedDeclaration {
        public TokenLocation Location { get; set; }

        public StructSignature Signature { get; set; }

        public IdentifierPath StructPath { get; set; }

        public void GenerateCode(ICDeclarationWriter declWriter) {
            // Write forward declaration
            declWriter.WriteForwardDeclaration(CDeclaration.StructPrototype(this.StructPath.ToString()));

            // Write full struct
            declWriter.WriteForwardDeclaration(CDeclaration.Struct(
                this.StructPath.ToString(),
                this.Signature.Members
                    .Select(x => new CParameter(declWriter.ConvertType(x.MemberType), x.MemberName))
                    .ToArray()));

            declWriter.WriteForwardDeclaration(CDeclaration.EmptyLine());
        }
    }
}
