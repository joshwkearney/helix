using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.Functions;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

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

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var target = this.target.CheckTypes(types);
            var targetType = types.ReturnTypes[target];

            // Make sure the target is a function
            if (targetType is not NamedType named || !types.Functions.TryGetValue(named.Path, out var sig)) {
                throw TypeCheckingErrors.ExpectedFunctionType(this.target.Location, targetType);
            }

            // Make sure the arg count lines up
            if (this.args.Count != sig.Parameters.Count) {
                throw TypeCheckingErrors.ParameterCountMismatch(
                    this.Location, 
                    sig.Parameters.Count, 
                    this.args.Count);
            }

            var newArgs = new ISyntaxTree[this.args.Count];

            // Make sure the arg types line up
            for (int i = 0; i < this.args.Count; i++) {
                var expectedType = sig.Parameters[i].Type;

                newArgs[i] = this.args[i].CheckTypes(types).UnifyTo(expectedType, types);
            }

            var result = new InvokeSyntax(this.Location, sig, newArgs);
            types.ReturnTypes[result] = sig.ReturnType;

            return result;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
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

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

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