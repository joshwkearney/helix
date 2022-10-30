﻿using Helix.Analysis.Types;
using Helix.Analysis;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using System.Reflection;
using Helix.Features.Variables;
using Helix.Analysis.Lifetimes;
using Helix.Features.Memory;

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

                return new IfParseSyntax(loc, cond, affirm, neg);
            }
            else {
                var loc = start.Location.Span(affirm.Location);

                return new IfParseSyntax(loc, cond, affirm);
            }
        }
    }
}

namespace Helix.Features.FlowControl {
    public record IfParseSyntax : ISyntaxTree {
        private static int ifTempCounter = 0;

        private readonly ISyntaxTree cond, iftrue, iffalse;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.cond, this.iftrue, this.iffalse };

        public bool IsPure { get; }

        public IfParseSyntax(TokenLocation location, ISyntaxTree cond, 
            ISyntaxTree iftrue) {

            this.Location = location;
            this.cond = cond;

            this.iftrue = new BlockSyntax(iftrue.Location, new ISyntaxTree[] {
                iftrue, new VoidLiteral(iftrue.Location)
            });

            this.iffalse = new VoidLiteral(location);
            this.IsPure = cond.IsPure && iftrue.IsPure;
            this.tempPath = location.Scope.Append("$if_temp_" + ifTempCounter++);
        }

        public IfParseSyntax(TokenLocation location, ISyntaxTree cond, ISyntaxTree iftrue, 
            ISyntaxTree iffalse) : this(location, cond, iftrue) {

            this.Location = location;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
            this.IsPure = cond.IsPure && iftrue.IsPure && iffalse.IsPure;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var iftrueTypes = new SyntaxFrame(types);
            var iffalseTypes = new SyntaxFrame(types);

            var cond = this.cond.CheckTypes(types).ToRValue(types).UnifyTo(PrimitiveType.Bool, types);
            var iftrue = this.iftrue.CheckTypes(iftrueTypes).ToRValue(iftrueTypes);
            var iffalse = this.iffalse.CheckTypes(iffalseTypes).ToRValue(iffalseTypes);

            iftrue = iftrue.UnifyFrom(iffalse, types);
            iffalse = iffalse.UnifyFrom(iftrue, types);

            var newLifetimes = this.CalculateModifiedVariables(iftrueTypes, iffalseTypes, types);            

            // TODO: Fix this not giving the component path at all
            // Make sure to bind all the new lifetimes we have discovered
            var bindings = newLifetimes
                .Select(x => new BindLifetimeSyntax(this.Location, x.Lifetime, x.Path, new IdentifierPath()))
                .Select(x => (ISyntaxTree)x)
                .ToList();

            var lifetimeBundle = this.CalculateLifetimes(bindings, types);
            var resultType = types.ReturnTypes[iftrue];

            var result = new IfSyntax(
                this.Location,
                cond,
                iftrue,
                iffalse,
                resultType,
                this.tempPath,
                bindings);

            types.ReturnTypes[result] = resultType;
            types.Lifetimes[result] = lifetimeBundle;

            return result;
        }

        private LifetimeBundle CalculateLifetimes(List<ISyntaxTree> bindings, SyntaxFrame types) {
            var lifetimeBundle = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>();
            var resultType = types.ReturnTypes[iftrue];

            // If we are returning a reference type then we need to calculate a new lifetime
            // This is required because the lifetimes that were used inside of the if body
            // may not be availible outside of it, so we need to reuinify around a new lifetime
            foreach (var (relPath, type) in VariablesHelper.GetMemberPaths(resultType, types)) {
                var bodyLifetimes = types.Lifetimes[iftrue].ComponentLifetimes[relPath]
                    .Concat(types.Lifetimes[iffalse].ComponentLifetimes[relPath])
                    .ToValueList();

                var isRoot = bodyLifetimes.Any(x => x.IsRoot);
                var path = this.tempPath.Append(relPath);
                var resultLifetime = new Lifetime(path, 0, isRoot);

                lifetimeBundle.Add(relPath, new[] { resultLifetime });

                // Make sure that our new lifetime is derived from the body lifetimes, and that
                // the body lifetimes are precursors to our lifetime for the purposes of lifetime
                // analysis
                foreach (var bodyLifetime in bodyLifetimes) {
                    types.LifetimeGraph.AddDerived(bodyLifetime, resultLifetime);
                    types.LifetimeGraph.AddPrecursor(resultLifetime, bodyLifetime);
                }

                // Add this new lifetime to the list of bindings we calculated earlier
                // TODO: Fix this
                if (type is PointerType || type is ArrayType) {
                    bindings.Add(new BindLifetimeSyntax(this.Location, resultLifetime, this.tempPath, relPath));
                }
            }

            return new LifetimeBundle(lifetimeBundle);
        }

