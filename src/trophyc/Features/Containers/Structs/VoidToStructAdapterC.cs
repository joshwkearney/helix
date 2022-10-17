using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;

namespace Trophy.Features.Containers.Structs {
    public class VoidToStructAdapterC : ISyntaxC {
        private readonly ISyntaxC target;

        public ITrophyType ReturnType { get; }

        public VoidToStructAdapterC(ISyntaxC target, AggregateSignature sig, ITrophyType returnType, ITypesRecorder types) {
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
