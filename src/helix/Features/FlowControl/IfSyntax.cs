using Helix.Analysis.Types;
using Helix.Analysis;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using System.Reflection;
using Helix.Features.Variables;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Collections;
using Helix.Analysis.Predicates;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree IfExpression() {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.ThenKeyword);
            var affirm = this.TopExpression();

            if (this.TryAdvance(TokenKind.ElseKeyword)) {
                var neg = this.TopExpression();
                var loc = start.Location.Span(neg.Location);

                return new IfSyntax(loc, cond, affirm, neg);
            }
            else {
                var loc = start.Location.Span(affirm.Location);

                return new IfSyntax(loc, cond, affirm);
            }
        }
    }
}

namespace Helix.Features.FlowControl {
    public record IfSyntax : ISyntaxTree {
        private static int ifTempCounter = 0;

        private readonly ISyntaxTree cond, iftrue, iffalse;
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.cond, this.iftrue, this.iffalse };

        public bool IsPure { get; }

        public bool IsStatement => true;

        public IfSyntax(
            TokenLocation location,
            ISyntaxTree cond,
            ISyntaxTree iftrue) {

            this.Location = location;
            this.cond = cond;

            this.iftrue = new BlockSyntax(
                iftrue, 
                new VoidLiteral(iftrue.Location)
            );

            this.iffalse = new VoidLiteral(location);
            this.IsPure = cond.IsPure && iftrue.IsPure;
            this.path = new IdentifierPath("$if" + ifTempCounter++);
        }

        public IfSyntax(TokenLocation location, ISyntaxTree cond, ISyntaxTree iftrue,
                        ISyntaxTree iffalse) 
            : this(location, cond, iftrue, iffalse, new IdentifierPath("$if" + ifTempCounter++)) {}

        public IfSyntax(TokenLocation location, ISyntaxTree cond, ISyntaxTree iftrue,
                        ISyntaxTree iffalse, IdentifierPath path) 
            : this(location, cond, iftrue) {

            this.Location = location;
            this.cond = cond;

            this.iftrue = iftrue;
            this.iffalse = iffalse;
            
            this.path = path;
            this.IsPure = cond.IsPure && iftrue.IsPure && iffalse.IsPure;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var cond = this.cond
                .CheckTypes(types)
                .ToRValue(types);

            var condPredicate = ISyntaxPredicate.Empty;
            if (cond.GetReturnType(types) is PredicateBool pbool) {
                condPredicate = pbool.Predicate;
            }

            cond = cond.UnifyTo(PrimitiveType.Bool, types);

            var truePath = types.Scope.Append(this.path.Segments.Last() + "T");
            var falsePath = types.Scope.Append(this.path.Segments.Last() + "F");

            types.ControlFlow.AddEdge(types.Scope, truePath, condPredicate);
            types.ControlFlow.AddEdge(types.Scope, falsePath, condPredicate.Negate());

            var cont = types.ControlFlow.GetContinuation(types.Scope);
            types.ControlFlow.AddContinuation(truePath, cont);
            types.ControlFlow.AddContinuation(falsePath, cont);

            var iftrueTypes = new TypeFrame(types, truePath);
            var iffalseTypes = new TypeFrame(types, falsePath);

            var ifTruePred = types.ControlFlow.GetPredicates(truePath);
            var ifFalsePred = types.ControlFlow.GetPredicates(falsePath);

            var iftrue = ifTruePred.Apply(this.iftrue, iftrueTypes).CheckTypes(iftrueTypes).ToRValue(iftrueTypes);
            var iffalse = ifFalsePred.Apply(this.iffalse, iffalseTypes).CheckTypes(iffalseTypes).ToRValue(iffalseTypes);

            iftrue = iftrue.UnifyFrom(iffalse, types);
            iffalse = iffalse.UnifyFrom(iftrue, types);

            if (!types.ControlFlow.AlwaysReturns(truePath)) {
                types.ControlFlow.AddEdge(truePath, cont);
            }

            if (!types.ControlFlow.AlwaysReturns(falsePath)) {
                types.ControlFlow.AddEdge(falsePath, cont);
            }

            MutateLocals(iftrueTypes, iffalseTypes, types);

            var resultType = iftrue.GetReturnType(types);

            var result = new IfSyntax(
                this.Location,
                cond,
                iftrue,
                iffalse,
                types.Scope.Append(this.path));

            var pred1 = iftrue.GetPredicate(types);
            var pred2 = iffalse.GetPredicate(types);
            var pred3 = pred1.And(pred2);

            SyntaxTagBuilder.AtFrame(types)
                .WithChildren(cond, iftrue, iffalse)
                .WithReturnType(resultType)
                .WithPredicate(pred3)
                .WithLifetimes(AnalyzeFlow(result.path, iftrue, iffalse, types))
                .BuildFor(result);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            return this;
        }

