using Helix.Analysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace helix.front.NameResolution {
    internal class TypeNameResolver : ITypeVisitor<IHelixType> {
        private readonly IdentifierPath scope;
        private readonly DeclarationStore declarations;
        private readonly NameMangler mangler;

        public TypeNameResolver(IdentifierPath scope, DeclarationStore declarations, NameMangler mangler) {
            this.scope = scope;
            this.declarations = declarations;
            this.mangler = mangler;
        }

        public IHelixType VisitArrayType(ArrayType type) {
            var inner = type.InnerType.Accept(this);

            return new ArrayType() { InnerType = inner };
        }

        public IHelixType VisitBoolType(BoolType type) => type;

        public IHelixType VisitFunctionType(FunctionType type) {
            var returnType = type.ReturnType.Accept(this);

            var pars = type.Parameters
                .Select(x => new FunctionParameter() {
                    Name = x.Name,
                    Type = x.Type.Accept(this),
                    IsMutable = x.IsMutable
                })
                .ToArray();

            return new FunctionType() {
                ReturnType = returnType,
                Parameters = pars
            };
        }

        public IHelixType VisitNominalType(NominalType type) {
            if (!this.declarations.ResolveDeclaration(this.scope, type.Name).TryGetValue(out var path)) {
                throw new InvalidOperationException();
            }

            return new NominalType() {
                Name = this.mangler.GetMangledName(path)
            };
        }

        public IHelixType VisitPointerType(PointerType type) {
            var inner = type.InnerType.Accept(this);

            return new PointerType() { InnerType = inner };
        }

        public IHelixType VisitSingularBoolType(SingularBoolType type) => type;

        public IHelixType VisitSingularWordType(SingularWordType type) => type;

        public IHelixType VisitStructType(StructType type) {
            var mems = type.Members
                .Select(x => new StructMember() {
                    Name = x.Name,
                    Type = x.Type.Accept(this),
                    IsMutable = x.IsMutable
                })
                .ToArray();

            return new StructType() { Members = mems };
        }

        public IHelixType VisitUnionType(UnionType type) {
            var mems = type.Members
                .Select(x => new UnionMember() {
                    Name = x.Name,
                    Type = x.Type.Accept(this),
                    IsMutable = x.IsMutable
                })
                .ToArray();

            return new UnionType() { Members = mems };
        }

        public IHelixType VisitVoidType(VoidType type) => type;

        public IHelixType VisitWordType(WordType type) => type;
    }
}
