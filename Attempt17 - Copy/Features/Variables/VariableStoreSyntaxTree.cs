using Attempt17.CodeGeneration;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.Variables {
    public class VariableStoreSyntaxTree : ISyntaxTree {
        public LanguageType ReturnType { get; }

        public ISyntaxTree Target { get; }

        public ISyntaxTree Value { get; }

        public ImmutableHashSet<IdentifierPath> CapturedVariables { get; }

        public VariableStoreSyntaxTree(ISyntaxTree target, ISyntaxTree value) {
            this.ReturnType = VoidType.Instance;
            this.CapturedVariables = ImmutableHashSet<IdentifierPath>.Empty;
            this.Target = target;
            this.Value = value;
        }

        public Scope ModifyLateralScope(Scope scope) => scope;

        public CBlock GenerateCode(CodeGenerator gen) {
            var writer = new CWriter();
            var target = this.Target.GenerateCode(gen);
            var value = this.Value.GenerateCode(gen);

            writer.Lines(value.SourceLines);
            writer.Lines(target.SourceLines);
            writer.VariableAssignment(CWriter.Dereference(target.Value), value.Value);

            return writer.ToBlock("0");
        }
    }
}