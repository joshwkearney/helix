using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.Features.Functions;
using Attempt20.Parsing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt20.Features.Containers {
    public class MemberInvokeParsedSyntax : IParsedSyntax {
        private IdentifierPath region;

        public TokenLocation Location { get; set; }

        public string MemberName { get; set; }

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

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            var target = this.Target.CheckTypes(names, types);
            var args = this.Arguments.Select(x => x.CheckTypes(names, types)).Prepend(target).ToArray();

            // Make sure this method exists
            if (!types.TryGetMethodPath(target.ReturnType, this.MemberName).TryGetValue(out var path)) {
                throw TypeCheckingErrors.MemberUndefined(this.Location, target.ReturnType, this.MemberName);
            }

            // Get the function
            var func = types.TryGetFunction(path).GetValue();

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

            return new FunctionInvokeSyntax() {
                Location = this.Location,
                ReturnType = func.ReturnType,
                Lifetimes = lifetimes,
                Arguments = args,
                Target = func,
                RegionName = this.region.Segments.Last(),
                TargetPath = path
            };
        }
    }
}
