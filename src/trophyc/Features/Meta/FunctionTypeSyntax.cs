using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Parsing;

namespace Trophy.Features.Meta {
    public class FunctionTypeSyntaxA : ISyntaxA {
        private readonly ISyntaxA returnType;
        private readonly IReadOnlyList<ISyntaxA> argTypes;

        public TokenLocation Location { get; }

        public FunctionTypeSyntaxA(TokenLocation loc, ISyntaxA returnType, IReadOnlyList<ISyntaxA> argTypes) {
            this.Location = loc;
            this.returnType = returnType;
            this.argTypes = argTypes;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var retType = this.returnType.CheckNames(names);
            var args = this.argTypes.Select(x => x.CheckNames(names)).ToArray();

            return new FunctionTypeSyntaxB(this.Location, retType, args);
        }

        public IOption<ITrophyType> ResolveToType(INamesRecorder names) {
            var retType = this.returnType.ResolveToType(names);
            var args = this.argTypes.Select(x => x.ResolveToType(names));

            if (!retType.Any() || !args.All(x => x.Any())) {
                return Option.None<ITrophyType>();
            }

            return Option.Some(
                new FunctionType(
                    retType.GetValue(), 
                    args.Select(x => x.GetValue()).ToArray()));
        }
    }

    public class FunctionTypeSyntaxB : ISyntaxB {
        private readonly ISyntaxB returnType;
        private readonly IReadOnlyList<ISyntaxB> argTypes;

        public TokenLocation Location { get; }

        public FunctionTypeSyntaxB(TokenLocation loc, ISyntaxB returnType, IReadOnlyList<ISyntaxB> argTypes) {
            this.Location = loc;
            this.returnType = returnType;
            this.argTypes = argTypes;
        }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.argTypes
                .SelectMany(x => x.VariableUsage)
                .Concat(this.returnType.VariableUsage)
                .ToImmutableHashSet();
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var retSyntax = this.returnType.CheckTypes(types);
            var argsSyntax = this.argTypes.Select(x => x.CheckTypes(types)).ToArray();

            if (!retSyntax.ReturnType.AsMetaType().TryGetValue(out var retType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.returnType.Location);
            }

            for (int i = 0; i < argsSyntax.Length; i++) {
                var syntax = argsSyntax[i];

                if (!syntax.ReturnType.AsMetaType().Any()) {
                    throw TypeCheckingErrors.ExpectedTypeExpression(this.argTypes[i].Location);
                }
            }

            var argTypes = argsSyntax.Select(x => x.ReturnType.AsMetaType().GetValue()).ToArray();
            var funcType = new FunctionType(retType, argTypes);

            return new TypeSyntaxC(funcType);
        }
    }
}