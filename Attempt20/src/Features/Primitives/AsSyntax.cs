using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.Parsing;

namespace Attempt20.Features.Primitives {
    public class AsParsedSyntax : IParsedSyntax {
        public IParsedSyntax Argument { get; set; }

        public TrophyType TargetType { get; set; }

        public TokenLocation Location { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.Argument = this.Argument.CheckNames(names);

            // Resolve types
            this.TargetType = names.ResolveTypeNames(this.TargetType, this.Location);

            return this;
        }

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            var arg = this.Argument.CheckTypes(names, types);

            if (types.TryUnifyTo(arg, this.TargetType).TryGetValue(out var newArg)) {
                return newArg;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(arg.Location, this.TargetType, arg.ReturnType);
            }
        }
    }
}
