using Helix.Parsing;
using Helix.Syntax.TypedTree.FlowControl;
using Helix.TypeChecking;

namespace Helix.Syntax.ParseTree.FlowControl;

public record LoopParseStatement : IParseStatement {
    public required TokenLocation Location { get; init; }

    public required IParseStatement Body { get; init; }
        
    public bool IsPure => false;

    public TypeCheckResult<ITypedStatement> CheckTypes(TypeFrame types) {
        ITypedStatement body;
        bool alwaysJumps = false;
        
        while (true) {
            // Push a new context for our loop body
            types = types.WithScope("$loop");
            
            try {
                (body, var loopTypes) = this.TypeCheckLoopBody(types);

                // If there aren't any continue frames or break frames, this loop either runs forever or returns
                if (loopTypes.ContinueFrames.IsEmpty && loopTypes.BreakFrames.IsEmpty) {
                    alwaysJumps = true;
                    break;
                }

                // If there aren't any continue frames, we the loop never loops. Since we already type checked it,
                // we're done
                if (loopTypes.ContinueFrames.IsEmpty) {
                    break;
                }

                // If we have continuation frames, we need to combine them into one
                // frame that represents the state of our types after any loop iteration
                var continueTypes = loopTypes.ContinueFrames.Aggregate((x, y) => x.CombineRefinementsWith(y));

                // If that frame doesn't match our starting frame, we type-checked with types that
                // are too specific and we need to combine them and do this again
                if (!types.DoValuesMatchWith(continueTypes)) {
                    types = types.CombineRefinementsWith(continueTypes);
                    continue;
                }

                // If we don't have any break frames, that means the loop is either infinite or it
                // returns from the function without going to the next statement after the loop.
                // In this case, we can be done
                if (loopTypes.BreakFrames.IsEmpty) {
                    alwaysJumps = true;
                    break;
                }

                // If our continue types do match the initial frame, then we need to combine all of
                // the break types to create the frame for after the loop
                types = loopTypes.BreakFrames.Aggregate((x, y) => x.CombineRefinementsWith(y));
                break;
            }
            finally {
                types = types.PopScope().PopLoopFrames();
            }
        }

        var result = new LoopStatement {
            Location = this.Location,
            Body = body,
            AlwaysJumps = alwaysJumps
        };

        return new(result, types);
    }

    private (ITypedStatement body, TypeFrame types) TypeCheckLoopBody(TypeFrame initalTypes) {
        // Actually typecheck the loop body
        var (body, loopTypes) = this.Body.CheckTypes(initalTypes);

        // If the body itself doesn't jump, then add its types as an implicit continue frame
        if (!body.AlwaysJumps) {
            loopTypes = loopTypes.WithContinueFrame(loopTypes);
        }

        return (body, loopTypes);
    }
}