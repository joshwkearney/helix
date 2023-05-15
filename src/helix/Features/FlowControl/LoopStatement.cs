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
using Helix.Features.Memory;
using System;
using helix.Syntax;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();
            var newBlock = new List<ISyntaxTree>();

            var test = new IfSyntax(
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

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.body };

        public bool IsPure => false;

        public LoopStatement(TokenLocation location, ISyntaxTree body) {

            this.Location = location;
            this.body = body;
        }

        public Option<ISyntaxTree> ToRValue(EvalFrame types) {
            if (!types.ReturnTypes.ContainsKey(this)) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ISyntaxTree CheckTypes(EvalFrame types) {
            if (types.ReturnTypes.ContainsKey(this)) {
                return this;
            }

            var bodyTypes = new EvalFrame(types);
            var body = this.body.CheckTypes(bodyTypes).ToRValue(bodyTypes);
            var result = (ISyntaxTree)new LoopStatement(this.Location, body);

            types.ReturnTypes[result] = PrimitiveType.Void;

            return result;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            this.body.AnalyzeFlow(flow);

            flow.Lifetimes[this] = new LifetimeBundle();

            // TODO: Redo all of this
            // Get a list of all the mutable variables that could change in this loop body
            /*var potentiallyModifiedVars = this.body.GetAllChildren()
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
                .SelectMany(x => {
                    var list = new List<VariableSignature>();

                    foreach (var (relPath, _) in VariablesHelper.GetMemberPaths(x.Type, types)) {
                        list.Add(types.Variables[x.Path.Append(relPath)]);
                    }

                    return list;
                })
                .Where(x => x.IsWritable)
                .Where(x => x.Type.IsRemote(types))
                .Distinct()
                .ToValueList();

            var bodyVars = potentiallyModifiedVars
                .Select(sig => {
                    var newSig = new VariableSignature(
                        sig.Type,
                        sig.IsWritable,
                        new Lifetime(sig.Path, sig.Lifetime.MutationCount + 1));

                    return newSig;
                })
                .ToValueList();

            // For every variable that might be modified in the loop, create a new lifetime
            // for it in the loop body so that if it does change, it is only changing the
            // new variable signature and not the old one
            foreach (var sig in bodyVars) {
                bodyTypes.Variables[sig.Path] = sig;
            }

            var bodyBindings = new List<ISyntaxTree>();

            var modifiedVars = bodyVars
                .Select(x => x.Path)
                .Except(bodyTypes.Variables.Values.Select(x => x.Path))
                .Distinct()
                .ToValueList();

            // For every variable that does change, change the scope after the loop
            // by incrementing its mutation counter. Also, DO NOT link up the variable
            // before the loop with the new signature we made. This forces any code in 
            // the loop to treat this variable as a root since we don't know its origin
            foreach (var path in modifiedVars) {
                var sig = bodyTypes.Variables[path];

                var newSig = new VariableSignature(
                    sig.Type,
                    sig.IsWritable,
                    new Lifetime(sig.Path, sig.Lifetime.MutationCount + 1));

                types.Variables[sig.Path] = newSig;
                types.LifetimeGraph.AddAlias(newSig.Lifetime, sig.Lifetime);

                bodyBindings.Add(new BindLifetimeSyntax(this.Location, sig.Lifetime, sig.Path));
            }

            var postBindings = new List<ISyntaxTree>();

            var unmodifiedVars = potentiallyModifiedVars
                .Select(x => x.Path)
                .Except(modifiedVars)
                .Distinct()
                .ToValueList();

            // For every variable that is not changed in the loop, change the scope after
            // the loop to just be the new signature we made earlier. Also, since it didn't
            // change, we are safe to link up the original lifetime for this variable with what
            // will be seen in the loop
            foreach (var path in unmodifiedVars) {
                var sig = bodyTypes.Variables[path];
                var oldSig = types.Variables[path];

                types.Variables[sig.Path] = sig;
                types.LifetimeGraph.AddAlias(sig.Lifetime, oldSig.Lifetime);

                bodyBindings.Add(new BindLifetimeSyntax(this.Location, sig.Lifetime, sig.Path));
                postBindings.Add(new BindLifetimeSyntax(this.Location, sig.Lifetime, sig.Path));
            }*/
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
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
