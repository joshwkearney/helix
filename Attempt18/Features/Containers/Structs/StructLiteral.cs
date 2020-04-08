using System;
using System.Collections.Generic;
using System.Linq;
using Attempt18.Types;

namespace Attempt18.Features.Containers.Structs {
    public class NamedArgument {
        public string Name { get; set; }

        public ISyntax Value { get; set; }
    }

    public class StructLiteral : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public LanguageType TargetType { get; set; }

        public NamedArgument[] Arguments { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            foreach (var arg in this.Arguments) {
                arg.Value.AnalyzeFlow(types, flow);
            }

            this.CapturedVariables = this.Arguments
                .SelectMany(x => x.Value.CapturedVariables)
                .Distinct()
                .ToArray();
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            foreach (var arg in this.Arguments) {
                arg.Value.DeclareNames(names);
            }
        }

        public void DeclareTypes(TypeChache cache) {
            foreach (var arg in this.Arguments) {
                arg.Value.DeclareTypes(cache);
            }
        }

        public object Evaluate(Dictionary<IdentifierPath, object> memory) {
            return this.Arguments
                .Select(x => new {
                    x.Name,
                    Value = x.Value.Evaluate(memory)
                })
                .ToDictionary(x => x.Name, x => x.Value);
        }

        public void PreEvaluate(Dictionary<IdentifierPath, object> memory) {
            foreach (var arg in this.Arguments) {
                arg.Value.PreEvaluate(memory);
            }
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            this.TargetType = this.TargetType.Resolve(names);

            foreach (var arg in this.Arguments) {
                arg.Value.ResolveNames(names);
            }
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;

            foreach (var arg in this.Arguments) {
                arg.Value.ResolveScope(containingScope);
            }
        }

        public ISyntax ResolveTypes(TypeChache types) {
            // Make sure the target type is a struct type
            if (!(this.TargetType is StructType structType)) {
                throw new Exception();
            }

            var info = types.Structs[structType.Path];
            var memNames = info.Members.Select(x => x.Name).ToHashSet();
            var argNames = this.Arguments.Select(x => x.Name).ToHashSet();

            // Make sure that all struct members are accounted for
            if (!memNames.SetEquals(argNames)) {
                throw new Exception();
            }

            // Type check the arguments
            foreach (var arg in this.Arguments) {
                arg.Value.ResolveTypes(types);
            }

            var zipped = info.Members.Join(this.Arguments, x => x.Name, x => x.Name, (x, y) => new {
                ExpectedType = x.Type,
                ActualType = y.Value.ReturnType
            });

            // Make sure that the argument types match the struct member types
            foreach (var zip in zipped) {
                if (zip.ActualType != zip.ExpectedType) {
                    throw new Exception();
                }
            }

            this.ReturnType = structType;

            return this;
        }
    }
}
