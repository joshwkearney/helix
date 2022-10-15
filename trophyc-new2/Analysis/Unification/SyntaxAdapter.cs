using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;

namespace Trophy.Analysis.Unification {
    public class SyntaxAdapter : ISyntaxTree {
        public ISyntaxTree OriginalSyntax { get; }

        public ISyntaxTree AdaptedSyntax { get; }

        public TrophyType ReturnType => this.AdaptedSyntax.ReturnType;
        
        public SyntaxAdapter(ISyntaxTree original, ISyntaxTree adapted) {
            this.OriginalSyntax = original;
            this.AdaptedSyntax = adapted;
        }

        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter) {
            this.OriginalSyntax.GenerateCode(writer, statWriter);

            return this.AdaptedSyntax.GenerateCode(writer, statWriter);
        }
    }
}
