using Attempt17.Parsing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt17.Features.Block {
    public class BlockParseTree : IParseTree {
        public TokenLocation Location { get; }

        public ImmutableList<IParseTree> Statements { get; }

        public BlockParseTree(TokenLocation location, ImmutableList<IParseTree> stats) {
            this.Location = location;
            this.Statements = stats;
        }

        public ISyntaxTree Analyze(Scope scope) {
            // Get the id for this scope
            var id = scope.NextBlockId;
            var blockPath = scope.Path.Append("block" + id);

            // Get a new scope for analyzing the statements
            var blockScope = scope.WithNextBlockId(0).SelectPath(_ => blockPath);

            // Analyze the statements
            var statScope = blockScope;
            var stats = ImmutableList<ISyntaxTree>.Empty;

            foreach (var stat in this.Statements) {
                var check = stat.Analyze(statScope);

                statScope = check.ModifyLateralScope(statScope);
                stats = stats.Add(check);
            }

            // Make sure we're not about to return a value that's dependent on variables
            // within this scope
            if (stats.Any()) {
                var captured = stats.Last().CapturedVariables;

                foreach (var var in captured) {
                    if (var.StartsWith(blockPath)) {
                        throw TypeCheckingErrors.VariableScopeExceeded(this.Statements.Last().Location, var);
                    }
                }
            }

            return new BlockSyntaxTree(stats);
        }
    }
}