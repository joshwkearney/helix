using Helix.Common;
using Helix.Common.Tokens;
using Helix.Common.Types;
using Helix.Common.Types.Visitors;

namespace Helix.Frontend.NameResolution
{
    internal class TypeNameResolver {
        private readonly IdentifierPath scope;
        private readonly DeclarationStore declarations;
        private readonly NameMangler mangler;
        private readonly TokenLocation location;
        private readonly NameResolverVisitor typeResolver;

        public TypeNameResolver(IdentifierPath scope, DeclarationStore declarations, NameMangler mangler, TokenLocation location) {
            this.scope = scope;
            this.declarations = declarations;
            this.mangler = mangler;
            this.location = location;
            this.typeResolver = new NameResolverVisitor(this);
        }

        public IHelixType ResolveType(IHelixType type) {
            return type.Accept(this.typeResolver);
        }

        public FunctionSignature ResolveFunctionSignature(FunctionSignature sig) {
            var returnType = this.ResolveType(sig.ReturnType);
             
            var pars = sig.Parameters
                .Select(x => new FunctionParameter() {
                    Name = this.mangler.GetMangledName(this.scope, x.Name),
                    Type = this.ResolveType(x.Type),
                    IsMutable = x.IsMutable
                })
                .ToArray();

            return new FunctionSignature() {
                ReturnType = returnType,
                Parameters = pars
            };
        }

        public StructSignature ResolveStructSignature(StructSignature sig) {
            var mems = sig.Members
                .Select(x => new StructMember() {
                    Name = x.Name,
                    Type = this.ResolveType(x.Type),
                    IsMutable = x.IsMutable
                })
                .ToValueList();

            return new StructSignature() { Members = mems };
        }

        public UnionSignature ResolveUnionSignature(UnionSignature sig) {
            var mems = sig.Members
                .Select(x => new UnionMember() {
                    Name = x.Name,
                    Type = this.ResolveType(x.Type),
                    IsMutable = x.IsMutable
                })
                .ToValueList();

            return new UnionSignature() { Members = mems };
        }

        private class NameResolverVisitor : ITypeVisitor<IHelixType> {
            private readonly TypeNameResolver resolver;

            public NameResolverVisitor(TypeNameResolver resolver) {
                this.resolver = resolver;
            }

            public IHelixType VisitArrayType(ArrayType type) {
                var inner = type.InnerType.Accept(this);

                return new ArrayType() { InnerType = inner };
            }

            public IHelixType VisitBoolType(BoolType type) => type;

            public IHelixType VisitNominalType(NominalType type) {
                if (!this.resolver.declarations.ResolveDeclaration(this.resolver.scope, type.Name).TryGetValue(out var path)) {
                    throw NameResolutionException.IdentifierUndefined(this.resolver.location, type.Name);
                }

                return new NominalType() {
                    Name = this.resolver.mangler.GetMangledName(path),
                    DisplayName = type.DisplayName
                };
            }

            public IHelixType VisitPointerType(PointerType type) {
                var inner = type.InnerType.Accept(this);

                return new PointerType() { InnerType = inner };
            }

            public IHelixType VisitSingularBoolType(SingularBoolType type) => type;

            public IHelixType VisitSingularUnionType(SingularUnionType type) {
                return new SingularUnionType(type.UnionType.Accept(this), type.Member, type.Value.Accept(this));
            }

            public IHelixType VisitSingularWordType(SingularWordType type) => type;

            public IHelixType VisitVoidType(VoidType type) => type;

            public IHelixType VisitWordType(WordType type) => type;
        }
    }
}
