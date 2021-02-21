using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.Parsing;

namespace Attempt20.Features.Primitives {
    public class AsSyntaxA : ISyntaxA {
        private readonly ISyntaxA arg;
        private readonly TrophyType target;

        public TokenLocation Location { get; }

        public AsSyntaxA(TokenLocation loc, ISyntaxA arg, TrophyType target) {
            this.Location = loc;
            this.arg = arg;
            this.target = target;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            var arg = this.arg.CheckNames(names);
            var target = names.ResolveTypeNames(this.target, this.Location);

            return new AsSyntaxB(this.Location, arg, target);
        }
    }

    public class AsSyntaxB : ISyntaxB {
        private readonly ISyntaxB arg;
        private readonly TrophyType target;

        public TokenLocation Location { get; }

        public AsSyntaxB(TokenLocation loc, ISyntaxB arg, TrophyType target) {
            this.Location = loc;
            this.target = target;
            this.arg = arg;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var arg = this.arg.CheckTypes(types);

            if (types.TryUnifyTo(arg, this.target).TryGetValue(out var newArg)) {
                return newArg;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(this.Location, this.target, arg.ReturnType);
            }
        }
    }
}