using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Features.Primitives;
using Attempt20.Parsing;
using Attempt20.src.Features.Containers.Structs;

namespace Attempt20.Features.Containers {
    public class VoidToStructAdapter : ISyntax {
        public ISyntax Target { get; }

        public TrophyType ReturnType { get; }

        public TokenLocation Location => this.Target.Location;

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.Target.Lifetimes;

        public VoidToStructAdapter(ISyntax target, StructSignature sig, TrophyType retType, ITypeRecorder types) {
            var voidLiteral = new VoidLiteralSyntax() { Location = target.Location };

            var args = sig.Members
                .Select(x => new StructArgument<ISyntax>() {
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

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return this.Target.GenerateCode(declWriter, statWriter);
        }
    }
}
