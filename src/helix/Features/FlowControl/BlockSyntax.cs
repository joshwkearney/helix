using Helix.Analysis;
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
using Helix.Features.Primitives;
using System.ComponentModel;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree Block() {
            var start = this.Advance(TokenKind.OpenBrace);

            ISyntaxTree stat;
            if (this.Peek(TokenKind.CloseBrace)) {
                stat = new VoidLiteral(start.Location);
            }
            else {
                stat = this.BlockStatement();
            }

            this.Advance(TokenKind.CloseBrace);

            return stat;
        }

        private ISyntaxTree BlockStatement() {
            var first = this.Statement();

            if (!this.Peek(TokenKind.CloseBrace)) {
                first = new BlockSyntax(first, this.BlockStatement());
            }

            return first;
        }
    }
}

namespace Helix.Features.FlowControl {
    public record BlockSyntax : ISyntaxTree {
        private static int blockCounter = 0;

        public TokenLocation Location { get; }

        public ISyntaxTree First { get; }

        public ISyntaxTree Second { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.First, this.Second };

        public bool IsPure { get; }

        public IdentifierPath Path { get; }

        public BlockSyntax(ISyntaxTree first, ISyntaxTree second, IdentifierPath path) {
            this.Location = first.Location.Span(second.Location);
            this.First = first;
            this.Second = second;
            this.IsPure = this.First.IsPure && this.Second.IsPure;
            this.Path = path;
        }

        public BlockSyntax(ISyntaxTree first, ISyntaxTree second) 
            : this(first, second, new IdentifierPath("$b" + blockCounter++)) { }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var firstScope = types.Scope;
            var secondScope = firstScope.Append(this.Path);

            var hasParentContinuation = types.ControlFlow.TryGetContinuation(firstScope, out var cont);
            types.ControlFlow.SetContinuation(firstScope, secondScope);

            var first = this.First.CheckTypes(types).ToRValue(types);
            if (types.ControlFlow.AlwaysReturns(firstScope)) {
                return first;
            }
            else {
                types.ControlFlow.AddEdge(firstScope, secondScope);
            }

            var secondTypes = new TypeFrame(types, secondScope);
            var predicate = first.GetPredicate(types);

            if (hasParentContinuation) {
                types.ControlFlow.SetContinuation(secondScope, cont);
            }

            var second = this.CheckStatement(this.Second, secondScope, predicate, secondTypes, out _);
            if (hasParentContinuation && !types.ControlFlow.AlwaysReturns(secondScope)) {
                types.ControlFlow.AddEdge(secondScope, cont);
            }

            var result = new BlockSyntax(first, second, this.Path);
            var returnType = second.GetReturnType(types);

            SyntaxTagBuilder.AtFrame(types)
                .WithChildren(first, second)
                .WithReturnType(returnType)
                .WithPredicate(predicate)
                .WithLifetimes(second.GetLifetimes(types))
                .BuildFor(result);

            return result;
        }

        private ISyntaxTree CheckStatement(ISyntaxTree stat, IdentifierPath name, ISyntaxPredicate predicate,
                                           TypeFrame types, out TypeFrame newTypes) {
            var statTypes = new TypeFrame(types, name);

            // Apply this predicate to the current context
            //var newStats = predicate
            //    .ApplyToTypes(stat.Location, statTypes)
            //    .Append(stat)
            //    .ToArray();

            //// Only make a new block if the predicate injected any statements
            //if (newStats.Length > 1) {
            //    stat = new BlockSyntax(stat.Location, newStats);
            //}

            // Evaluate this statement and get the next predicate
            var result = stat.CheckTypes(statTypes).ToRValue(statTypes);

            MutateLocals(statTypes, types);

            newTypes = statTypes;
            return result;
        }

        private static void MutateLocals(TypeFrame bodyTypes, TypeFrame types) {
            if (types == bodyTypes) {
                return;
            }

            var modifiedLocalLifetimes = bodyTypes.Locals
                .Where(x => !types.Locals.Contains(x))
                .Select(x => x.Key)
                .Where(types.Locals.ContainsKey)
                .ToArray();

            foreach (var path in modifiedLocalLifetimes) {
                var oldBounds = types.Locals[path];
                var newBounds = bodyTypes.Locals[path];

               // var roots = types.GetMaximumRoots(newBounds.ValueLifetime);

                // If the new value of this variable depends on a lifetime that was created
                // inside the loop, we need to declare a new root so that nothing after the
                // loop uses code that is no longer in scope
                //if (roots.Any(x => !types.LifetimeRoots.Contains(x))) {
                //    var newRoot = new ValueLifetime(
                //        oldBounds.ValueLifetime.Path,
                //        LifetimeRole.Root,
                //        LifetimeOrigin.TempValue,
                //        Math.Max(oldBounds.ValueLifetime.Version, newBounds.ValueLifetime.Version));

                //    // Add our new root to the list of acceptable roots
                //    types.LifetimeRoots = types.LifetimeRoots.Add(newRoot);

                //    newBounds = newBounds.WithValue(newRoot);
                //}

                // Replace the current value with our root
                types.Locals = types.Locals.SetItem(path, newBounds);
            }
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
