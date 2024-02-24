using System.Collections.Immutable;
using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.CSyntax;
using Helix.Features.FlowControl;
using Helix.Features.Functions;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Variables;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Collections;
using Helix.Features.Types;
using Helix.Analysis.Predicates;
using Helix.HelixMinusMinus;

namespace Helix.Parsing {
    public partial class Parser {
        private FunctionParseSignature FunctionSignature() {
            var start = this.Advance(TokenKind.FunctionKeyword);
            var funcName = this.Advance(TokenKind.Identifier).Value;

            this.Advance(TokenKind.OpenParenthesis);

            var pars = ImmutableList<ParseFunctionParameter>.Empty;
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                var parStart = this.Advance(TokenKind.VarKeyword);
                var parName = this.Advance(TokenKind.Identifier).Value;
                this.Advance(TokenKind.AsKeyword);

                var parType = this.TopExpression();
                var parLoc = parStart.Location.Span(parType.Location);

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars = pars.Add(new ParseFunctionParameter(parLoc, parName, parType));
            }

            var end = this.Advance(TokenKind.CloseParenthesis);
            var returnType = new VoidLiteral(end.Location) as IParseTree;

            if (this.TryAdvance(TokenKind.AsKeyword)) {
                returnType = this.TopExpression();
            }

            var loc = start.Location.Span(returnType.Location);
            var sig = new FunctionParseSignature(loc, funcName, returnType, pars);

            return sig;
        }

        private IDeclaration FunctionDeclaration() {
            var sig = this.FunctionSignature();

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            var body = this.TopExpression();           
            this.Advance(TokenKind.Semicolon);

            return new FunctionParseDeclaration(
                sig.Location.Span(body.Location), 
                sig,
                body);
        }
    }
}

namespace Helix.Features.Functions {
    public record FunctionParseDeclaration(
        TokenLocation Location,
        FunctionParseSignature Signature,
        IParseTree Body) : IDeclaration {

        public void DeclareNames(TypeFrame types) {
            FunctionsHelper.CheckForDuplicateParameters(
                this.Location,
                this.Signature.Parameters.Select(x => x.Name));

            FunctionsHelper.DeclareName(this.Signature, types);
        }

        public void DeclareTypes(TypeFrame types) {
            var named = new NominalType(this.Signature.Name, NominalTypeKind.Function);

            this.Signature.ResolveNames(types);
            types.Locals = types.Locals.SetItem(this.Signature.Name, new LocalInfo(named));
        }

        public void GenerateHelixMinusMinus(ImperativeSyntaxWriter writer) {
            var bodyWriter = new ImperativeSyntaxWriter(writer);
            this.Body.ToImperativeSyntax(bodyWriter);

            writer.AddStatement(new ParseFunctionStatement(this.Location, this.Signature, this.Signature.Name, bodyWriter.Statements));
        }
    }


    public record ParseFunctionStatement(
        TokenLocation Location,
        FunctionParseSignature Signature,
        string Name,
        IReadOnlyList<IImperativeStatement> Body) : IImperativeStatement {

        public void CheckTypes(TypeFrame types, ImperativeSyntaxWriter writer) {
            var sig = this.Signature.ResolveNames(types);

            // Set the scope for type checking the body
            var bodyTypes = new TypeFrame(types);

            // Declare parameters
            FunctionsHelper.DeclareParameters(sig, bodyTypes);

            // Check types
            var bodyWriter = new ImperativeSyntaxWriter(writer);

            foreach (var stat in this.Body) {
                stat.CheckTypes(bodyTypes, bodyWriter);
            }

            if (sig.ReturnType != PrimitiveType.Void /*&& !body.AlwaysReturns(types)*/) {
                throw TypeException.NoReturn(this.Location);
            }

            var result = new FunctionStatement(this.Location, sig, this.Signature.Name, bodyWriter.Statements);

            writer.AddStatement(result);
        }

        public string[] Write() {
            var stats = this.Body
                .SelectMany(x => x.Write())
                .Select(x => "    " + x)
                .ToArray();

            return new[] { $"func {this.Name}(...) as ... {{" }
                .Concat(stats)
                .Append("};")
                .ToArray();
        }
    }

    public record FunctionStatement(
        TokenLocation Location,
        FunctionType Signature,
        string Name,
        IReadOnlyList<IImperativeStatement> Body) : IImperativeStatement {
        public void CheckTypes(TypeFrame types, ImperativeSyntaxWriter writer) {
            writer.AddStatement(this);
        }

        public string[] Write() {
            var stats = this.Body
                .SelectMany(x => x.Write())
                .Select(x => "    " + x)
                .ToArray();

            var pars = this.Signature.Parameters
                .Select(x => "var " + x.Name + " as " + x.Type)
                .ToArray();

            return new[] { $"func {this.Name}({string.Join(", ", pars)}) as {this.Signature.ReturnType} {{" }
                .Concat(stats)
                .Append("};")
                .ToArray();
        }
    }
}
 