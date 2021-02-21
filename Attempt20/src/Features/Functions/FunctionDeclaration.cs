using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.CodeGeneration;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Functions {
    public class FunctionDeclarationA : IDeclarationA {
        public ISyntaxA Body { get; }

        public FunctionSignature Signature { get; }

        public TokenLocation Location { get; }

        public FunctionDeclarationA(TokenLocation location, FunctionSignature sig, ISyntaxA body) {
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

            var parNames = this.Signature.Parameters.Select(x => x.Name).ToArray();
            var unique = parNames.Distinct().ToArray();

            var dups = this.Signature
                .Parameters
                .Select(x => x.Name)
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            // Check for duplicate parameter names
            if (dups.Any()) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, dups.First());
            }

            return this;
        }

        public IDeclarationB ResolveNames(INameRecorder names) {
            // Resolve the type names
            var returnType = names.ResolveTypeNames(this.Signature.ReturnType, this.Location);
            var pars = this.Signature
                .Parameters
                .Select(x => new FunctionParameter(x.Name, names.ResolveTypeNames(x.Type, this.Location)))
                .ToImmutableList();

            var sig = new FunctionSignature(this.Signature.Name, returnType, pars);
            var funcPath = names.CurrentScope.Append(sig.Name);

            // Push this function name as the new scope
            names.PushScope(funcPath);
            names.PushRegion(IdentifierPath.StackPath);

            // Declare the parameters
            foreach (var par in sig.Parameters) {
                names.DeclareLocalName(funcPath.Append(par.Name), NameTarget.Variable);
            }

            // Pop the new scope out
            var body = this.Body.CheckNames(names);

            names.PopRegion();
            names.PopScope();

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
            for (int i = 0; i < this.Signature.Parameters.Count; i++) {
                var par = this.Signature.Parameters[i];
                var id = this.parIds[i];

                var path = this.funcPath.Append(par.Name);
                var info = new VariableInfo(
                    par.Name,
                    par.Type,
                    VariableDefinitionKind.Parameter,
                    id,
                    new[] { new IdentifierPath("$args_" + par.Name) }.ToImmutableHashSet(),
                    new[] { new IdentifierPath("$args_" + par.Name) }.ToImmutableHashSet());

                types.DeclareVariable(path, info);
            }

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
            foreach (var capLifetime in body.Lifetimes) {
                if (capLifetime.Segments.Any() && capLifetime.Segments.First().StartsWith("$args_")) {
                    continue;
                }

                if (!capLifetime.Outlives(IdentifierPath.HeapPath)) { 
                    throw TypeCheckingErrors.LifetimeExceeded(this.body.Location, IdentifierPath.HeapPath, capLifetime);
                }
            }

            return new FunctionDeclarationC(this.Signature, this.funcPath, body);
        }
    }

    public class FunctionDeclarationC : IDeclarationC {
        public readonly FunctionSignature sig;
        private readonly IdentifierPath funcPath;
        private readonly ISyntaxC body;

        public FunctionDeclarationC(FunctionSignature sig, IdentifierPath funcPath, ISyntaxC body) {
            this.sig = sig;
            this.funcPath = funcPath;
            this.body = body;
        }

        public void GenerateCode(ICWriter declWriter) {
            declWriter.RequireRegions();

            var returnType = declWriter.ConvertType(this.sig.ReturnType);
            var pars = this.sig
                .Parameters
                .Select(x => new CParameter(declWriter.ConvertType(x.Type), x.Name))
                .Prepend(new CParameter(CType.NamedType("$Region*"), "heap"))
                .ToArray();

            var statWriter = new CStatementWriter();
            var stats = new List<CStatement>();
            statWriter.StatementWritten += (s, e) => stats.Add(e);

            var retExpr = this.body.GenerateCode(declWriter, statWriter);
            stats.Add(CStatement.Return(retExpr));

            var decl = CDeclaration.Function(returnType, this.funcPath.ToString(), pars, stats);
            var forwardDecl = CDeclaration.FunctionPrototype(returnType, this.funcPath.ToString(), pars);

            declWriter.WriteDeclaration(decl);
            declWriter.WriteDeclaration(CDeclaration.EmptyLine());

            declWriter.WriteForwardDeclaration(forwardDecl);
            declWriter.WriteForwardDeclaration(CDeclaration.EmptyLine());
        }
    }
}