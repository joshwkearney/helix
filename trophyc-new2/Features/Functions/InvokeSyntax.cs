using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.Functions;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntax InvokeExpression(ISyntax first) {
            this.Advance(TokenKind.OpenParenthesis);

            var args = new List<ISyntax>();

            while (!this.Peek(TokenKind.CloseParenthesis)) {
                args.Add(this.TopExpression());

                if (!this.TryAdvance(TokenKind.Comma)) {
                    break;
                }
            }

            var last = this.Advance(TokenKind.CloseParenthesis);
            var loc = first.Location.Span(last.Location);

            return new InvokeParseTree(loc, first, args);
        }
    }
}

namespace Trophy.Features.Functions {
    public record InvokeParseTree : ISyntax {
        private readonly ISyntax target;
        private readonly IReadOnlyList<ISyntax> args;

        public TokenLocation Location { get; }

        public InvokeParseTree(TokenLocation loc, ISyntax target, IReadOnlyList<ISyntax> args) {
            this.Location = loc;
            this.target = target;
            this.args = args;
        }

        public Option<TrophyType> TryInterpret(INamesRecorder names) => Option.None;

        public ISyntax CheckTypes(ITypesRecorder types) {
            var target = this.target.CheckTypes(types);
            var targetType = types.GetReturnType(target);

            // Make sure the target is a function
            if (targetType is not FunctionType funcType) {
                throw TypeCheckingErrors.ExpectedFunctionType(this.target.Location, targetType);
            }

            // Make sure the arg count lines up
            if (this.args.Count != funcType.Signature.Parameters.Count) {
                throw TypeCheckingErrors.ParameterCountMismatch(
                    this.Location, 
                    funcType.Signature.Parameters.Count, 
                    this.args.Count);
            }

            var newArgs = new ISyntax[this.args.Count];

            // Make sure the arg types line up
            for (int i = 0; i < this.args.Count; i++) {
                var expectedType = funcType.Signature.Parameters[i].Type;

                newArgs[i] = this.args[i].CheckTypes(types).UnifyTo(expectedType, types);
            }

            var result = new InvokeSyntax(this.Location, funcType.Signature, newArgs);
            types.SetReturnType(result, funcType.Signature.ReturnType);

            return result;
        }

        public ISyntax ToRValue(ITypesRecorder types) {
            throw new InvalidOperationException();
        }

        public ISyntax ToLValue(ITypesRecorder types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record InvokeSyntax : ISyntax {
        private readonly FunctionSignature sig;
        private readonly IReadOnlyList<ISyntax> args;

        public TokenLocation Location { get; }

        public InvokeSyntax(
            TokenLocation loc,
            FunctionSignature sig,
            IReadOnlyList<ISyntax> args) {

            this.Location = loc;
            this.sig = sig;
            this.args = args;
        }

        public Option<TrophyType> TryInterpret(INamesRecorder names) => Option.None;

        public ISyntax CheckTypes(ITypesRecorder types) => this;

        public ISyntax ToRValue(ITypesRecorder types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            var args = this.args
                .Select(x => x.GenerateCode(writer))
                .ToArray();

            var invoke = new CInvoke() {
                Target = new CVariableLiteral(writer.GetVariableName(this.sig.Path)),
                Arguments = args
            };

            var type = writer.ConvertType(this.sig.ReturnType);

            return writer.WriteImpureExpression(type, invoke);
        }
    }
}