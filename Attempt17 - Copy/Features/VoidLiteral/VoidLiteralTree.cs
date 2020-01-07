using Attempt17.CodeGeneration;
using Attempt17.Parsing;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.VoidLiteral {
    public class VoidLiteralTree : IParseTree, ISyntaxTree {
        public TokenLocation Location { get; }

        public LanguageType ReturnType => VoidType.Instance;

        public ImmutableHashSet<IdentifierPath> CapturedVariables => ImmutableHashSet<IdentifierPath>.Empty;

        public VoidLiteralTree(TokenLocation loc) {
            this.Location = loc;
        }

        public ISyntaxTree Analyze(Scope scope) => this;

        public Scope ModifyLateralScope(Scope scope) => scope;

        public CBlock GenerateCode(CodeGenerator gen) => new CBlock("0");
    }
}