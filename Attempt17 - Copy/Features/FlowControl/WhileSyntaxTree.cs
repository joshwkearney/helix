using Attempt17.CodeGeneration;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.FlowControl {
    public class WhileSyntaxTree : ISyntaxTree {
        public LanguageType ReturnType => VoidType.Instance;

        public ImmutableHashSet<IdentifierPath> CapturedVariables => ImmutableHashSet<IdentifierPath>.Empty;

        public ISyntaxTree Condition { get; }

        public ISyntaxTree Body { get; }

        public WhileSyntaxTree(ISyntaxTree cond, ISyntaxTree body) {
            this.Condition = cond;
            this.Body = body;
        }

        public Scope ModifyLateralScope(Scope scope) => scope;

        public CBlock GenerateCode(CodeGenerator gen) {
            var cond = this.Condition.GenerateCode(gen);
            var body = this.Body.GenerateCode(gen);
            var writer = new CWriter();

            writer.Lines(cond.SourceLines);
            writer.Line($"while ({cond.Value}) {{");
            writer.Lines(CWriter.Trim(CWriter.Indent(body.SourceLines)));
            writer.Line("}");
            writer.EmptyLine();

            return writer.ToBlock("0");
        }
    }
}