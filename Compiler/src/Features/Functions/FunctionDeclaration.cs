using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Functions {
    public class FunctionDeclarationA : IDeclarationA {
        public ISyntaxA Body { get; }

        public ParseFunctionSignature Signature { get; }

        public TokenLocation Location { get; }

        public FunctionDeclarationA(TokenLocation location, ParseFunctionSignature sig, ISyntaxA body) {
            this.Location = location;
            this.Signature = sig;
            this.Body = body;
        }

        public IDeclarationA DeclareNames(INameRecorder names) {
            // Make sure this name isn't taken
            if (names.TryFindName(this.Signature.Name, out _, out _)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.Signature.Name);
            }

            // Declare this function
            names.DeclareGlobalName(names.CurrentScope.Append(this.Signature.Name), NameTarget.Function);

            // Check for duplicate parameter names
            FunctionsHelper.CheckForDuplicateParameters(this.Location, this.Signature.Parameters.Select(x => x.Name));

            return this;
        }

        public IDeclarationB ResolveNames(INameRecorder names) {
            if (!this.Signature.ReturnType.ResolveToType(names).TryGetValue(out var returnType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.Location);
            }

            // Resolve the type names
            var parsOpt = this.Signature.Parameters
                .Select(x => x.Type.ResolveToType(names).Select(y => new FunctionParameter(x.Name, y)))
                .ToArray();

            if (!parsOpt.All(x => x.Any())) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.Location);
            }

            var pars = parsOpt
                .Select(x => x.GetValue())
                .ToImmutableList();

            var sig = new FunctionSignature(this.Signature.Name, returnType, pars);
            var funcPath = names.CurrentScope.Append(sig.Name);
            var body = FunctionsHelper.ResolveBodyNames(names, funcPath, this.Body, pars);

            // Reserve ids for the parameters
            var ids = pars.Select(_ => names.GetNewVariableId()).ToArray();

            return new FunctionDeclarationB(this.Location, funcPath, sig, body, ids);
        }
    }

    public class FunctionDeclarationB : IDeclarationB {
        private readonly ISyntaxB body;
        private readonly IdentifierPath funcPath;
        private readonly IReadOnlyList<int> parIds;

        public FunctionSignature Signature { get; }

        public TokenLocation Location { get; }

        public FunctionDeclarationB(
            TokenLocation location, 
            IdentifierPath funcPath, 
            FunctionSignature sig, 
            ISyntaxB body, 
            IReadOnlyList<int> parIds) {

            this.Location = location;
            this.funcPath = funcPath;
            this.Signature = sig;
            this.body = body;
            this.parIds = parIds;
        }

        public IDeclarationB DeclareTypes(ITypeRecorder types) {
            types.DeclareFunction(this.funcPath, this.Signature);

            return this;
        }

        public IDeclarationC ResolveTypes(ITypeRecorder types) {
            // Declare the parameters
            FunctionsHelper.DeclareParameters(types, this.funcPath, this.Signature.Parameters, this.parIds);

            // Type check the body
            var body = this.body.CheckTypes(types);

            // Make sure the return types line up
            if (types.TryUnifyTo(body, this.Signature.ReturnType).TryGetValue(out var newbody)) {
                body = newbody;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(
                    this.body.Location,
                    this.Signature.ReturnType, 
                    body.ReturnType);
            }

            // The return value must be allocated on the heap or be one of the arguments
            FunctionsHelper.CheckForInvalidReturnScope(this.body.Location, body);

            return new FunctionDeclarationC(this.Signature, this.funcPath, body, this.parIds);
        }
    }

    public class FunctionDeclarationC : IDeclarationC {
        public readonly FunctionSignature sig;
        private readonly IdentifierPath funcPath;
        private readonly ISyntaxC body;
        private readonly IReadOnlyList<int> parIds;

        public FunctionDeclarationC(FunctionSignature sig, IdentifierPath funcPath, ISyntaxC body, IReadOnlyList<int> parIds) {
            this.sig = sig;
            this.funcPath = funcPath;
            this.body = body;
            this.parIds = parIds;
        }

        public void GenerateCode(ICWriter declWriter) {
            declWriter.RequireRegions();

            var returnType = declWriter.ConvertType(this.sig.ReturnType);
            var pars = this.sig
                .Parameters
                .Select((x, i) => new CParameter(declWriter.ConvertType(x.Type), "$" + x.Name + this.parIds[i]))
                .Prepend(new CParameter(CType.VoidPointer, "env"))
                .ToArray();

            var statWriter = new CStatementWriter();
            var stats = new List<CStatement>();
            statWriter.StatementWritten += (s, e) => stats.Add(e);

            // Unpack the heap
            statWriter.WriteStatement(
                CStatement.VariableDeclaration(
                    CType.NamedType("Region*"), 
                    "heap", 
                    CExpression.VariableLiteral("env")));
            statWriter.WriteStatement(CStatement.NewLine());

            var retExpr = this.body.GenerateCode(declWriter, statWriter);
            stats.Add(CStatement.Return(retExpr));

            var decl = CDeclaration.Function(returnType, "$" + this.funcPath, false, pars, stats);
            var forwardDecl = CDeclaration.FunctionPrototype(returnType, "$" + this.funcPath, false, pars);

            declWriter.WriteDeclaration(decl);
            declWriter.WriteDeclaration(CDeclaration.EmptyLine());

            declWriter.WriteForwardDeclaration(forwardDecl);
            declWriter.WriteForwardDeclaration(CDeclaration.EmptyLine());
        }
    }
}