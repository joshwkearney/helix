using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Functions;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.Types;

namespace Helix.Parsing {
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

            return new InvokeParseSyntax(loc, first, args);
        }
    }
}

namespace Helix.Features.Functions {
    public record InvokeParseSyntax : ISyntaxTree {
        private static int tempCounter = 0;

        private readonly ISyntaxTree target;
        private readonly IReadOnlyList<ISyntaxTree> args;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.args.Prepend(this.target);

        public bool IsPure => false;

        public InvokeParseSyntax(TokenLocation loc, ISyntaxTree target, 
            IReadOnlyList<ISyntaxTree> args) {

            this.Location = loc;
            this.target = target;
            this.args = args;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            var target = this.target.CheckTypes(types).ToRValue(types);
            var targetType = target.GetReturnType(types);

            // TODO: Support invoking non-nominal functions
            // Make sure the target is a function
            if (!targetType.AsFunction(types).TryGetValue(out var sig) || targetType is not NominalType named) {
                throw TypeException.ExpectedFunctionType(this.target.Location, targetType);
            }

            // Make sure the arg count lines up
            if (this.args.Count != sig.Parameters.Count) {
                throw TypeException.ParameterCountMismatch(
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

            var path = types.Scope.Append("$call" + tempCounter++);
            var result = new InvokeSyntax(this.Location, sig, newArgs, named.Path, path);

            SyntaxTagBuilder.AtFrame(types)
                .WithChildren(newArgs.Append(target))
                .WithReturnType(sig.ReturnType)
                .BuildFor(result);

            return result;            
        }
    }

    public record InvokeSyntax : ISyntaxTree {
        private readonly FunctionType sig;
        private readonly IReadOnlyList<ISyntaxTree> args;
        private readonly IdentifierPath funcPath;
        private readonly IdentifierPath invokeTempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.args;

        public bool IsPure => false;

        public InvokeSyntax(
            TokenLocation loc,
            FunctionType sig,
            IReadOnlyList<ISyntaxTree> args,
            IdentifierPath path,
            IdentifierPath tempPath) {

            this.Location = loc;
            this.sig = sig;
            this.args = args;
            this.funcPath = path;
            this.invokeTempPath = tempPath;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) => this;

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var args = this.args
                .Select(x => x.GenerateCode(types, writer))
                .ToArray();

            var result = new CInvoke() {
                Target = new CVariableLiteral(writer.GetVariableName(this.funcPath)),
                Arguments = args
            };

            var name = writer.GetVariableName();

            var stat = new CVariableDeclaration() {
                Name = name,
                Type = writer.ConvertType(this.sig.ReturnType, types),
                Assignment = result
            };

            writer.WriteComment($"Line {this.Location.Line}: Function call");
            writer.WriteStatement(stat);

            return new CVariableLiteral(name);
        }
    }
}