using System;
using System.Collections.Generic;
using System.Linq;
using Attempt18.Features.Functions;
using Attempt18.Types;

namespace Attempt18.Features.Containers.Structs {
    public class StructDeclaration : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public string Name { get; set; }

        public Parameter[] Members { get; set; }

        public ISyntax[] Declarations { get; set; }

        public IdentifierPath StructPath { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            foreach (var decl in this.Declarations) {
                decl.AnalyzeFlow(types, flow);
            }

            this.CapturedVariables = new IdentifierPath[0];
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            names.AddGlobalName(this.StructPath, NameTarget.Struct);

            foreach (var mem in this.Members) {
                names.AddGlobalName(this.StructPath.Append(mem.Name), NameTarget.Reserved);
            }

            foreach (var decl in this.Declarations) {
                decl.DeclareNames(names);
            }
        }

        public void DeclareTypes(TypeChache cache) {
            var sig = new StructSignature() {
                Members = this.Members,
                Name = this.Name
            };

            cache.Structs.Add(this.StructPath, sig);

            foreach (var decl in this.Declarations) {
                decl.DeclareTypes(cache);

                // Add the methods for this struct
                if (decl is FunctionDeclaration funcDecl) {
                    var structType = new StructType(this.StructPath);

                    if (!cache.Methods.TryGetValue(structType, out _)) {
                        cache.Methods[structType] = new Dictionary<string, IdentifierPath>();
                    }

                    cache.Methods[structType][funcDecl.Signature.Name] = funcDecl.FunctionPath;
                }
            }
        }

        public object Evaluate(Dictionary<IdentifierPath, object> memory) {
            return 0;
        }

        public void PreEvaluate(Dictionary<IdentifierPath, object> memory) { }

        public void ResolveNames(NameCache<NameTarget> names) {
            foreach (var mem in this.Members) {
                mem.Type = mem.Type.Resolve(names);
            }

            foreach (var decl in this.Declarations) {
                decl.ResolveNames(names);
            }
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;
            this.StructPath = containingScope.Append(this.Name);

            // Check for duplicate members
            var distinct = this.Members.Select(x => x.Name).Distinct().ToArray();

            if (distinct.Length != this.Members.Length) {
                throw new Exception("Duplicate member names are not allowed");
            }

            // Add a "this" parameter to all function declarations
            foreach (var decl in this.Declarations) {
                if (decl is FunctionDeclaration funcDecl) {
                    var par = new Parameter() {
                        Name = "this",
                        Type = new StructType(this.StructPath)
                    };

                    funcDecl.Signature.Parameters = funcDecl
                        .Signature
                        .Parameters
                        .Prepend(par)
                        .ToArray();
                }
            }

            foreach (var decl in this.Declarations) {
                decl.ResolveScope(this.StructPath);
            }
        }

        public ISyntax ResolveTypes(TypeChache types) {
            foreach (var decl in this.Declarations) {
                decl.ResolveTypes(types);
            }

            this.ReturnType = VoidType.Instance;

            return this;
        }
    }
}
