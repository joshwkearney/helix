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

        public bool IsStatement => true;

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

            var firstScope = types.Scope.Append(this.Path);
            var secondScope = firstScope.Append("2");

            types.ControlFlow.AddEdge(types.Scope, firstScope);

            var continuation = types.ControlFlow.GetContinuation(types.Scope);
            types.ControlFlow.AddContinuation(firstScope, secondScope);
            types.ControlFlow.AddContinuation(secondScope, continuation);

            var firstTypes = new TypeFrame(types, firstScope);
            var first = this.First.CheckTypes(firstTypes).ToRValue(firstTypes);

            if (!first.IsStatement && !firstTypes.ControlFlow.AlwaysReturns(firstScope)) {
                firstTypes.ControlFlow.AddEdge(firstScope, secondScope);
            }
            
            var pred = types.ControlFlow.GetPredicates(secondScope);
            var secondTypes = new TypeFrame(firstTypes, secondScope);
            var second = pred.Apply(this.Second, secondTypes).CheckTypes(secondTypes).ToRValue(secondTypes);

            if (!second.IsStatement && !types.ControlFlow.AlwaysReturns(secondScope)) {
                types.ControlFlow.AddEdge(secondScope, continuation);
            }

            var result = new BlockSyntax(first, second, this.Path);
            var returnType = second.GetReturnType(firstTypes);

            SyntaxTagBuilder.AtFrame(firstTypes)
                .WithChildren(first, second)
                .WithReturnType(returnType)
                .WithLifetimes(second.GetLifetimes(firstTypes))
                .BuildFor(result);

            MutateLocals(secondTypes, firstTypes);
            MutateLocals(firstTypes, types);

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