        private static LifetimeBounds AnalyzeFlow(IdentifierPath path, ISyntaxTree iftrue, 
                                                  ISyntaxTree iffalse, TypeFrame flow) {
            var ifTrueBounds = iftrue.GetLifetimes(flow);
            var ifFalseBounds = iffalse.GetLifetimes(flow);

            if (iftrue.GetReturnType(flow).IsValueType(flow) || iffalse.GetReturnType(flow).IsValueType(flow)) {
                return new LifetimeBounds();
            }

            var role = LifetimeRole.Alias;
            var newRoots = false
                || HasNewRoots(ifTrueBounds.ValueLifetime, flow) 
                || HasNewRoots(ifFalseBounds.ValueLifetime, flow);

            if (newRoots) {
                role = LifetimeRole.Root;
            }

            var valueLifetime = new ValueLifetime(
                path,  
                role, 
                LifetimeOrigin.TempValue);

            flow.DataFlow.AddAssignment(
                valueLifetime, 
                ifTrueBounds.ValueLifetime, 
                iftrue.GetReturnType(flow));

            flow.DataFlow.AddAssignment(
                valueLifetime, 
                ifFalseBounds.ValueLifetime,
                iffalse.GetReturnType(flow));

            if (newRoots) {
                flow.ValidRoots = flow.ValidRoots.Add(valueLifetime);
            }

            return new LifetimeBounds(valueLifetime);
        }

        private static bool HasNewRoots(Lifetime lifetime, TypeFrame flow) {
            var roots = flow.GetMaximumRoots(lifetime);

            return roots.Any(x => !flow.ValidRoots.Contains(x));
        }

        private static void MutateLocals(
            TypeFrame trueFrame,
            TypeFrame falseFrame,
            TypeFrame flow) {

            var modifiedLocals = trueFrame.Locals
                .Concat(falseFrame.Locals)
                .Where(x => !flow.Locals.Contains(x))
                .Select(x => x.Key)
                .Where(flow.Locals.ContainsKey)
                .ToArray();

            foreach (var varPath in modifiedLocals) {
                var parentType = flow.Locals[varPath].Type;

                var trueLocal = trueFrame
                    .Locals
                    .GetValueOrNone(varPath)
                    .OrElse(() => new LocalInfo(parentType));

                var falseLocal = falseFrame
                    .Locals
                    .GetValueOrNone(varPath)
                    .OrElse(() => new LocalInfo(parentType));

                var trueLifetime = trueLocal.Bounds.ValueLifetime;
                var falseLifetime = falseLocal.Bounds.ValueLifetime;

                Lifetime postLifetime;
                if (trueLifetime.Version >= falseLifetime.Version || falseLifetime == Lifetime.None) {
                    postLifetime = trueLifetime.IncrementVersion();
                }
                else {
                    postLifetime = falseLifetime.IncrementVersion();
                }

                flow.DataFlow.AddAssignment(trueLifetime, postLifetime, trueLocal.Type);
                flow.DataFlow.AddAssignment(falseLifetime, postLifetime, falseLocal.Type);

                var roots = flow.GetMaximumRoots(postLifetime);

                // If the new value of this variable depends on a lifetime that was created
                // inside the loop, we need to declare a new root so that nothing after the
                // loop uses code that is no longer in scope
                if (roots.Any(x => !flow.ValidRoots.Contains(x))) {
                    postLifetime = new ValueLifetime(
                        postLifetime.Path,
                        LifetimeRole.Root,
                        LifetimeOrigin.TempValue,
                        postLifetime.Version + 1);
                }

                var newLocal = flow.Locals[varPath]
                    .WithBounds(new LifetimeBounds(postLifetime))
                    .WithType(parentType.GetMutationSupertype(flow));                

                flow.Locals = flow.Locals.SetItem(varPath, newLocal);
            }
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var affirmList = new List<ICStatement>();
            var negList = new List<ICStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            var affirm = this.iftrue.GenerateCode(types, affirmWriter);
            var neg = this.iffalse.GenerateCode(types, negWriter);

            var tempName = writer.GetVariableName();
            var returnType = this.GetReturnType(types);

            if (returnType != PrimitiveType.Void) {
                affirmWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = affirm
                });

                negWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = neg
                });
            }

            var tempStat = new CVariableDeclaration() {
                Type = writer.ConvertType(returnType, types),
                Name = tempName
            };

            if (affirmList.Any() && affirmList.Last().IsEmpty) {
                affirmList.RemoveAt(affirmList.Count - 1);
            }

            if (negList.Any() && negList.Last().IsEmpty) {
                negList.RemoveAt(negList.Count - 1);
            }

            var expr = new CIf() {
                Condition = this.cond.GenerateCode(types, writer),
                IfTrue = affirmList,
                IfFalse = negList
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.cond.Location.Line}: If statement");

            // Don't bother writing the temp variable if we are returning void
            if (returnType != PrimitiveType.Void) {
                writer.WriteStatement(tempStat);
            }

            writer.WriteStatement(expr);
            writer.WriteEmptyLine();

            if (returnType != PrimitiveType.Void) {
                return new CVariableLiteral(tempName);
            }
            else {
                return new CIntLiteral(0);
            }
        }
    }
}