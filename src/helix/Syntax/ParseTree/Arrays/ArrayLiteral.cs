using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Arrays;
using Helix.Syntax.TypedTree.Primitives;
using Helix.TypeChecking;

namespace Helix.Syntax.ParseTree.Arrays;

public record ArrayLiteral : IParseExpression {
    public required TokenLocation Location { get; init; }
        
    public required IReadOnlyList<IParseExpression> Arguments { get; init; }
    
    public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) {
        if (this.Arguments.Count == 0) {
            return new TypeCheckResult<ITypedExpression>(new VoidLiteral { Location = this.Location }, types);
        }

        var args = new ITypedExpression[this.Arguments.Count];

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

        var result = new TypedArrayLiteral {
            Location = this.Location,
            Arguments = args,
            ArraySignature = new Types.ArrayType(totalType)
        };

        return new TypeCheckResult<ITypedExpression>(result, types);
    }
}