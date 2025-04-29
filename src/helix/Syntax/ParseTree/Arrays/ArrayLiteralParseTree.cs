using Helix.Parsing;
using Helix.Syntax.TypedTree.Arrays;
using Helix.Syntax.TypedTree.Primitives;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Arrays;

public record ArrayLiteralParseTree : IParseTree {
    public required TokenLocation Location { get; init; }
        
    public required IReadOnlyList<IParseTree> Arguments { get; init; }
    
    public TypeCheckResult<ITypedTree> CheckTypes(TypeFrame types) {
        if (this.Arguments.Count == 0) {
            return new TypeCheckResult<ITypedTree>(new VoidLiteral { Location = this.Location }, types);
        }

        var args = new ITypedTree[this.Arguments.Count];

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

        var result = new ArrayLiteralTypedTree {
            Location = this.Location,
            Arguments = args,
            ArraySignature = new ArrayType(totalType)
        };

        return new TypeCheckResult<ITypedTree>(result, types);
    }
}