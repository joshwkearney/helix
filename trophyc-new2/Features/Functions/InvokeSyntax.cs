using Trophy.Analysis;
using Trophy.Analysis.Unification;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Functions;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree InvokeExpression(ISyntaxTree first) {
            this.Advance(TokenKind.OpenParenthesis);

            var args = new List<ISyntaxTree>();

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
    public record InvokeParseTree : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly IReadOnlyList<ISyntaxTree> args;

        public TokenLocation Location { get; }

        public InvokeParseTree(TokenLocation loc, ISyntaxTree target, IReadOnlyList<ISyntaxTree> args) {
            this.Location = loc;
            this.target = target;
            this.args = args;
        }

        public Option<TrophyType> ToType(INamesRecorder names) => Option.None;

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
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

            var newArgs = new ISyntaxTree[this.args.Count];

            // Make sure the arg types line up
            for (int i = 0; i < this.args.Count; i++) {
                var expectedType = funcType.Signature.Parameters[i].Type;
                var arg = this.args[i].CheckTypes(types);
                var argType = types.GetReturnType(arg);

                if (types.TryUnifyTo(arg, argType, expectedType).TryGetValue(out var newArg)) {
                    newArgs[i] = newArg;
                }
                else { 
                    throw TypeCheckingErrors.UnexpectedType(this.Location, expectedType, argType);
                }
            }

            var result = new InvokeSyntax(this.Location, funcType.Signature, newArgs);
            types.SetReturnType(result, funcType.Signature.ReturnType);

            return result;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => Option.None;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(CStatementWriter statWriter) {
            throw new InvalidOperationException();
        }
    }

    public record InvokeSyntax : ISyntaxTree {
        private readonly FunctionSignature sig;
        private readonly IReadOnlyList<ISyntaxTree> args;

        public TokenLocation Location { get; }

        public InvokeSyntax(
            TokenLocation loc,
            FunctionSignature sig,
            IReadOnlyList<ISyntaxTree> args) {

            this.Location = loc;
            this.sig = sig;
            this.args = args;
        }

        public Option<TrophyType> ToType(INamesRecorder names) => Option.None;

        public ISyntaxTree CheckTypes(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(CStatementWriter writer) {
            var args = this.args
                .Select(x => x.GenerateCode(writer))
                .ToArray();

            var type = writer.ConvertType(this.sig.ReturnType);
            var target = CExpression.VariableLiteral(this.sig.Path.ToCName());
            var invoke = CExpression.Invoke(target, args);

            return writer.WriteImpureExpression(type, invoke);
        }
    }
}