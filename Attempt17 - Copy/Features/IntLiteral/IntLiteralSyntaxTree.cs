using Attempt17.CodeGeneration;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.IntLiteral {
    public class IntLiteralSyntaxTree : ISyntaxTree {
        public LanguageType ReturnType => IntType.Instance;

        public long Value { get; }

        public ImmutableHashSet<IdentifierPath> CapturedVariables => ImmutableHashSet<IdentifierPath>.Empty;

        public IntLiteralSyntaxTree(long value) {
            this.Value = value;
        }

        public CBlock GenerateCode(CodeGenerator gen) {
            return new CBlock(this.Value.ToString() + "L");
        }

        public Scope ModifyLateralScope(Scope scope) => scope;
    }
}
