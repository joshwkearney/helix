using System;
using System.Collections.Generic;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Parsing;

namespace Trophy.Features.Containers.Structs {
    public class MetaSyntaxGenerator {
        private readonly IDeclarationA innerDecl;
        private readonly IReadOnlyList<string> genericNames;

        public MetaSyntaxGenerator(IDeclarationA decl, IReadOnlyList<string> genericNames) {
            this.innerDecl = decl;
            this.genericNames = genericNames;
        }

        public IDeclarationC Generate(INamesRecorder names, ITypeRecorder types, IReadOnlyList<ITrophyType> genericTypes) {
            int id = names.GetNewVariableId();

            var context = names.Context.WithScope(x => x.Append("$meta" + id));
            var declC = names.WithContext(context, names => {
                foreach (var name in this.genericNames) {
                    names.DeclareName(names.Context.Scope.Append(name), NameTarget.Reserved, IdentifierScope.LocalName);
                }

                if (this.genericNames.Count != genericTypes.Count) {
                    throw new Exception();
                }

                for (int i = 0; i < this.genericNames.Count; i++) {
                    var name = this.genericNames[i];
                    var type = genericTypes[i];

                    if (type.AsNamedType().TryGetValue(out var typePath) && types.TryGetStruct(typePath).TryGetValue(out var sig)) {
                        types.DeclareStruct(names.Context.Scope.Append(name), sig);
                    }
                    else {
                        throw new Exception();
                    }
                }

                return this.innerDecl
                    .DeclareNames(names)
                    .ResolveNames(names)
                    .DeclareTypes(types)
                    .ResolveTypes(types);
            });

            return declC;
        }
    }

    public class MetaStructDeclarationA : IDeclarationA {
        private static int counter;

        private readonly IDeclarationA decl;
        private readonly IReadOnlyList<string> typeNames;
        private readonly int id;

        public TokenLocation Location => this.decl.Location;

        public MetaStructDeclarationA(IDeclarationA decl, IReadOnlyList<string> typeNames, int id = -1) {
            this.decl = decl;
            this.typeNames = typeNames;
            this.id = id;
        }

        public IDeclarationA DeclareNames(INamesRecorder names) {
            int id = counter++;

            // Declare the generic names for name resolution
            var context = names.Context.WithScope(x => x.Append("$meta" + id));
            var decl = names.WithContext(context, names => {
                foreach (var name in this.typeNames) {
                    var path = names.Context.Scope.Append(name);

                    names.DeclareName(path, NameTarget.Struct, IdentifierScope.LocalName);
                }

                return this.decl.DeclareNames(names);
            });

            return new MetaStructDeclarationA(decl, this.typeNames, id);
        }

        public IDeclarationB ResolveNames(INamesRecorder names) {
            var name = "$meta" + this.id;
            var path = names.Context.Scope.Append(name);

            return new MetaStructDeclarationB(decl, path, names, this.typeNames);
        }
    }

    public class MetaStructDeclarationB : IDeclarationB {
        private readonly IDeclarationA decl;
        private readonly IReadOnlyList<string> typeNames;
        private readonly IdentifierPath path;
        private readonly INamesRecorder names;

        public MetaStructDeclarationB(IDeclarationA decl, IdentifierPath path, INamesRecorder names, IReadOnlyList<string> typeNames) {
            this.decl = decl;
            this.path = path;
            this.typeNames = typeNames;
            this.names = names;
        }

        public IDeclarationB DeclareTypes(ITypeRecorder types) {
            var generator = new MetaSyntaxGenerator(this.decl, this.typeNames);

            // Declare this meta type
            var metaType = new GenericType(this.path, this.typeNames);
            types.DeclareMetaType(metaType, args => {
                var decl = generator.Generate(this.names, types, args);

                return (null, decl);
            });

            return this;
        }

        public IDeclarationC ResolveTypes(ITypeRecorder types) {
            return new EmptyDeclaration();
        }
    }

    public class EmptyDeclaration : IDeclarationC {
        public void GenerateCode(ICWriter writer) { }
    }
}