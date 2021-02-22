using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.Features.Functions;
using Attempt20.Parsing;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt20.Features.Containers {
    public enum AggregateKind {
        Struct, Union
    }

    public class AggregateDeclarationA : IDeclarationA {
        private readonly AggregateSignature sig;
        private readonly IReadOnlyList<IDeclarationA> decls;
        private readonly AggregateKind kind;

        public TokenLocation Location { get; }

        public AggregateDeclarationA(TokenLocation location, AggregateSignature sig, AggregateKind kind, IReadOnlyList<IDeclarationA> decls) {
            this.Location = location;
            this.sig = sig;
            this.kind = kind;
            this.decls = decls;
        }

        public IDeclarationA DeclareNames(INameRecorder names) {
            // Make sure this name isn't taken
            if (names.TryFindName(this.sig.Name, out _, out _)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.sig.Name);
            }

            var aggPath = names.CurrentScope.Append(this.sig.Name);

            // Declare this aggregate
            if (this.kind == AggregateKind.Struct) {
                names.DeclareGlobalName(aggPath, NameTarget.Struct);
            }
            else {
                names.DeclareGlobalName(aggPath, NameTarget.Union);
            }

            // Declare the members
            foreach (var mem in this.sig.Members) {
                names.DeclareGlobalName(aggPath.Append(mem.MemberName), NameTarget.Reserved);
            }

            var parNames = this.sig.Members.Select(x => x.MemberName).ToArray();
            var unique = parNames.Distinct().ToArray();

            // Check for duplicate member names
            if (parNames.Length != unique.Length) {
                var dup = parNames.Except(unique).First();

                throw TypeCheckingErrors.IdentifierDefined(this.Location, dup);
            }

            // Process the rest of the nested declarations
            names.PushScope(names.CurrentScope.Append(this.sig.Name));

            // Rewrite function declarations to be methods
            var decls = this.decls
                .Select(x => {
                    if (x is FunctionDeclarationA func) {
                        var structType = new NamedType(names.CurrentScope.Append(this.sig.Name));
                        var newPars = func.Signature.Parameters.Prepend(new FunctionParameter("this", structType)).ToImmutableList();
                        var newSig = new FunctionSignature(func.Signature.Name, func.Signature.ReturnType, newPars);

                        return new FunctionDeclarationA(func.Location, newSig, func.Body);
                    }
                    else {
                        return x;
                    }
                })
                .Select(x => x.DeclareNames(names))
                .ToArray();

            names.PopScope();

            return new AggregateDeclarationA(this.Location, this.sig, this.kind, decls);
        }

        public IDeclarationB ResolveNames(INameRecorder names) {
            // Resolve members
            var mems = this.sig
                .Members
                .Select(x => new StructMember(x.MemberName, names.ResolveTypeNames(x.MemberType, this.Location)))
                .ToArray();

            var aggPath = names.CurrentScope.Append(this.sig.Name);
            var sig = new AggregateSignature(this.sig.Name, mems);

            // Process the rest of the nested declarations
            names.PushScope(names.CurrentScope.Append(this.sig.Name));
            var decls = this.decls.Select(x => x.ResolveNames(names)).ToArray();
            names.PopScope();

            return new AggregateDeclarationB(this.Location, this.kind, aggPath, this.sig, decls);
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

        public IDeclarationB DeclareTypes(ITypeRecorder types) {
            var structType = new NamedType(this.aggPath);

            if (this.kind == AggregateKind.Struct) {
                types.DeclareStruct(this.aggPath, this.sig);
            }
            else {
                types.DeclareUnion(this.aggPath, this.sig);
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

        public IDeclarationC ResolveTypes(ITypeRecorder types) {
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