using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.FlowControl;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Predicates;
using Helix.Features.Primitives;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseSyntax Block() {
            var start = this.Advance(TokenKind.OpenBrace);
            var stats = new List<IParseSyntax>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                stats.Add(this.Statement());
            }

            var end = this.Advance(TokenKind.CloseBrace);
            var loc = start.Location.Span(end.Location);

            return BlockParse.FromMany(loc, stats);
        }
    }
}

namespace Helix.Features.FlowControl {
    public record BlockParse : IParseSyntax {
        private static int blockCounter = 0;

        public static IParseSyntax FromMany(TokenLocation loc, IReadOnlyList<IParseSyntax> stats) {
            if (stats.Count == 0) {
                return new VoidLiteral(loc);
            }
            else if (stats.Count == 1) {
                return stats[0];
            }
            else {
                return stats
                    .Reverse()
                    .Aggregate((x, y) => new BlockParse(y.Location.Span(x.Location), y, x));
            }
        }

        public TokenLocation Location { get; }

        public IEnumerable<IParseSyntax> Children => [this.First, this.Second];

        public IParseSyntax First { get; }
        
        public IParseSyntax Second { get; }

        public bool IsPure { get; }
        
        public BlockParse(TokenLocation location, IParseSyntax first, IParseSyntax second) {
            this.Location = location;
            this.First = first;
            this.Second = second;
            this.IsPure = first.IsPure && second.IsPure;
        }
        
        public IParseSyntax CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }
            
            // Check the first statement
            var first = this.First.CheckTypes(types);
            var predicate = first.GetPredicate(types);

            // Deepen the scope because the predicate might want to shadow variables
            // and it will need a new path to do so
            var statTypes = new TypeFrame(types, "$" + blockCounter++);

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
            var result = new BlockParse(this.Location, first, second);
            var returnType = second.GetReturnType(statTypes);

            types.SyntaxTags[result] = new SyntaxTagBuilder(types)
                .WithChildren(first, second)
                .WithReturnType(returnType)
                .WithPredicate(predicate)
                .Build();

            return result;
        }

        public IParseSyntax ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            this.First.GenerateCode(types, writer);
            return this.Second.GenerateCode(types, writer);
        }
    }
}
