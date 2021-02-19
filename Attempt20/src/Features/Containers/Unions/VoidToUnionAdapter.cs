using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Features.Primitives;
using Attempt20.Parsing;
using Attempt20.src.Features.Containers.Structs;
using Attempt20.src.Features.Containers.Unions;

namespace Attempt20.Features.Containers.Unions {
    public class VoidToUnionAdapter : ISyntax {
        public ISyntax Target { get; }

        public TrophyType ReturnType { get; }

        public TokenLocation Location => this.Target.Location;

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.Target.Lifetimes;

        public VoidToUnionAdapter(ISyntax target, StructSignature sig, TrophyType retType, ITypeRecorder types) {
            var voidLiteral = new VoidLiteralSyntax() { Location = target.Location };

            var mem = sig.Members.First();
            var arg = new StructArgument<ISyntax>() {
                MemberName = mem.MemberName,
                MemberValue = types.TryUnifyTo(voidLiteral, mem.MemberType).GetValue()
            };

            this.Target = new NewUnionTypeCheckedSyntax() {
                ReturnType = retType,
                Lifetimes = target.Lifetimes,
                Location = target.Location,
                TargetPath = retType.AsNamedType().GetValue(),
                Argument = arg,
                UnionTag = 0
            };

            this.ReturnType = retType;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return this.Target.GenerateCode(declWriter, statWriter);
        }
    }
}
