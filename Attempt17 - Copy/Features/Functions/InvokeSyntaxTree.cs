using Attempt17.CodeGeneration;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt17.Features.Functions {
    public class InvokeSyntaxTree : ISyntaxTree {
        public LanguageType ReturnType => this.Target.Signature.ReturnType;

        public ImmutableHashSet<IdentifierPath> CapturedVariables { get; }

        public FunctionInfo Target { get; }

        public ImmutableList<ISyntaxTree> Arguments { get; }

        public InvokeSyntaxTree(FunctionInfo info, ImmutableList<ISyntaxTree> args) {
            this.Target = info;
            this.Arguments = args;
            this.CapturedVariables = args.Aggregate(
                ImmutableHashSet<IdentifierPath>.Empty,
                (x, y) => x.Union(y.CapturedVariables));
        }

        public CBlock GenerateCode(CodeGenerator gen) {
            var args = this.Arguments.Select(x => x.GenerateCode(gen)).ToArray();
            var targetName = this.Target.Path.ToCName();
            var tempName = gen.GetTempVariableName();
            var tempType = this.ReturnType.GenerateCType();
            var writer = new CWriter();
            var invoke = targetName + "(";

            foreach (var arg in args) {
                invoke += arg.Value + ", ";
                writer.Lines(arg.SourceLines);
            }

            invoke = invoke.TrimEnd(',', ' ');
            invoke += ")";

            writer.VariableInit(tempType, tempName, invoke);

            return writer.ToBlock(tempName);
        }

        public Scope ModifyLateralScope(Scope scope) => scope;
    }
}