using System.Collections.Generic;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.Features.Containers;
using Attempt20.Features.Containers.Arrays;
using Attempt20.Features.Primitives;

namespace Attempt20.Compiling {
    public class TypesRecorder : ITypeRecorder {
        private readonly Dictionary<IdentifierPath, VariableInfo> variables = new Dictionary<IdentifierPath, VariableInfo>();
        private readonly Dictionary<IdentifierPath, FunctionSignature> functions = new Dictionary<IdentifierPath, FunctionSignature>();
        private readonly Dictionary<IdentifierPath, StructSignature> structs = new Dictionary<IdentifierPath, StructSignature>();

        public void DeclareVariable(IdentifierPath path, VariableInfo info) {
            this.variables[path] = info;
        }

        public void DeclareFunction(IdentifierPath path, FunctionSignature sig) {
            this.functions[path] = sig;
        }

        public void DeclareStruct(IdentifierPath path, StructSignature sig) {
            this.structs[path] = sig;
        }

        public IOption<FunctionSignature> TryGetFunction(IdentifierPath path) {
            return this.functions.GetValueOption(path);
        }

        public IOption<VariableInfo> TryGetVariable(IdentifierPath path) {
            return this.variables.GetValueOption(path);
        }

        public IOption<StructSignature> TryGetStruct(IdentifierPath path) {
            return structs.GetValueOption(path);
        }

        public IOption<ISyntax> TryUnifyTo(ISyntax target, TrophyType newType) {
            if (target.ReturnType == newType) {
                return Option.Some(target);
            }

            if (target.ReturnType.IsVoidType) {
                if (newType.IsIntType) {
                    return Option.Some(new VoidToPrimitiveAdapter() { Target = target, ReturnType = TrophyType.Integer });
                }
                else if (newType.IsBoolType) {
                    return Option.Some(new VoidToPrimitiveAdapter() { Target = target, ReturnType = TrophyType.Boolean });
                }
                else if (newType.AsSingularFunctionType().Any()) {
                    return Option.Some(new VoidToPrimitiveAdapter() { Target = target, ReturnType = newType });
                }
                else if (newType.AsArrayType().TryGetValue(out var arrayType)) {
                    return Option.Some(new VoidToArrayAdapter() { Target = target, ReturnType = arrayType });
                }
                else if (newType.AsNamedType().TryGetValue(out var path)) {
                    if (this.TryGetStruct(path).TryGetValue(out var sig) && newType.HasDefaultValue(this)) {
                        return Option.Some(new VoidToStructAdapter(target, sig, newType, this));
                    }
                }
            }
            else if (target.ReturnType.IsBoolType) {
                if (newType.IsIntType) {
                    return Option.Some(new BoolToIntAdapter() { Target = target });
                }
            }
            else if (target.ReturnType.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                if (newType.AsArrayType().TryGetValue(out var arrayType) && fixedArrayType.ElementType == arrayType.ElementType) {
                    return Option.Some(new FixedArrayToArrayAdapter() { ReturnType = newType, Target = target });
                }
            }


            return Option.None<ISyntax>();
        }
    };
}
