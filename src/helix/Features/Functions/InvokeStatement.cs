using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Functions;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Primitives;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree InvokeExpression(ISyntaxTree first, BlockBuilder block) {
            this.Advance(TokenKind.OpenParenthesis);

            var args = new List<ISyntaxTree>();

            while (!this.Peek(TokenKind.CloseParenthesis)) {
                args.Add(this.TopExpression(block));

                if (!this.TryAdvance(TokenKind.Comma)) {
                    break;
                }
            }

            var last = this.Advance(TokenKind.CloseParenthesis);
            var loc = first.Location.Span(last.Location);

            var tempName = block.GetTempName();
            var tempPath = this.scope.Append(tempName);
            var temp = new InvokeParseStatement(loc, first, tempPath, args);

            block.Statements.Add(temp);

            return new VariableAccessParseSyntax(loc, tempName);
        }
    }
}

namespace Helix.Features.Functions {
    public record InvokeParseStatement : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly IReadOnlyList<ISyntaxTree> args;
        private readonly IdentifierPath resultPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.args.Prepend(this.target);

        public bool IsPure => false;

        public InvokeParseStatement(TokenLocation loc, ISyntaxTree target, 
            IdentifierPath resultPath,
            IReadOnlyList<ISyntaxTree> args) {

            this.Location = loc;
            this.target = target;
            this.args = args;
            this.resultPath = resultPath;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var target = this.target.CheckTypes(types).ToRValue(types);
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

            var captured = new List<IdentifierPath>();

            // If there are any reference types in the result that can be found
            // in any of the arguments then assume we captured that argument.
            // Note: Pointer and array types are normalized to writable in case
            // somebody is casting away their readonly-ness
            if (!sig.ReturnType.IsValueType(types)) {
                var retRefs = sig.ReturnType
                    .GetContainedTypes(types)
                    .Where(x => !x.IsValueType(types))
                    .Select(NormalizeTypes)
                    .ToArray();

                foreach (var arg in newArgs) {
                    bool overlap = types.ReturnTypes[arg]
                        .GetContainedTypes(types)
                        .Where(x => !x.IsValueType(types))
                        .Select(NormalizeTypes)
                        .Intersect(retRefs)
                        .Any();

                    if (overlap) {
                        captured.AddRange(types.CapturedVariables[arg]);
                    }
                }
            }

            // TODO: Introduce a new captured variable if the function being called
            // is "pooling". This is because the new captured variable will have a 
            // lifetime computed at runtime based on where the function actually allocates
            // its return value. This new lifetime will be taken from the context struct
            // passed to the function

            // Declare a variable for the result
            var resultSig = new VariableSignature(
                this.resultPath, 
                sig.ReturnType, 
                false, 
                captured);

            types.Variables[this.resultPath] = resultSig;
            types.SyntaxValues[this.resultPath] = new DummySyntax(this.Location);

            var result = new InvokeStatement(this.Location, sig, this.resultPath, newArgs);

            types.ReturnTypes[result] = PrimitiveType.Void;
            types.CapturedVariables[result] = Array.Empty<IdentifierPath>();

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

        private static HelixType NormalizeTypes(HelixType type) {
            if (type is PointerType ptr) {
                return new PointerType(ptr.InnerType, true);
            }
            else if (type is ArrayType arr) {
                return new ArrayType(arr.InnerType);
            }
            else {
                return type;
            }
        }
    }

    public record InvokeStatement : ISyntaxTree {
        private readonly FunctionSignature sig;
        private readonly IReadOnlyList<ISyntaxTree> args;
        private readonly IdentifierPath resultPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => args;

        public bool IsPure => false;

        public InvokeStatement(
            TokenLocation loc,
            FunctionSignature sig,
            IdentifierPath resultPath,
            IReadOnlyList<ISyntaxTree> args) {

            this.Location = loc;
            this.sig = sig;
            this.args = args;
            this.resultPath = resultPath;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            var args = this.args
                .Select(x => x.GenerateCode(writer))
                .ToArray();

            var result = new CInvoke() {
                Target = new CVariableLiteral(writer.GetVariableName(this.sig.Path)),
                Arguments = args
            };

            var stat = new CVariableDeclaration() {
                Name = writer.GetVariableName(this.resultPath),
                Type = writer.ConvertType(this.sig.ReturnType),
                Assignment = result
            };

            writer.WriteStatement(stat);

            return new CIntLiteral(0);
        }
    }
}