using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Features.FlowControl;
using Attempt20.Features.Primitives;
using Attempt20.Parsing;

namespace Attempt20.Features.Containers.Unions {
    public class VoidToUnionAdapterC : ISyntaxC {
        private readonly ISyntaxC target;

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.target.Lifetimes;

        public VoidToUnionAdapterC(ISyntaxC target, AggregateSignature sig, TrophyType returnType, ITypeRecorder types) {
            var voidLiteral = new VoidLiteralC();
            var mem = sig.Members.First();

            var arg = new StructArgument<ISyntaxC>() {
                MemberName = mem.MemberName,
                MemberValue = types.TryUnifyTo(voidLiteral, mem.MemberType).GetValue()
            };

            // TODO - MAGIC ZERO
            var block = new BlockSyntaxC(new[] {
                target,
                new NewUnionSyntaxC(arg, 0, returnType)
            });

            this.target = block;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return this.target.GenerateCode(declWriter, statWriter);
        }
    }
}
