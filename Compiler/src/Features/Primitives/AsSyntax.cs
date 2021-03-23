using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Parsing;

namespace Trophy.Features.Primitives {
    public class AsSyntaxA : ISyntaxA {
        private readonly ISyntaxA arg;
        private readonly ISyntaxA target;

        public TokenLocation Location { get; }

        public AsSyntaxA(TokenLocation loc, ISyntaxA arg, ISyntaxA target) {
            this.Location = loc;
            this.arg = arg;
            this.target = target;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var arg = this.arg.CheckNames(names);
            var target = this.target.CheckNames(names);

            return new AsSyntaxB(this.Location, arg, target);
        }
    }

    public class AsSyntaxB : ISyntaxB {
        private readonly ISyntaxB arg;
        private readonly ISyntaxB target;

        public TokenLocation Location { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.arg.VariableUsage;
        }

        public AsSyntaxB(TokenLocation loc, ISyntaxB arg, ISyntaxB target) {
            this.Location = loc;
            this.target = target;
            this.arg = arg;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var arg = this.arg.CheckTypes(types);
            var target = this.target.CheckTypes(types);

            if (!target.ReturnType.AsMetaType().Select(x => x.PayloadType).TryGetValue(out var targetType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.target.Location);
            }

            if (types.TryUnifyTo(arg, targetType).TryGetValue(out var newArg)) {
                return newArg;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(this.Location, targetType, arg.ReturnType);
            }
        }
    }
}