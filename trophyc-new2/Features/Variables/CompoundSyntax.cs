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
    public class CompoundSyntax : ISyntax {
        private readonly IReadOnlyList<ISyntax> args;

        public TokenLocation Location { get; }

        public CompoundSyntax(TokenLocation loc, IReadOnlyList<ISyntax> args) {
            this.Location = loc;
            this.args = args;
        }

        public ISyntax CheckTypes(ITypesRecorder types) {
            var result = new CompoundSyntax(
                this.Location, 
                this.args.Select(x => x.CheckTypes(types)).ToArray());

            types.SetReturnType(result, PrimitiveType.Void);
            return result;
        }

        public ISyntax ToRValue(ITypesRecorder types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            foreach (var arg in this.args) {
                arg.GenerateCode(writer);
            }

            return new CIntLiteral(0);
        }
    }
}
