using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Features.FlowControl;
using Attempt20.Features.Primitives;
using Attempt20.Parsing;

namespace Attempt20.Features.Containers.Structs {
    public class VoidToStructAdapterC : ISyntaxC {
        private readonly ISyntaxC target;

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.target.Lifetimes;

        public VoidToStructAdapterC(ISyntaxC target, AggregateSignature sig, TrophyType returnType, ITypeRecorder types) {
            var voidLiteral = new VoidLiteralC();

            var args = sig.Members
                .Select(x => new StructArgument<ISyntaxC>() {
                    MemberName = x.MemberName,
                    MemberValue = types.TryUnifyTo(voidLiteral, x.MemberType).GetValue()
                })
                .ToArray();

            var block = new BlockSyntaxC(new[] {
                target,
                new NewStructSyntaxC(args, returnType)
            });

            this.target = block;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return this.target.GenerateCode(declWriter, statWriter);
        }
    }
}
