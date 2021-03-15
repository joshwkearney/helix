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

        public IDeclarationC Generate(INameRecorder names, ITypeRecorder types, IReadOnlyList<TrophyType> genericTypes) {
            int id = names.GetNewVariableId();
            var path = names.CurrentScope.Append("$meta" + id);

            names.PushScope(path);

            foreach (var name in this.genericNames) {
                names.DeclareLocalName(path.Append(name), NameTarget.Reserved);
            }
            
            if (this.genericNames.Count != genericTypes.Count) {
                throw new Exception();
            }

            for (int i = 0; i < this.genericNames.Count; i++) {
                var name = this.genericNames[i];
                var type = genericTypes[i];

                if (type.AsNamedType().TryGetValue(out var typePath) && types.TryGetStruct(typePath).TryGetValue(out var sig)) {
                    types.DeclareStruct(path.Append(name), sig);
                }
                else {
                    throw new Exception();
                }
            }

            var declC = this.innerDecl
                .DeclareNames(names)
                .ResolveNames(names)
                .DeclareTypes(types)
                .ResolveTypes(types);

            names.PopScope();

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

        public IDeclarationA DeclareNames(INameRecorder names) {
            int id = counter++;
            var path = names.CurrentScope.Append("$meta" + id);

            // Declare the generic names for name resolution
            names.PushScope(path);

            foreach (var name in this.typeNames) {
                names.DeclareLocalName(path.Append(name), NameTarget.Struct);
            }

            var decl = this.decl.DeclareNames(names);

            names.PopScope();

            return new MetaStructDeclarationA(decl, this.typeNames, id);
        }

        public IDeclarationB ResolveNames(INameRecorder names) {
            var name = "$meta" + this.id;
            var path = names.CurrentScope.Append(name);

            return new MetaStructDeclarationB(decl, path, names, this.typeNames);
        }
    }

    public class MetaStructDeclarationB : IDeclarationB {
        private readonly IDeclarationA decl;
        private readonly IReadOnlyList<string> typeNames;
        private readonly IdentifierPath path;
        private readonly INameRecorder names;

        public MetaStructDeclarationB(IDeclarationA decl, IdentifierPath path, INameRecorder names, IReadOnlyList<string> typeNames) {
            this.decl = decl;
            this.path = path;
            this.typeNames = typeNames;
            this.names = names;
        }

        public IDeclarationB DeclareTypes(ITypeRecorder types) {
            var generator = new MetaSyntaxGenerator(this.decl, this.typeNames);

            // Declare this meta type
            var metaType = new MetaType(this.path, this.typeNames);
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