using Attempt17.CodeGeneration;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.Functions {
    public class FunctionLiteralSyntaxTree : ISyntaxTree {
        public LanguageType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> CapturedVariables => ImmutableHashSet<IdentifierPath>.Empty;

        public FunctionLiteralSyntaxTree(IdentifierPath path) {
            this.ReturnType = new NamedType(path);
        }

        public CBlock GenerateCode(CodeGenerator gen) {
            return new CBlock("0");
        }

        public Scope ModifyLateralScope(Scope scope) => scope;
    }
}