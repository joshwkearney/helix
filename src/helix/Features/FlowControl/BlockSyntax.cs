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
        private ISyntaxTree Block() {
            var start = this.Advance(TokenKind.OpenBrace);
            var stats = new List<ISyntaxTree>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                stats.Add(this.Statement());
            }

            var end = this.Advance(TokenKind.CloseBrace);
            var loc = start.Location.Span(end.Location);

            if (stats.Count == 0) {
                return new VoidLiteral(loc);
            }
            else if (stats.Count == 1) {
                return stats[0];
            }
            else {
                stats.Reverse();
                return stats.Aggregate((x, y) => new BlockSyntax(x.Location.Span(y.Location), y, x));
            }
        }
    }
}

namespace Helix.Features.FlowControl {
    public record BlockSyntax : ISyntaxTree {
        private static int blockCounter = 0;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => [this.First, this.Second];

        public ISyntaxTree First { get; }
        
        public ISyntaxTree Second { get; }

        public bool IsPure { get; }
        
        public BlockSyntax(TokenLocation location, ISyntaxTree first, ISyntaxTree second) {
            this.Location = location;
            this.First = first;
            this.Second = second;
            this.IsPure = first.IsPure && second.IsPure;
        }
        
        public ISyntaxTree CheckTypes(TypeFrame types) {
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
                stat = new CompoundSyntax(
                    this.Second.Location, 
                    newStats.Append(this.Second).ToArray());
            }

            // Evaluate this statement and get the next predicate
            var second = stat.CheckTypes(statTypes).ToRValue(statTypes);
            var result = new BlockSyntax(this.Location, first, second);
            var returnType = stat.GetReturnType(types);

            types.SyntaxTags[result] = new SyntaxTagBuilder(types)
                .WithChildren(first, stat)
                .WithReturnType(returnType)
                .WithPredicate(predicate)
                .Build();

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
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
