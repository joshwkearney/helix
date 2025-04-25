using Helix.Parsing;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.Primitives;

namespace Helix.Features.FlowControl {
    public record BlockParseSyntax : IParseSyntax {
        public static IParseSyntax FromMany(TokenLocation loc, IReadOnlyList<IParseSyntax> stats) {
            if (stats.Count == 0) {
                return new VoidLiteral { Location = loc };
            }
            else if (stats.Count == 1) {
                return stats[0];
            }
            else {
                return stats
                    .Reverse()
                    .Aggregate((x, y) => new BlockParseSyntax {
                        Location = y.Location.Span(x.Location),
                        First = y,
                        Second = x
                    });
            }
        }

        public required TokenLocation Location { get; init; }
        
        public required IParseSyntax First { get; init; }
        
        public required IParseSyntax Second { get; init; }

        public bool IsPure => this.First.IsPure && this.Second.IsPure;
        
        public ISyntax CheckTypes(TypeFrame types) {
            // Check the first statement
            var first = this.First.CheckTypes(types);
            var predicate = first.Predicate;

            // Deepen the scope because the predicate might want to shadow variables
            // and it will need a new path to do so
            var statTypes = new TypeFrame(types, "$block");

            // Apply this predicate to the current context
            var newStats = predicate.ApplyToTypes(this.Second.Location, statTypes);
            var stat = this.Second;

            // Only make a new block if the predicate injected any statements
            if (newStats.Count > 0) {
                stat = FromMany(
                    this.Second.Location, 
                    newStats.Append(this.Second).ToArray());
            }

            // Evaluate this statement and get the next predicate
            var second = stat.CheckTypes(statTypes).ToRValue(statTypes);

            var result = new BlockSyntax {
                Location = this.Location,
                First = first,
                Second = second
            };

            return result;
        }
    }

}
