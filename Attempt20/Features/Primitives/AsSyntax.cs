using System;
namespace Attempt20.Features.Primitives {
    public class AsParsedSyntax : IParsedSyntax {
        public IParsedSyntax Argument { get; set; }

        public LanguageType TargetType { get; set; }

        public TokenLocation Location { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.Argument = this.Argument.CheckNames(names);

            // Resolve types
            this.TargetType = names.ResolveTypeNames(this.TargetType, this.Location);

            return this;
        }

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
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