        private IReadOnlyList<VariableSignature> CalculateModifiedVariables(
            SyntaxFrame iftrueTypes,
            SyntaxFrame iffalseTypes, 
            SyntaxFrame types) {

            var modifiedVars = iftrueTypes.Variables
                .Concat(iffalseTypes.Variables)
                .Select(x => x.Key)
                .Intersect(types.Variables.Select(x => x.Key));

            var newLifetimes = new List<VariableSignature>();

            // For every variable mutated within this if statement, we need to create a new
            // lifetime for that variable after the if statement so that code that runs after
            // doesn't use outdated lifetime information. Also, variables can be changed
            // in different ways so we need to re-unify the branches anyway
            foreach (var path in modifiedVars) {
                var oldSig = types.Variables[path];

                // If this variable is changed in both paths, take the max mutation count and add one
                if (iftrueTypes.Variables.Keys.Contains(path) && iffalseTypes.Variables.Keys.Contains(path)) {
                    var trueSig = iftrueTypes.Variables[path];
                    var falseSig = iffalseTypes.Variables[path];

                    var mutationCount = 1 + Math.Max(
                        trueSig.Lifetime.MutationCount,
                        falseSig.Lifetime.MutationCount);

                    var newSig = new VariableSignature(
                        path,
                        oldSig.Type,
                        true,
                        mutationCount,
                        oldSig.Lifetime.IsRoot || trueSig.Lifetime.IsRoot || falseSig.Lifetime.IsRoot);

                    newLifetimes.Add(newSig);

                    types.Variables[path] = newSig;

                    // Make sure that the new lifetime is dependent on both if branches
                    types.LifetimeGraph.AddPrecursor(newSig.Lifetime, trueSig.Lifetime);
                    types.LifetimeGraph.AddPrecursor(newSig.Lifetime, falseSig.Lifetime);

                    // Make sure that the branch lifetimes are dependent on the new lifetime
                    types.LifetimeGraph.AddDerived(trueSig.Lifetime, newSig.Lifetime);
                    types.LifetimeGraph.AddDerived(falseSig.Lifetime, newSig.Lifetime);
                }
                else {
                    // If this variable is changed in only one path
                    Lifetime oldLifetime;

                    if (iftrueTypes.Variables.ContainsKey(path)) {
                        oldLifetime = iftrueTypes.Variables[path].Lifetime;
                    }
                    else {
                        oldLifetime = iffalseTypes.Variables[path].Lifetime;
                    }

                    var newSig = new VariableSignature(
                        path,
                        oldSig.Type,
                        oldSig.IsWritable,
                        oldLifetime.MutationCount + 1,
                        oldSig.Lifetime.IsRoot || oldLifetime.IsRoot);

                    newLifetimes.Add(newSig);

                    // Make sure the new lifetime is dependent on the if branch
                    types.LifetimeGraph.AddPrecursor(newSig.Lifetime, oldLifetime);
                    types.LifetimeGraph.AddDerived(oldLifetime, newSig.Lifetime);

                    // Make sure the new lifetime is dependent on the old lifetime
                    types.LifetimeGraph.AddPrecursor(newSig.Lifetime, oldSig.Lifetime);
                    types.LifetimeGraph.AddDerived(oldSig.Lifetime, newSig.Lifetime);

                    types.Variables[path] = newSig;
                }
            }

            return newLifetimes;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record IfSyntax : ISyntaxTree {
        private readonly ISyntaxTree cond, iftrue, iffalse;
        private readonly HelixType returnType;
        private readonly IdentifierPath tempPath;
        private readonly IEnumerable<ISyntaxTree> bindings;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.cond, this.iftrue, this.iffalse };

        public bool IsPure { get; }

        public IfSyntax(TokenLocation loc, ISyntaxTree cond,
                        ISyntaxTree iftrue,
                        ISyntaxTree iffalse, HelixType returnType,
                        IdentifierPath tempPath,
                        IEnumerable<ISyntaxTree> bindings) {

            this.Location = loc;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
            this.returnType = returnType;
            this.IsPure = cond.IsPure && iftrue.IsPure && iffalse.IsPure;
            this.tempPath = tempPath;
            this.bindings = bindings;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            var affirmList = new List<ICStatement>();
            var negList = new List<ICStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            var affirm = this.iftrue.GenerateCode(types, affirmWriter);
            var neg = this.iffalse.GenerateCode(types, negWriter);

            var tempName = writer.GetVariableName(this.tempPath);

            if (this.returnType != PrimitiveType.Void) {
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
                Type = writer.ConvertType(this.returnType),
                Name = tempName
            };

            var expr = new CIf() {
                Condition = this.cond.GenerateCode(types, writer),
                IfTrue = affirmList,
                IfFalse = negList
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.cond.Location.Line}: If statement");

            // Don't bother writing the temp variable if we are returning void
            if (this.returnType != PrimitiveType.Void) {
                writer.WriteStatement(tempStat);
            }

            writer.WriteStatement(expr);

            // Register all the lifetimes that changed within this if statement, including
            // the binding that register our return lifetime
            foreach (var binding in this.bindings) {
                binding.GenerateCode(types, writer);
            }

            writer.WriteEmptyLine();

            if (this.returnType != PrimitiveType.Void) {
                return new CVariableLiteral(tempName);
            }
            else {
                return new CIntLiteral(0);
            }
        }
    }
}