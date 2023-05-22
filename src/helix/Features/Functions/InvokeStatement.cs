using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Functions;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

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
            var targetType = types.ReturnTypes[target];

            // Make sure the target is a function
            if (targetType is not NamedType named || !types.Functions.TryGetValue(named.Path, out var sig)) {
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

                newArgs[i] = this.args[i].CheckTypes(types).ConvertTypeTo(expectedType, types);
            }

            var result = new InvokeSyntax(this.Location, sig, newArgs);
            types.ReturnTypes[result] = sig.ReturnType;

            return result;            
        }
    }

    public record InvokeSyntax : ISyntaxTree {
        private readonly FunctionSignature sig;
        private readonly IReadOnlyList<ISyntaxTree> args;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.args;

        public bool IsPure => false;

        public InvokeSyntax(
            TokenLocation loc,
            FunctionSignature sig,
            IReadOnlyList<ISyntaxTree> args) {

            this.Location = loc;
            this.sig = sig;
            this.args = args;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) => this;

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            foreach (var arg in this.args) {
                arg.AnalyzeFlow(flow);
            }

            // TODO: Fix this
            flow.SyntaxLifetimes[this] = new LifetimeBundle();
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            var args = this.args
                .Select(x => x.GenerateCode(types, writer))
                .Prepend(new CVariableLiteral("_pool"))
                .ToArray();

            var result = new CInvoke() {
                Target = new CVariableLiteral(writer.GetVariableName(this.sig.Path)),
                Arguments = args
            };

            var name = writer.GetVariableName();

            var stat = new CVariableDeclaration() {
                Name = name,
                Type = writer.ConvertType(this.sig.ReturnType),
                Assignment = result
            };

            writer.WriteStatement(stat);

            return new CVariableLiteral(name);
        }
    }
}