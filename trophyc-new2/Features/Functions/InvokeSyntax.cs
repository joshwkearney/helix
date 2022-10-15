using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy;
using Trophy.Analysis;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Functions;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing
{
    public partial class Parser {
        private IParseTree InvokeExpression(IParseTree first) {
            this.Advance(TokenKind.OpenParenthesis);

            var args = new List<IParseTree>();

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

namespace Trophy.Features.Functions
{
    public class InvokeParseTree : IParseTree {
        private readonly IParseTree target;
        private readonly IReadOnlyList<IParseTree> args;

        public TokenLocation Location { get; }

        public InvokeParseTree(TokenLocation loc, IParseTree target, IReadOnlyList<IParseTree> args) {
            this.Location = loc;
            this.target = target;
            this.args = args;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            var target = this.target.ResolveTypes(scope, names, types);

            // Make sure the target is a function
            if (target.ReturnType is not FunctionType funcType) {
                throw TypeCheckingErrors.ExpectedFunctionType(this.target.Location, target.ReturnType);
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
                var expected = funcType.Signature.Parameters[i].Type;
                var arg = this.args[i].ResolveTypes(scope, names, types);

                if (arg.TryUnifyTo(expected).TryGetValue(out var newArg)) {
                    newArgs[i] = newArg;
                }
                else { 
                    throw TypeCheckingErrors.UnexpectedType(this.Location, expected, arg.ReturnType);
                }
            }

            return new InvokeSyntax(funcType.Signature, newArgs);
        }
    }

    public class InvokeSyntax : ISyntaxTree {
        private readonly FunctionSignature sig;
        private readonly IReadOnlyList<ISyntaxTree> args;

        public TrophyType ReturnType => this.sig.ReturnType;

        public InvokeSyntax(
            FunctionSignature sig,
            IReadOnlyList<ISyntaxTree> args) {

            this.sig = sig;
            this.args = args;
        }

        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter) {
            var args = this.args
                .Select(x => x.GenerateCode(writer, statWriter))
                .ToArray();

            var type = writer.ConvertType(this.ReturnType);
            var target = CExpression.VariableLiteral(this.sig.Path.ToCName());
            var invoke = CExpression.Invoke(target, args);

            return statWriter.WriteImpureExpression(type, invoke);
        }
    }
}