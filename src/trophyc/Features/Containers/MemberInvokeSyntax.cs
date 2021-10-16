using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Features.Functions;
using Trophy.Parsing;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Features.FlowControl;
using Trophy.Features.Variables;

namespace Trophy.Features.Containers {
    public class MemberInvokeSyntaxA : ISyntaxA {
        private readonly string memberName;
        private readonly ISyntaxA target;
        private readonly IReadOnlyList<ISyntaxA> args;

        public TokenLocation Location { get; }

        public MemberInvokeSyntaxA(TokenLocation location, ISyntaxA target, string memberName, IReadOnlyList<ISyntaxA> args) {
            this.Location = location;
            this.target = target;
            this.memberName = memberName;
            this.args = args;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var target = this.target.CheckNames(names);
            var args = this.args.Select(x => x.CheckNames(names)).ToArray();
            var region = RegionsHelper.GetClosestHeap(names.Context.Region);

            return new MemberInvokeSyntaxB(this.Location, target, this.memberName, args, region);
        }
    }

    public class MemberInvokeSyntaxB : ISyntaxB {
        private static int counter = 0;

        private readonly IdentifierPath enclosingHeap;
        private readonly string memberName;
        private readonly ISyntaxB target;
        private readonly IReadOnlyList<ISyntaxB> args;

        public TokenLocation Location { get; set; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.args
                .Select(x => x.VariableUsage)
                .Append(this.target.VariableUsage)
                .Aggregate((x, y) => x.Union(y))
                .Add(new VariableUsage(this.enclosingHeap, VariableUsageKind.Region));
        }

        public MemberInvokeSyntaxB(
            TokenLocation location,
            ISyntaxB target,
            string memberName,
            IReadOnlyList<ISyntaxB> args,
            IdentifierPath region) {

            this.target = target;
            this.memberName = memberName;
            this.args = args;
            this.enclosingHeap = region;
            this.Location = location;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var target = this.target.CheckTypes(types);
            var args = this.args.Select(x => x.CheckTypes(types)).Prepend(target).ToArray();
            var path = new IdentifierPath();

            // Make sure this method exists
            if (!types.TryGetMethodPath(target.ReturnType, this.memberName).TryGetValue(out path)) {
                // Try to access this function statically (universal call syntax)
                if (target.ReturnType.AsMetaType().TryGetValue(out var meta)
                    && types.TryGetMethodPath(meta.PayloadType, this.memberName).TryGetValue(out path)) {

                    args = args.Skip(1).ToArray();
                }
                else { 
                    throw TypeCheckingErrors.MemberUndefined(this.Location, target.ReturnType, this.memberName);
                }
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

                if (!expected.Equals(actual)) {
                    if (i == 0 && expected.Equals(new VarRefType(actual, true))) {
                        var id = counter++;
                        var info = new VariableInfo(
                            "$member_invoke_temp" + id,
                            expected, 
                            VariableKind.RefVariable, 
                            VariableSource.Local, 
                            id);

                        var block = new BlockSyntaxC(new ISyntaxC[] { 
                            new VarRefSyntaxC(info, args[i]),
                            new VariableAccessdSyntaxC(info, VariableAccessKind.LiteralAccess, info.Type)
                        });

                        args[i] = block;

                        continue;
                    }

                    throw TypeCheckingErrors.UnexpectedType(this.Location, expected, actual);
                }
            }

            return new SingularFunctionInvokeSyntaxC(path, args, this.enclosingHeap.Segments.Last(), func.ReturnType);
        }
    }
}
