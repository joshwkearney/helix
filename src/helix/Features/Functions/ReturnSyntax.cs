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
using Helix.Features.Primitives;

namespace Helix.Parsing {
    public partial class Parser {
        public ISyntaxTree ReturnStatement() {
            var start = this.Advance(TokenKind.ReturnKeyword);

            if (this.Peek(TokenKind.Semicolon)) {
                return new ReturnSyntax(
                    start.Location,
                    new VoidLiteral(start.Location));
            }
            else {
                return new ReturnSyntax(
                    start.Location,
                    this.TopExpression());
            }
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

            if (!this.TryGetCurrentFunction(types, out var sig, out var funcPath)) {
                throw new InvalidOperationException();
            }

            types.ControlFlow.AddEndingContinutation(types.Scope);

            var payload = this.payload
                .CheckTypes(types)
                .ToRValue(types)
                .UnifyTo(sig.ReturnType, types);

            var result = new ReturnSyntax(this.Location, payload, sig);

            types.ControlFlow.AddEndingEdge(types.Scope);

            SyntaxTagBuilder
                .AtFrame(types)
                .BuildFor(result);

            FunctionsHelper.AnalyzeReturnValueFlow(
                this.Location, 
                this.funcSig, 
                payload, 
                types);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        private bool TryGetCurrentFunction(TypeFrame types, out FunctionType func, out IdentifierPath resultPath) {
            var path = types.Scope;

            while (!path.IsEmpty) {
                if (types.TryGetFunction(path, out func)) {
                    resultPath = path;
                    return true;
                }

                path = path.Pop();
            }

            func = default;
            resultPath = default;
            return false;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            if (this.funcSig.ReturnType == PrimitiveType.Void) {
                this.payload.GenerateCode(types, writer);

                writer.WriteStatement(new CReturn());
            }
            else {
                writer.WriteStatement(new CReturn() {
                    Target = this.payload.GenerateCode(types, writer)
                });
            }

            return new CIntLiteral(0);
        }
    }
}
