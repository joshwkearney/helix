using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Functions {
    public class FunctionInvokeSyntaxA : ISyntaxA {
        private readonly ISyntaxA target;
        private readonly IReadOnlyList<ISyntaxA> args;

        public TokenLocation Location { get; }

        public FunctionInvokeSyntaxA(TokenLocation loc, ISyntaxA target, IReadOnlyList<ISyntaxA> args) {
            this.Location = loc;
            this.target = target;
            this.args = args;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            var target = this.target.CheckNames(names);
            var args = this.args.Select(x => x.CheckNames(names)).ToArray();
            var region = IdentifierPath.HeapPath;

            if (names.CurrentRegion != IdentifierPath.StackPath) {
                region = names.CurrentRegion;
            }

            return new FunctionInvokeSyntaxB(this.Location, target, args, region);
        }
    }

    public class FunctionInvokeSyntaxB : ISyntaxB {
        private readonly ISyntaxB target;
        private readonly IReadOnlyList<ISyntaxB> args;
        private readonly IdentifierPath region;

        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get => this.args
                .Select(x => x.VariableUsage)
                .Append(this.target.VariableUsage)
                .Aggregate((x, y) => x.AddRange(y));
        }

        public FunctionInvokeSyntaxB(TokenLocation location, ISyntaxB target, IReadOnlyList<ISyntaxB> args, IdentifierPath region) {
            this.Location = location;
            this.target = target;
            this.args = args;
            this.region = region;
        }

        private IReadOnlyList<ISyntaxC> CheckArgs(
            IReadOnlyList<ISyntaxC> args, 
            IReadOnlyList<ITrophyType> pars, 
            ITypeRecorder types) {

            // Make sure the arg count lines up
            if (args.Count != pars.Count) {
                throw TypeCheckingErrors.ParameterCountMismatch(this.Location, pars.Count, args.Count);
            }

            var result = new ISyntaxC[args.Count];

            // Make sure the arg types line up
            for (int i = 0; i < args.Count; i++) {
                var expected = pars[i];
                var actual = args[i].ReturnType;

                if (!types.TryUnifyTo(args[i], expected).TryGetValue(out var newArg)) {
                    throw TypeCheckingErrors.UnexpectedType(this.Location, expected, actual);
                }

                result[i] = newArg;
            }

            return result;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var target = this.target.CheckTypes(types);
            var args = (IReadOnlyList<ISyntaxC>)this.args.Select(x => x.CheckTypes(types)).ToArray();
            var lifetimes = args
                .Select(x => x.Lifetimes)
                .Aggregate(ImmutableHashSet.Create<IdentifierPath>(), (x, y) => x.Union(y))
                // .Union(target.Lifetimes)
                .Add(this.region)
                .Remove(IdentifierPath.StackPath);

            // Make sure the target is a function
            if (target.ReturnType.AsSingularFunctionType().TryGetValue(out var singFuncType)) {
                if (!types.TryGetFunction(singFuncType.FunctionPath).TryGetValue(out var sig)) {
                    throw new Exception("Internal compiler inconsistency");
                }

                // Process the args
                var pars = sig.Parameters.Select(x => x.Type).ToArray();
                args = this.CheckArgs(args, pars, types);

                // If this type is copiable then we don't capture anything
                if (sig.ReturnType.GetCopiability(types) == TypeCopiability.Unconditional) {
                    lifetimes = lifetimes.Clear();
                }

                return new SingularFunctionInvokeSyntaxC(
                    target: singFuncType.FunctionPath,
                    args: args,
                    region: this.region.Segments.Last(),
                    returnType: sig.ReturnType,
                    lifetimes: lifetimes);
            }
            else if (target.ReturnType.AsFunctionType().TryGetValue(out var funcType)) {
                args = this.CheckArgs(args, funcType.ParameterTypes, types);

                // Make sure we take into account the target, because it is captured
                lifetimes = lifetimes.Union(target.Lifetimes);

                // If this type is copiable then we don't capture anything
                if (funcType.ReturnType.GetCopiability(types) == TypeCopiability.Unconditional) {
                    lifetimes = lifetimes.Clear();
                }

                return new FunctionInvokeSyntaxC(
                    target: target,
                    args: args,
                    returnType: funcType.ReturnType,
                    lifetimes: lifetimes);
            }
            else {
                throw TypeCheckingErrors.ExpectedFunctionType(this.target.Location, target.ReturnType);
            }            
        }
    }

    public class FunctionInvokeSyntaxC : ISyntaxC {
        private readonly ISyntaxC target;
        private readonly IReadOnlyList<ISyntaxC> args;

        public ITrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; }

        public FunctionInvokeSyntaxC(
            ISyntaxC target, 
            IReadOnlyList<ISyntaxC> args, 
            ITrophyType returnType, 
            ImmutableHashSet<IdentifierPath> lifetimes) {

            this.target = target;
            this.args = args;
            this.ReturnType = returnType;
            this.Lifetimes = lifetimes;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            var target = this.target.GenerateCode(writer, statWriter);
            var args = this.args
                .Select(x => x.GenerateCode(writer, statWriter))
                .Prepend(CExpression.MemberAccess(target, "environment"))
                .ToArray();

            var invoke = CExpression.Invoke(
                CExpression.MemberAccess(target, "function"),
                args);

            return invoke;
        }
    }

    public class SingularFunctionInvokeSyntaxC : ISyntaxC {
        private readonly IdentifierPath targetPath;
        private readonly IReadOnlyList<ISyntaxC> args;
        private readonly string region;

        public ITrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; }

        public SingularFunctionInvokeSyntaxC(
            IdentifierPath target, 
            IReadOnlyList<ISyntaxC> args, 
            string region, 
            ITrophyType returnType, 
            ImmutableHashSet<IdentifierPath> lifetimes) {

            this.targetPath = target;
            this.args = args;
            this.region = region;
            this.ReturnType = returnType;
            this.Lifetimes = lifetimes;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var args = this.args
                .Select(x => x.GenerateCode(declWriter, statWriter))
                .Prepend(CExpression.VariableLiteral(this.region))
                .ToArray();

            var target = CExpression.VariableLiteral("$" + this.targetPath);
            var invoke = CExpression.Invoke(target, args);

            if (this.ReturnType.IsVoidType) {
                statWriter.WriteStatement(CStatement.FromExpression(invoke));

                return CExpression.IntLiteral(0);
            }
            else {
                return invoke;
            }
        }
    }
}