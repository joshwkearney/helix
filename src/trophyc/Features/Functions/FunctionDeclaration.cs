using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
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

        public IDeclarationA DeclareNames(INamesRecorder names) {
            // Make sure this name isn't taken
            if (names.TryFindName(this.Signature.Name, out _, out _)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.Signature.Name);
            }

            // Declare this function
            var path = names.Context.Scope.Append(this.Signature.Name);
            names.DeclareName(path, NameTarget.Function, IdentifierScope.GlobalName);

            // Check for duplicate parameter names
            FunctionsHelper.CheckForDuplicateParameters(this.Location, this.Signature.Parameters.Select(x => x.Name));

            return this;
        }

        public IDeclarationB ResolveNames(INamesRecorder names) {
            if (!this.Signature.ReturnType.ResolveToType(names).TryGetValue(out var returnType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.Location);
            }

            var pars = FunctionsHelper.CheckParameters(this.Signature.Parameters, names, this.Location);
            var sig = new FunctionSignature(this.Signature.Name, returnType, pars);
            var funcPath = names.Context.Scope.Append(sig.Name);
            var body = FunctionsHelper.ResolveBodyNames(names, funcPath, new IdentifierPath("heap"), this.Body, pars);

            // Reserve ids for the parameters
            var ids = pars.Select(_ => names.GetNewVariableId()).ToArray();

            return new FunctionDeclarationB(this.Location, funcPath, sig, body, ids, names.Context.Region);
        }
    }

    public class FunctionDeclarationB : IDeclarationB {
        private readonly ISyntaxB body;
        private readonly IdentifierPath funcPath;
        private readonly IReadOnlyList<int> parIds;
        private readonly IdentifierPath region;

        public FunctionSignature Signature { get; }

        public TokenLocation Location { get; }

        public FunctionDeclarationB(
            TokenLocation location, 
            IdentifierPath funcPath, 
            FunctionSignature sig, 
            ISyntaxB body, 
            IReadOnlyList<int> parIds,
            IdentifierPath region) {

            this.Location = location;
            this.funcPath = funcPath;
            this.Signature = sig;
            this.body = body;
            this.parIds = parIds;
            this.region = region;
        }

        public IDeclarationB DeclareTypes(ITypesRecorder types) {
            types.DeclareName(this.funcPath, NamePayload.FromFunction(this.Signature));

            return this;
        }

        public IDeclarationC ResolveTypes(ITypesRecorder types) {
            // Declare the parameters
            FunctionsHelper.DeclareParameters(types, this.funcPath, this.Signature.Parameters, this.parIds);

            var context = types.Context.WithContainingFunction(ContainingFunction.FromDeclaration(this.Signature));
            var body = types.WithContext(context, types => this.body.CheckTypes(types));

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
            var bodyRegion = this.region.Append("$args").Append("heap");
            FunctionsHelper.CheckForInvalidReturnScope(this.body.Location, bodyRegion, body);

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
            var unpack = CExpression.VariableLiteral("env");
            unpack = CExpression.Cast(CType.NamedType("Region*"), unpack);

            statWriter.WriteStatement(CStatement.VariableDeclaration(CType.NamedType("Region*"), "heap", unpack));
            statWriter.WriteStatement(CStatement.NewLine());

            var retExpr = this.body.GenerateCode(declWriter, statWriter);

            if (this.sig.ReturnType.IsVoidType) {
                statWriter.WriteStatement(CStatement.FromExpression(retExpr));
            }
            else { 
                stats.Add(CStatement.Return(retExpr));
            }

            CDeclaration decl;
            CDeclaration forwardDecl;

            if (this.sig.ReturnType.IsVoidType) {
                decl = CDeclaration.Function("$" + this.funcPath, false, pars, stats);
                forwardDecl = CDeclaration.FunctionPrototype("$" + this.funcPath, false, pars);
            }
            else {
                decl = CDeclaration.Function(returnType, "$" + this.funcPath, false, pars, stats);
                forwardDecl = CDeclaration.FunctionPrototype(returnType, "$" + this.funcPath, false, pars);
            }            

            declWriter.WriteDeclaration3(decl);
            declWriter.WriteDeclaration3(CDeclaration.EmptyLine());

            declWriter.WriteDeclaration2(forwardDecl);
            declWriter.WriteDeclaration2(CDeclaration.EmptyLine());
        }
    }
}