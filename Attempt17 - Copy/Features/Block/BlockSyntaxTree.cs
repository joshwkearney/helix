using Attempt17.CodeGeneration;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt17.Features.Block {
    public class BlockSyntaxTree : ISyntaxTree {
        public LanguageType ReturnType { get; }

        public ImmutableList<ISyntaxTree> Statements { get; }

        public ImmutableHashSet<IdentifierPath> CapturedVariables { get; }

        public BlockSyntaxTree(ImmutableList<ISyntaxTree> stats) {
            this.Statements = stats;

            if (this.Statements.Any()) {
                this.ReturnType = this.Statements.Last().ReturnType;
                this.CapturedVariables = this.Statements.Last().CapturedVariables;
            }
            else {
                this.ReturnType = VoidType.Instance;
                this.CapturedVariables = ImmutableHashSet<IdentifierPath>.Empty;
            }
        }

        public CBlock GenerateCode(CodeGenerator gen) {
            var codes = this.Statements.Select(x => x.GenerateCode(gen)).ToArray();

            if (codes.Any()) {
                return codes.Aggregate((x, y) => x.Combine(y, (c, v) => v));
            }
            else {
                return new CBlock("0");
            }
        }

        public Scope ModifyLateralScope(Scope scope) => scope;
    }
}