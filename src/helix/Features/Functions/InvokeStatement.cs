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

                newArgs[i] = this.args[i].CheckTypes(types).UnifyTo(expectedType, types);
            }

            var path = this.Location.Scope.Append("$invoke_temp_" + tempCounter++);
            var result = new InvokeSyntax(this.Location, sig, newArgs, path);

            result.SetReturnType(sig.ReturnType, types);
            result.SetCapturedVariables(newArgs.Append(target), types);

            return result;            
        }
    }

    public record InvokeSyntax : ISyntaxTree {
        private readonly FunctionSignature sig;
        private readonly IReadOnlyList<ISyntaxTree> args;
        private readonly IdentifierPath invokeTempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.args;

        public bool IsPure => false;

        public InvokeSyntax(
            TokenLocation loc,
            FunctionSignature sig,
            IReadOnlyList<ISyntaxTree> args,
            IdentifierPath tempPath) {

            this.Location = loc;
            this.sig = sig;
            this.args = args;
            this.invokeTempPath = tempPath;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) => this;

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            foreach (var arg in this.args) {
                arg.AnalyzeFlow(flow);
            }

            // Things to do:
            // 1) Figure out possible reference type aliasing through arguments
            // 1) Figure out possible pointer aliasing through arguments
            // 2) Figure out possible argument dependencies for the return value
            // 3) Create new inferenced return value lifetime

            var invokeLifetime = new InferredLocationLifetime(
                this.Location,
                this.invokeTempPath.ToVariablePath(),
                flow.LifetimeRoots,
                LifetimeOrigin.TempValue);

            var dict = new Dictionary<IdentifierPath, LifetimeBounds>();

            foreach (var (relPath, memType) in this.sig.ReturnType.GetMembers(flow)) {
                var memBounds = new LifetimeBounds(invokeLifetime);

                dict.Add(relPath, memBounds);
            }

            flow.SyntaxLifetimes[this] = new LifetimeBundle(dict);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            var region = this
                .GetLifetimes(types)[new IdentifierPath()]
                .ValueLifetime
                .GenerateCode(types, writer);

            var args = this.args
                .Select(x => x.GenerateCode(types, writer))
                .Prepend(region)
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

            writer.WriteComment($"Line {this.Location.Line}: Function call");
            writer.WriteStatement(stat);

            return new CVariableLiteral(name);
        }
    }
}