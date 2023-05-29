﻿using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.FlowControl;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Predicates;
using System;

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

            return new BlockSyntax(loc, stats);
        }
    }
}

namespace Helix.Features.FlowControl {
    public record BlockSyntax : ISyntaxTree {
        private static int blockCounter = 0;
        private static int statCounter = 0;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.Statements;

        public IReadOnlyList<ISyntaxTree> Statements { get; }

        public bool IsPure { get; }

        public IdentifierPath Path { get; }

        public BlockSyntax(TokenLocation location, IReadOnlyList<ISyntaxTree> statements, IdentifierPath path) {
            this.Location = statements.Select(x => x.Location).Prepend(location).Last();
            this.Statements = statements;
            this.IsPure = this.Statements.All(x => x.IsPure);
            this.Path = path;
        }

        public BlockSyntax(TokenLocation location, IReadOnlyList<ISyntaxTree> statements) 
            : this(location, statements, new IdentifierPath("$b" + blockCounter++)) { }

        public BlockSyntax(ISyntaxTree statement) : this(statement.Location, new[] { statement }) { }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            if (this.Statements.Count == 1) {
                return this.Statements[0].CheckTypes(types);
            }

            var name = this.Path.Segments.Last();
            var bodyTypes = new TypeFrame(types, name);

            var stats = new List<ISyntaxTree>();
            var predicate = ISyntaxPredicate.Empty;
            var statTypes = bodyTypes;

            foreach (var forStat in this.Statements) {
                var stat = this.CheckStatement(forStat, predicate, bodyTypes);                
                stats.Add(stat);

                // Get this predicate's effects for the next statement
                predicate = stat.GetPredicate(statTypes);
            }

            var result = new BlockSyntax(this.Location, stats, this.Path);
            var returnType = stats
                .LastOrNone()
                .Select(x => bodyTypes.ReturnTypes[x])
                .OrElse(() => PrimitiveType.Void);

            result.SetReturnType(returnType, bodyTypes);
            result.SetCapturedVariables(stats, bodyTypes);
            result.SetPredicate(ISyntaxPredicate.Empty, bodyTypes);

            types.MergeFrom(bodyTypes);

            return result;
        }

        private ISyntaxTree CheckStatement(ISyntaxTree stat, ISyntaxPredicate predicate, TypeFrame types) {
            // Apply this predicate's effects to the later statements
            if (predicate == ISyntaxPredicate.Empty) {
                return stat.CheckTypes(types).ToRValue(types);
            }
            else { 
                // Deepen the scope because the predicate might want to shadow variables
                // and it will need a new path to do so
                var statTypes = new TypeFrame(types, "$s" + statCounter++);

                // Apply this predicate to the current context
                var newStats = predicate
                    .ApplyToTypes(stat.Location, statTypes)
                    .Append(stat)
                    .ToArray();

                // Only make a new block if the predicate injected any statements
                if (newStats.Length > 0) {
                    stat = new BlockSyntax(stat.Location, newStats);
                }

                // Evaluate this statement and get the next predicate
                stat = stat.CheckTypes(statTypes).ToRValue(statTypes);

                types.MergeFrom(statTypes);
                return stat;
            }
        }

        public void AnalyzeFlow(FlowFrame flow) {
            foreach (var stat in this.Statements) {
                stat.AnalyzeFlow(flow);
            }

            var bundle = this.Statements
                .LastOrNone()
                .Select(x => x.GetLifetimes(flow))
                .OrElse(() => new LifetimeBounds());

            this.SetLifetimes(bundle, flow);
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            if (this.Statements.Any()) {
                foreach (var stat in this.Statements.SkipLast(1)) {
                    stat.GenerateCode(types, writer);
                }

                return this.Statements.Last().GenerateCode(types, writer);
            }
            else {
                return new CIntLiteral(0);
            }
        }
    }
}
