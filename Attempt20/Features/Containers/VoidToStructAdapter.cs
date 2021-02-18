using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt20.CodeGeneration;
using Attempt20.Features.Primitives;

namespace Attempt20.Features.Containers {
    public class VoidToStructAdapter : ITypeCheckedSyntax {
        public ITypeCheckedSyntax Target { get; }

        public LanguageType ReturnType { get; }

        public TokenLocation Location => this.Target.Location;

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.Target.Lifetimes;

        public VoidToStructAdapter(ITypeCheckedSyntax target, StructSignature sig, LanguageType retType, ITypeRecorder types) {
            var voidLiteral = new VoidLiteralSyntax() { Location = target.Location };

            var args = sig.Members
                .Select(x => new StructArgument<ITypeCheckedSyntax>() {
                    MemberName = x.MemberName,
                    MemberValue = types.TryUnifyTo(voidLiteral, x.MemberType).GetValue()
                })
                .ToArray();

            this.Target = new NewStructTypeCheckedSyntax() {
                ReturnType = retType,
                Arguments = args,
                Lifetimes = target.Lifetimes,
                Location = target.Location,
                TargetPath = retType.AsNamedType().GetValue()
            };

            this.ReturnType = retType;
        }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            return this.Target.GenerateCode(declWriter, statWriter);
        }
    }
}
