using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.FlowControl;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Primitives;
using System.Net;
using Helix.Analysis.Lifetimes;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();
            var newBlock = new List<ISyntaxTree>();

            var test = new IfParseSyntax(
                cond.Location,
                new UnaryParseSyntax(cond.Location, UnaryOperatorKind.Not, cond),
                new BreakContinueSyntax(cond.Location, true));

            // False loops will never run and true loops don't need a break test
            if (cond is not Features.Primitives.BoolLiteral) {
                newBlock.Add(test);
            }

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            this.isInLoop.Push(true);
            var body = this.TopExpression();
            this.isInLoop.Pop();

            newBlock.Add(body);

            var loc = start.Location.Span(body.Location);
            var loop = new LoopStatement(loc, new BlockSyntax(loc, newBlock));

            return loop;
        }
    }
}

namespace Helix.Features.FlowControl {
    public record LoopStatement : ISyntaxTree {
        private readonly ISyntaxTree body;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.body };

        public bool IsPure => false;

        public LoopStatement(TokenLocation location,
                             ISyntaxTree body, bool isTypeChecked = false) {

            this.Location = location;
            this.body = body;
            this.isTypeChecked = isTypeChecked;
        }

        public Option<ISyntaxTree> ToRValue(EvalFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ISyntaxTree CheckTypes(EvalFrame types) {
            if (this.isTypeChecked) {
                return this;
            }

            var potentiallyModifiedVars = this.body.GetAllChildren()
                .Select(x => x as VariableAccessParseSyntax)
                .Where(x => x != null)
                .Select(x => {
                    if (types.TryResolvePath(this.Location.Scope, x.Name, out var path)) {
                        return path;
                    }
                    else {
                        return new IdentifierPath();
                    }
                })
                .Where(x => x != new IdentifierPath())
                .Where(x => types.Variables.ContainsKey(x))
                .Select(x => types.Variables[x])
                .Where(x => x.IsWritable)
                .Where(x => x.Type.IsRemote(types))
                .Distinct()
                .ToValueList();

            var bodyTypes = new EvalFrame(types);

            foreach (var sig in potentiallyModifiedVars) {
                var newSig = new VariableSignature(
                    sig.Type,
                    sig.IsWritable,
                    new Lifetime(sig.Path, sig.Lifetime.MutationCount + 1));

                bodyTypes.Variables[sig.Path] = newSig;
            }

            var body = this.body.CheckTypes(bodyTypes).ToRValue(bodyTypes);
            var result = new LoopStatement(this.Location, body, true);

            var modifiedVars = bodyTypes.Variables.Values
                .Where(x => potentiallyModifiedVars.Any(y => y.Path == x.Path))
                .Except(potentiallyModifiedVars)
                .ToValueList();

            foreach (var sig in modifiedVars) {
                var oldSig = types.Variables[sig.Path];

                var newSig = new VariableSignature(
                    sig.Type,
                    sig.IsWritable,
                    new Lifetime(sig.Path, sig.Lifetime.MutationCount + 1));

                types.Variables[sig.Path] = newSig;

                types.LifetimeGraph.AddAlias(newSig.Lifetime, sig.Lifetime);
                types.LifetimeGraph.AddAlias(newSig.Lifetime, oldSig.Lifetime);
            }

            var unmodifiedVars = bodyTypes.Variables.Values
                .Intersect(potentiallyModifiedVars)
                .ToValueList();

            foreach (var sig in unmodifiedVars) {
                var oldSig = types.Variables[sig.Path];
                var bodySig = bodyTypes.Variables[sig.Path];

                types.Variables[sig.Path] = sig;
                types.LifetimeGraph.AddAlias(sig.Lifetime, oldSig.Lifetime);
                types.LifetimeGraph.AddAlias(sig.Lifetime, bodySig.Lifetime);
            }

            //var modifiedVars = bodyTypes
            //    .Variables
            //    .Select(x => x.Key)
            //    .Intersect(types.Variables.Select(x => x.Key))
            //    .ToArray();

            // TODO: Fix this

            // Loops are a very weird case to lifetime check. The problem is that loop
            // bodies can run more than once so we can have an assignment later in the
            // loop affect the lifetime of a variable earlier in the loop. The solution
            // is to first identify which variables in the loop body are modified using
            // a new syntax frame, and then for each modified variable, sum its lifetime
            // through a graph representing the modified variables in the loop. This will
            // trace the lifetime through the loop modifications until we hit a lifetime
            // that was not modified by the loop, which will stop the graph search. This
            // makes sure that the variable lifetimes after the loop represent all the 
            // posibilities that could have occured inside the loop.
            //foreach (var path in modifiedVars) {
            //    var found = new HashSet<IdentifierPath>();
            //    var search = new Stack<IdentifierPath>(new[] { path });
            //    var lifetime = new Lifetime();

            //    // Search through the modified variables of the loop until we discover all
            //    // possible mutations paths, summing the lifetime along the way
            //    while (search.Any()) {
            //        var next = search.Pop();

            //        // This variable is constant with respect to the loop, so we're done
            //        if (!bodyTypes.Variables.Keys.Contains(next)) {
            //            continue;
            //        }

            //        // We have already visited this variable and merged it, so skip it now
            //        // This prevents infinite loops, which can happen
            //        if (found.Contains(next)) {
            //            continue;
            //        }

            //        var sig = bodyTypes.Variables[next];

            //        lifetime = lifetime.Concat(sig.Lifetime);
            //        found.Add(next);

            //        foreach (var origin in sig.Lifetime.Dependencies) {
            //            search.Push(origin);
            //        }
            //    }

            //    var oldSig = types.Variables[path];

            //    // Add the newly discovered loop lifetime to the existing variable lifetime,
            //    // since loops may not run at all
            //    types.Variables[path] = new VariableSignature(
            //        path,
            //        oldSig.Type,
            //        oldSig.IsWritable,
            //        lifetime);
            //}

            types.ReturnTypes[result] = PrimitiveType.Void;
            types.Lifetimes[result] = new LifetimeBundle();

            return result;
        }

        public ICSyntax GenerateCode(EvalFrame types, ICStatementWriter writer) {
            var bodyStats = new List<ICStatement>();
            var bodyWriter = new CStatementWriter(writer, bodyStats);

            this.body.GenerateCode(types, bodyWriter);
            
            var stat = new CWhile() {
                Condition = new CIntLiteral(1),
                Body = bodyStats
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: While or for loop");
            writer.WriteStatement(stat);
            writer.WriteEmptyLine();

            return new CIntLiteral(0);
        }
    }
}
