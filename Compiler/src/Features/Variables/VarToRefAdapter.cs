using Trophy;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Trophy.Features.Variables {
    public class VarToRefAdapter : ISyntaxC {
        private readonly ISyntaxC target;

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.target.Lifetimes;

        public VarToRefAdapter(ISyntaxC target, TrophyType returnType) {
            this.target = target;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            return this.target.GenerateCode(writer, statWriter);
        }
    }
}
