using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Features.Functions;
using Trophy.Parsing;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Features.Meta;

namespace Trophy.Features.Containers {
    public enum AggregateKind {
        Struct, Union
    }    

    public class AggregateDeclarationA : IDeclarationA {
        private readonly ParseAggregateSignature sig;
        private readonly IReadOnlyList<IDeclarationA> decls;
        private readonly AggregateKind kind;

        public TokenLocation Location { get; }

        public AggregateDeclarationA(TokenLocation location, ParseAggregateSignature sig, AggregateKind kind, IReadOnlyList<IDeclarationA> decls) {
            this.Location = location;
            this.sig = sig;
            this.kind = kind;
            this.decls = decls;
        }

        public IDeclarationA DeclareNames(INamesRecorder names) {
            // Make sure this name isn't taken
            if (names.TryFindName(this.sig.Name, out _, out _)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.sig.Name);
            }

            var aggPath = names.Context.Scope.Append(this.sig.Name);

            // Declare this aggregate
            if (this.kind == AggregateKind.Struct) {
                names.DeclareName(aggPath, NameTarget.Struct, IdentifierScope.GlobalName);
            }
            else {
                names.DeclareName(aggPath, NameTarget.Union, IdentifierScope.GlobalName);
            }

            // Declare the members
            foreach (var mem in this.sig.Members) {
                names.DeclareName(aggPath.Append(mem.MemberName), NameTarget.Reserved, IdentifierScope.GlobalName);
            }

            var parNames = this.sig.Members.Select(x => x.MemberName).ToArray();
            var unique = parNames.Distinct().ToArray();

            // Check for duplicate member names
            if (parNames.Length != unique.Length) {
                var dup = parNames.Except(unique).First();

                throw TypeCheckingErrors.IdentifierDefined(this.Location, dup);
            }

            // Rewrite function declarations to be methods
            var context = names.Context.WithScope(x => x.Append(this.sig.Name));
            var decls = names.WithContext(context, names => {
                return this.decls
                    //.Select(x => {
                        //if (x is FunctionDeclarationA func) {
                        //    var structType = new NamedType(names.Context.Scope);
                        //    var newPar = new ParseFunctionParameter("this", new TypeAccessSyntaxA(func.Location, structType), VariableKind.Value);
                        //    var newPars = func.Signature.Parameters.Prepend(newPar).ToImmutableList();
                        //    var newSig = new ParseFunctionSignature(func.Signature.Name, func.Signature.ReturnType, newPars);

                        //    return new FunctionDeclarationA(func.Location, newSig, func.Body);
                        //}
                        //else {
                        //    return x;
                        //}
                    //})
                    .Select(x => x.DeclareNames(names))
                    .ToArray();
            });

            return new AggregateDeclarationA(this.Location, this.sig, this.kind, decls);
        }

        public IDeclarationB ResolveNames(INamesRecorder names) {
            // Resolve members
            var memsOpt = this.sig
                .Members
                .Select(x => x.MemberType.ResolveToType(names).Select(y => new AggregateMember(x.MemberName, y, x.Kind)))
                .ToArray();

            if (!memsOpt.All(x => x.Any())) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.Location);
            }

            var mems = memsOpt
                .Select(x => x.GetValue())
                .Select(x => {
                    if (x.Kind == VariableKind.Value) {
                        return x;
                    }
                    else {
                        return new AggregateMember(x.MemberName, new VarRefType(x.MemberType, x.Kind == VariableKind.RefVariable), x.Kind);
                    }
                })
                .ToArray();


            var aggPath = names.Context.Scope.Append(this.sig.Name);
            var sig = new AggregateSignature(this.sig.Name, mems);

            // Process the rest of the nested declarations
            var context = names.Context.WithScope(x => x.Append(this.sig.Name));
            var decls = names.WithContext(context, names => {
                return this.decls.Select(x => x.ResolveNames(names)).ToArray();
            });

            return new AggregateDeclarationB(this.Location, this.kind, aggPath, sig, decls);
        }
    }

    public class AggregateDeclarationB : IDeclarationB {
        private readonly AggregateSignature sig;
        private readonly IReadOnlyList<IDeclarationB> decls;
        private readonly AggregateKind kind;
        private readonly IdentifierPath aggPath;

        public AggregateDeclarationB(
            TokenLocation location, 
            AggregateKind kind, 
            IdentifierPath aggPath, 
            AggregateSignature sig, 
            IReadOnlyList<IDeclarationB> decls) {

            this.Location = location;
            this.kind = kind;
            this.aggPath = aggPath;
            this.sig = sig;
            this.decls = decls;
        }

        public TokenLocation Location { get; }

        public IDeclarationB DeclareTypes(ITypesRecorder types) {
            var structType = new NamedType(this.aggPath);

            if (this.kind == AggregateKind.Struct) {
                types.DeclareName(this.aggPath, NamePayload.FromStruct(this.sig));
            }
            else {
                types.DeclareName(this.aggPath, NamePayload.FromUnion(this.sig));
            }

            // Process the rest of the nested declarations
            foreach (var decl in this.decls) {
                decl.DeclareTypes(types);
            }

            // Register methods
            foreach (var decl in this.decls) {
                if (decl is FunctionDeclarationB func) {
                    types.DeclareMethodPath(structType, func.Signature.Name, this.aggPath.Append(func.Signature.Name));
                }
            }

            return this;
        }

        public IDeclarationC ResolveTypes(ITypesRecorder types) {
            // Process the rest of the nested declarations
            var decls = this.decls.Select(x => x.ResolveTypes(types)).ToArray();

            if (this.kind == AggregateKind.Struct) {
                return new StructDeclarationC(this.sig, this.aggPath, decls);
            }
            else {
                return new UnionDeclarationC(this.sig, this.aggPath, decls);
            }
        }
    }
}