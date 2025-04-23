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
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.Types;

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
            var returnType = new VoidLiteral(end.Location) as ISyntaxTree;

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
    public record FunctionParseDeclaration : IDeclaration {
        private readonly FunctionParseSignature signature;
        private readonly ISyntaxTree body;

        public TokenLocation Location { get; }

        public FunctionParseDeclaration(TokenLocation loc, FunctionParseSignature sig, ISyntaxTree body) {

            this.Location = loc;
            this.signature = sig;
            this.body = body;
        }

        public void DeclareNames(TypeFrame types) {
            FunctionsHelper.CheckForDuplicateParameters(
                this.Location, 
                this.signature.Parameters.Select(x => x.Name));

            FunctionsHelper.DeclareName(this.signature, types);
        }

        public void DeclareTypes(TypeFrame types) {
            var path = types.Scope.Append(this.signature.Name);
            var sig = this.signature.ResolveNames(types);
            var named = new NominalType(path, NominalTypeKind.Function);

            types.Locals = types.Locals.SetItem(path, new LocalInfo(named));

            // Declare this function
            types.NominalSignatures.Add(path, sig);
        }
        
        public IDeclaration CheckTypes(TypeFrame types) {
            var path = types.ResolvePath(types.Scope, this.signature.Name);
            var sig = new NominalType(path, NominalTypeKind.Function).AsFunction(types).GetValue();

            // Set the scope for type checking the body
            types = new TypeFrame(types, this.signature.Name);

            // Declare parameters
            FunctionsHelper.DeclareParameters(sig, path, types);
            
            // Check types
            var body = this.body;

            if (sig.ReturnType == PrimitiveType.Void) {
                body = new BlockSyntax(this.body.Location, new ISyntaxTree[] { 
                    this.body,
                    new VoidLiteral(this.body.Location)
                });
            }

            body = body.CheckTypes(types)
                .ToRValue(types)
                .UnifyTo(sig.ReturnType, types);
            
#if DEBUG
            // Debug check: make sure that every syntax tree was type checked
            foreach (var expr in body.GetAllChildren()) {
                if (!expr.IsTypeChecked(types)) {
                    throw new Exception("Compiler assertion failed: syntax tree does not have a return type");
                }
            }
#endif

            return new FunctionDeclaration(this.Location, path, sig, body);
        }
    }

    public record FunctionDeclaration : IDeclaration {
        private readonly ISyntaxTree body;

        public FunctionType Signature { get; }

        public TokenLocation Location { get; }

        public IdentifierPath Path { get; }

        public FunctionDeclaration(TokenLocation loc, IdentifierPath path, FunctionType sig, ISyntaxTree body) {
            this.Location = loc;
            this.Signature = sig;
            this.body = body;
            this.Path = path;
        }

        public void DeclareNames(TypeFrame names) {
            throw new InvalidOperationException();
        }

        public void DeclareTypes(TypeFrame paths) {
            throw new InvalidOperationException();
        }

        public IDeclaration CheckTypes(TypeFrame types) {
            throw new InvalidOperationException();
        }

        public void GenerateCode(TypeFrame types, ICWriter writer) {
            writer.ResetTempNames();

            var returnType = this.Signature.ReturnType == PrimitiveType.Void
                ? new CNamedType("void")
                : writer.ConvertType(this.Signature.ReturnType, types);

            var pars = this.Signature
                .Parameters
                .Select((x, i) => new CParameter() { 
                    Type = writer.ConvertType(x.Type, types),
                    Name = writer.GetVariableName(this.Path.Append(x.Name))
                })
                .Prepend(new CParameter() {
                    Name = "_return_region",
                    Type = new CNamedType("_Region*")
                })
                .ToArray();

            var funcName = writer.GetVariableName(this.Path);
            var body = new List<ICStatement>();
            var bodyWriter = new CStatementWriter(writer, body);

            // Register the parameters as local variables
            foreach (var par in this.Signature.Parameters) {
                foreach (var (relPath, _) in par.Type.GetMembers(types)) {
                    var path = this.Path.Append(par.Name).Append(relPath);

                    bodyWriter.VariableKinds[path] = CVariableKind.Local;
                }
            }

            // Generate the body
            var retExpr = this.body.GenerateCode(types, bodyWriter);

            if (this.Signature.ReturnType != PrimitiveType.Void) {
                bodyWriter.WriteEmptyLine();
                bodyWriter.WriteStatement(new CReturn() { 
                    Target = retExpr
                });
            }

            // If the body ends with an empty line, trim it
            if (body.Any() && body.Last().IsEmpty) {
                body = body.SkipLast(1).ToList();
            }

            var decl = new CFunctionDeclaration() {
                ReturnType = returnType,
                Name = funcName,
                Parameters = pars,
                Body = body
            };

            var forwardDecl = new CFunctionDeclaration() {
                ReturnType = returnType,
                Name = funcName,
                Parameters = pars
            };

            writer.WriteDeclaration2(forwardDecl);

            writer.WriteDeclaration4(decl);
            writer.WriteDeclaration4(new CEmptyLine());
        }
    }
}
