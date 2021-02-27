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

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var target = this.target.CheckTypes(types);
            var args = this.args.Select(x => x.CheckTypes(types)).ToArray();

            // Make sure the target is a function
            if (!target.ReturnType.AsSingularFunctionType().TryGetValue(out var funcType)) {
                throw TypeCheckingErrors.ExpectedFunctionType(this.target.Location, target.ReturnType);
            }

            if (!types.TryGetFunction(funcType.FunctionPath).TryGetValue(out var func)) {
                throw new Exception("Internal compiler inconsistency");
            }

            // Make sure the arg count lines up
            if (args.Length != func.Parameters.Count) {
                throw TypeCheckingErrors.ParameterCountMismatch(this.Location, func.Parameters.Count, args.Length);
            }

            // Make sure the arg types line up
            for (int i = 0; i < args.Length; i++) {
                var expected = func.Parameters[i].Type;
                var actual = args[i].ReturnType;

                if (expected != actual) {
                    throw TypeCheckingErrors.UnexpectedType(this.Location, expected, actual);
                }
            }

            var lifetimes = args
                .Select(x => x.Lifetimes)
                .Aggregate(ImmutableHashSet.Create<IdentifierPath>(), (x, y) => x.Union(y))
                .Union(target.Lifetimes)
                .Add(this.region)
                .Remove(IdentifierPath.StackPath);

            if (func.ReturnType.GetCopiability(types) == TypeCopiability.Unconditional) {
                lifetimes = lifetimes.Clear();
            }

            return new SingularFunctionInvokeSyntaxC(
                target: funcType.FunctionPath,
                args: args,
                region: this.region.Segments.Last(),
                returnType: func.ReturnType,
                lifetimes: lifetimes);
        }
    }

    public class SingularFunctionInvokeSyntaxC : ISyntaxC {
        private readonly IdentifierPath targetPath;
        private readonly IReadOnlyList<ISyntaxC> args;
        private readonly string region;

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; }

        public SingularFunctionInvokeSyntaxC(IdentifierPath target, IReadOnlyList<ISyntaxC> args, 
            string region, TrophyType returnType, ImmutableHashSet<IdentifierPath> lifetimes) {

            this.targetPath = target;
            this.args = args;
            this.region = region;
            this.ReturnType = returnType;
            this.Lifetimes = lifetimes;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var args = this.args.Select(x => x.GenerateCode(declWriter, statWriter)).Prepend(CExpression.VariableLiteral(this.region)).ToArray();
            var target = CExpression.VariableLiteral("$" + this.targetPath);

            return CExpression.Invoke(target, args);
        }
    }
}