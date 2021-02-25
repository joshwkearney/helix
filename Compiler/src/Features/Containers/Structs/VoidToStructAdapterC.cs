﻿using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Features.Containers.Structs {
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