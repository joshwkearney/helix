
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Arrays;

public record ArrayLiteralParseSyntax : IParseSyntax {
    public required TokenLocation Location { get; init; }
        
    public required IReadOnlyList<IParseSyntax> Arguments { get; init; }
        
    public bool IsPure => this.Arguments.All(x => x.IsPure);

    public TypeCheckResult CheckTypes(TypeFrame types) {
        if (this.Arguments.Count == 0) {
            return new TypeCheckResult(new VoidLiteral { Location = this.Location }, types);
        }

        var args = new ISyntax[this.Arguments.Count];

        for (int i = 0; i < this.Arguments.Count; i++) {
            (args[i], types) = this.Arguments[i].CheckTypes(types);
        }

        var totalType = args[0].ReturnType;

        for (int i = 1; i < args.Length; i++) {
            var argType = args[i].ReturnType;

            if (argType.CanUnifyTo(totalType, types)) {
                continue;
            }

            if (!argType.CanUnifyFrom(totalType, types, out totalType)) {
                throw TypeException.UnexpectedType(args[i].Location, totalType, argType);
            }
        }

        args = args
            .Select(x => x.UnifyTo(totalType, types))
            .ToArray();

        var result = new ArrayLiteralSyntax {
            Location = this.Location,
            Arguments = args,
            ArraySignature = new ArrayType(totalType)
        };

        return new TypeCheckResult(result, types);
    }
}