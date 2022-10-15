using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis.Unification;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;

namespace Trophy.Analysis.SyntaxTree {
    public interface ISyntaxTree {
        public TrophyType ReturnType { get; }

        public Option<ISyntaxTree> TryUnifyTo(TrophyType type) => TypeUnifier.TryUnify(this, type);

        public Option<ISyntaxTree> ToLValue() => Option.None;

        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter);
    }
}