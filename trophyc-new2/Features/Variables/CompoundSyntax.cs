using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.Syntax;
using Trophy.Parsing;

namespace Trophy.Features.Variables {
    public class CompoundSyntax : ISyntaxTree {
        private readonly IReadOnlyList<ISyntaxTree> args;

        public TokenLocation Location { get; }

        public CompoundSyntax(TokenLocation loc, IReadOnlyList<ISyntaxTree> args) {
            this.Location = loc;
            this.args = args;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var result = new CompoundSyntax(
                this.Location, 
                this.args.Select(x => x.CheckTypes(types)).ToArray());

            types.ReturnTypes[result] = PrimitiveType.Void;
            return result;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            foreach (var arg in this.args) {
                arg.GenerateCode(writer);
            }

            return new CIntLiteral(0);
        }
    }
}
