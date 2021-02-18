using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.CodeGeneration;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Functions {
    public class FunctionParseDeclaration : IParsedDeclaration {
        public TokenLocation Location { get; set; }

        public FunctionSignature Signature { get; set; }

        public IParsedSyntax Body { get; set; }

        public void DeclareNames(INameRecorder names) {
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
        }

        public void DeclareTypes(INameRecorder names, ITypeRecorder types) {
            types.DeclareFunction(names.CurrentScope.Append(this.Signature.Name), this.Signature);
        }

        public void ResolveNames(INameRecorder names) {            
            // Resolve the type names
            var returnType = names.ResolveTypeNames(this.Signature.ReturnType, this.Location);
            var pars = this.Signature
                .Parameters
                .Select(x => new FunctionParameter(x.Name, names.ResolveTypeNames(x.Type, this.Location)))
                .ToImmutableList();

            this.Signature = new FunctionSignature(this.Signature.Name, returnType, pars);

            // Push this function name as the new scope
            names.PushScope(names.CurrentScope.Append(this.Signature.Name));
            names.PushRegion(IdentifierPath.StackPath);

            // Declare the parameters
            foreach (var par in this.Signature.Parameters) {
                names.DeclareLocalName(names.CurrentScope.Append(par.Name), NameTarget.Variable);
            }

            // Pop the new scope out
            this.Body = this.Body.CheckNames(names);

            names.PopRegion();
            names.PopScope();
        }

        public IDeclaration ResolveTypes(INameRecorder names, ITypeRecorder types) {
            names.PushScope(names.CurrentScope.Append(this.Signature.Name));
            names.PushRegion(IdentifierPath.StackPath);

            // Declare the parameters
            foreach (var par in this.Signature.Parameters) {
                var path = names.CurrentScope.Append(par.Name);
                var info = new VariableInfo(
                    par.Type,
                    VariableDefinitionKind.Parameter,
                    new[] { new IdentifierPath("$args_" + par.Name) }.ToImmutableHashSet(),
                    new[] { new IdentifierPath("$args_" + par.Name) }.ToImmutableHashSet());

                types.DeclareVariable(path, info);
            }

            // Type check the body
            var body = this.Body.CheckTypes(names, types);

            // Make sure the return types line up
            if (types.TryUnifyTo(body, this.Signature.ReturnType).TryGetValue(out var newbody)) {
                body = newbody;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(body.Location,
                        this.Signature.ReturnType, body.ReturnType);
            }

            names.PopRegion();
            names.PopScope();

            // The return value must be allocated on the heap or be one of the arguments
            foreach (var capLifetime in body.Lifetimes) {
                if (capLifetime.Segments.Any() && capLifetime.Segments.First().StartsWith("$args_")) {
                    continue;
                }

                if (!capLifetime.Outlives(IdentifierPath.HeapPath)) { 
                    throw TypeCheckingErrors.LifetimeExceeded(body.Location, IdentifierPath.HeapPath, capLifetime);
                }
            }

            return new FunctionTypeCheckedDeclaration() {
                Location = this.Location,
                Signature = this.Signature,
                FunctionPath = names.CurrentScope.Append(this.Signature.Name),
                Body = body
            };
        }
    }

    public class FunctionTypeCheckedDeclaration : IDeclaration {
        public TokenLocation Location { get; set; }

        public FunctionSignature Signature { get; set; }

        public IdentifierPath FunctionPath { get; set; }

        public ISyntax Body { get; set; }

        public void GenerateCode(ICWriter declWriter) {
            declWriter.RequireRegions();

            var returnType = declWriter.ConvertType(this.Signature.ReturnType);
            var pars = this.Signature
                .Parameters
                .Select(x => new CParameter(declWriter.ConvertType(x.Type), x.Name))
                .Prepend(new CParameter(CType.NamedType("$Region*"), "heap"))
                .ToArray();

            var statWriter = new CStatementWriter();
            var stats = new List<CStatement>();
            statWriter.StatementWritten += (s, e) => stats.Add(e);

            var retExpr = this.Body.GenerateCode(declWriter, statWriter);
            stats.Add(CStatement.Return(retExpr));

            var decl = CDeclaration.Function(returnType, this.FunctionPath.ToString(), pars, stats);
            var forwardDecl = CDeclaration.FunctionPrototype(returnType, this.FunctionPath.ToString(), pars);

            declWriter.WriteDeclaration(decl);
            declWriter.WriteDeclaration(CDeclaration.EmptyLine());

            declWriter.WriteForwardDeclaration(forwardDecl);
            declWriter.WriteForwardDeclaration(CDeclaration.EmptyLine());
        }
    }
}