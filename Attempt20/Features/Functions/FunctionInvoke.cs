using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt20.CodeGeneration;

namespace Attempt20.Features.Functions {
    public class FunctionInvokeParseSyntax : IParsedSyntax {
        private IdentifierPath region;

        public TokenLocation Location { get; set; }

        public IParsedSyntax Target { get; set; }

        public IReadOnlyList<IParsedSyntax> Arguments { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.Target = this.Target.CheckNames(names);
            this.Arguments = this.Arguments.Select(x => x.CheckNames(names)).ToArray();

            if (names.CurrentRegion == IdentifierPath.StackPath) {
                this.region = IdentifierPath.HeapPath;
            }
            else {
                this.region = names.CurrentRegion;
            }

            return this;
        }

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            var target = this.Target.CheckTypes(names, types);
            var args = this.Arguments.Select(x => x.CheckTypes(names, types)).ToArray();

            // Make sure the target is a function
            if (!target.ReturnType.AsSingularFunctionType().TryGetValue(out var funcType)) {
                throw TypeCheckingErrors.ExpectedFunctionType(target.Location, target.ReturnType);
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
                .Add(this.region);

            if (func.ReturnType.GetCopiability(types) == TypeCopiability.Unconditional) {
                lifetimes = lifetimes.Clear();
            }

            return new FunctionInvokeTypeCheckedSyntax() {
                Location = this.Location,
                ReturnType = func.ReturnType,
                Lifetimes = lifetimes,
                Arguments = args,
                Target = func,
                RegionName = this.region.Segments.Last(),
                TargetPath = funcType.FunctionPath
            };
        }
    }

    public class FunctionInvokeTypeCheckedSyntax : ITypeCheckedSyntax {
        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public FunctionSignature Target { get; set; }

        public IdentifierPath TargetPath { get; set; }

        public IReadOnlyList<ITypeCheckedSyntax> Arguments { get; set; }

        public string RegionName { get; set; }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            var args = this.Arguments.Select(x => x.GenerateCode(declWriter, statWriter)).Prepend(CExpression.VariableLiteral(this.RegionName)).ToArray();
            var target = CExpression.VariableLiteral(this.TargetPath.ToString());

            return CExpression.Invoke(target, args);
        }
    }
}