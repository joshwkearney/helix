using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Features.Functions;
using Helix.Features.Types;

namespace Helix.Parsing {
    public partial class Parser {
        public ISyntaxTree ReturnStatement() {
            var start = this.Advance(TokenKind.ReturnKeyword);
            var arg = this.TopExpression();

            return new ReturnSyntax(
                start.Location,
                arg);
        }
    }
}

namespace Helix.Features.Functions {
    public record ReturnSyntax : ISyntaxTree {
        private readonly ISyntaxTree payload;
        private readonly FunctionType funcSig;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.payload };

        public bool IsPure => false;

        public ReturnSyntax(TokenLocation loc, ISyntaxTree payload, 
                            FunctionType func = null) {

            this.Location = loc;
            this.payload = payload;
            this.funcSig = func;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var payload = this.payload.CheckTypes(types).ToRValue(types);

            if (!this.TryGetCurrentFunction(types, out var sig)) {
                throw new InvalidOperationException();
            }

            var result = new ReturnSyntax(this.Location, payload, sig);

            result.SetReturnType(PrimitiveType.Void, types);
            result.SetCapturedVariables(types);
            result.SetPredicate(types);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (this.IsFlowAnalyzed(flow)) {
                return;
            }

            flow.SyntaxLifetimes[this] = new LifetimeBundle();
            FunctionsHelper.AnalyzeReturnValueFlow(this.Location, this.funcSig, this.payload, flow);
        }

        private bool TryGetCurrentFunction(TypeFrame types, out FunctionType func) {
            var path = types.Scope;

            while (!path.IsEmpty) {
                if (types.TryGetFunction(path, out func)) {
                    return true;
                }

                path = path.Pop();
            }

            func = null;
            return false;
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            writer.WriteStatement(new CReturn() {
                Target = this.payload.GenerateCode(types, writer)
            });

            return new CIntLiteral(0);
        }
    }
}
