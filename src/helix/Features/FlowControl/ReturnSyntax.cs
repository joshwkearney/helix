using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Analysis;
using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Parsing {
    public partial class Parser {
        public ISyntaxTree ReturnStatement() {
            var start = this.Advance(TokenKind.ReturnKeyword);
            var arg = this.TopExpression();

            return new ReturnSyntax(
                start.Location,
                arg, 
                this.funcPath.Peek());
        }
    }
}

namespace Helix.Features.FlowControl {
    public record ReturnSyntax : ISyntaxTree {
        private readonly ISyntaxTree payload;
        private readonly IdentifierPath func;
        private readonly bool isTypeChecked = false;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.payload };

        public bool IsPure => false;

        public ReturnSyntax(TokenLocation loc, ISyntaxTree payload, 
            IdentifierPath func, bool isTypeChecked = false) {

            this.Location = loc;
            this.payload = payload;
            this.func = func;
            this.isTypeChecked = isTypeChecked;
        }

        public ISyntaxTree ToRValue(SyntaxFrame frame) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var sig = types.Functions[this.func];
            var payload = this.payload.CheckTypes(types).ToRValue(types);
            var result = new ReturnSyntax(this.Location, payload, this.func, true);

            types.ReturnTypes[result] = PrimitiveType.Void;
            types.Lifetimes[result] = Array.Empty<Lifetime>();

            // Add a dependency for every lifetime in the result on the heap
            foreach (var time in types.Lifetimes[result]) {
                var heapLifetime = new Lifetime(new IdentifierPath("$heap"), 0, true);

                types.LifetimeGraph.AddDerived(time, heapLifetime);
            }

            return result;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            writer.WriteStatement(new CReturn() {
                Target = this.payload.GenerateCode(types, writer)
            });

            return new CIntLiteral(0);
        }
    }
}
