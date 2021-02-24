using System.Collections.Generic;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.Features.Containers.Arrays;
using Attempt20.Features.Containers.Structs;
using Attempt20.Features.Containers.Unions;
using Attempt20.Features.Primitives;
using Attempt20.Parsing;
using Compiler.Features.Variables;

namespace Attempt20.Compiling {
    public class TypesRecorder : ITypeRecorder {
        private readonly Dictionary<IdentifierPath, VariableInfo> variables = new Dictionary<IdentifierPath, VariableInfo>();
        private readonly Dictionary<IdentifierPath, FunctionSignature> functions = new Dictionary<IdentifierPath, FunctionSignature>();
        private readonly Dictionary<IdentifierPath, AggregateSignature> structs = new Dictionary<IdentifierPath, AggregateSignature>();
        private readonly Dictionary<IdentifierPath, AggregateSignature> unions = new Dictionary<IdentifierPath, AggregateSignature>();
        private readonly Dictionary<TrophyType, Dictionary<string, IdentifierPath>> methods = new Dictionary<TrophyType, Dictionary<string, IdentifierPath>>();

        public void DeclareVariable(IdentifierPath path, VariableInfo info) {
            this.variables[path] = info;
        }

        public void DeclareFunction(IdentifierPath path, FunctionSignature sig) {
            this.functions[path] = sig;
        }

        public void DeclareStruct(IdentifierPath path, AggregateSignature sig) {
            this.structs[path] = sig;
        }

        public IOption<FunctionSignature> TryGetFunction(IdentifierPath path) {
            return this.functions.GetValueOption(path);
        }

        public IOption<VariableInfo> TryGetVariable(IdentifierPath path) {
            return this.variables.GetValueOption(path);
        }

        public IOption<AggregateSignature> TryGetStruct(IdentifierPath path) {
            return structs.GetValueOption(path);
        }

        public IOption<ISyntaxC> TryUnifyTo(ISyntaxC target, TrophyType newType) {
            if (target.ReturnType == newType) {
                return Option.Some(target);
            }

            if (target.ReturnType.IsVoidType) {
                if (newType.IsIntType) {
                    return Option.Some(new VoidToPrimitiveAdapterC(target, TrophyType.Integer));
                }
                else if (newType.IsBoolType) {
                    return Option.Some(new VoidToPrimitiveAdapterC(target, TrophyType.Boolean));
                }
                else if (newType.AsSingularFunctionType().Any()) {
                    return Option.Some(new VoidToPrimitiveAdapterC(target, newType));
                }
                else if (newType.AsArrayType().TryGetValue(out var arrayType)) {
                    return Option.Some(new VoidToArrayAdapterC(target, arrayType));
                }
                else if (newType.AsNamedType().TryGetValue(out var path)) {
                    if (this.TryGetStruct(path).TryGetValue(out var sig) && newType.HasDefaultValue(this)) {
                        return Option.Some(new VoidToStructAdapterC(target, sig, newType, this));
                    }
                    else if (this.TryGetUnion(path).TryGetValue(out var unionSig) && newType.HasDefaultValue(this)) {
                        return Option.Some(new VoidToUnionAdapterC(target, sig, newType, this));
                    }
                }
            }
            else if (target.ReturnType.IsBoolType) {
                if (newType.IsIntType) {
                    return Option.Some(new BoolToIntAdapter(target));
                }
            }
            else if (target.ReturnType.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                if (newType.AsArrayType().TryGetValue(out var fixedArrayType2) && fixedArrayType.ElementType == fixedArrayType2.ElementType) {
                    if (!(fixedArrayType.IsReadOnly && !fixedArrayType2.IsReadOnly)) {
                        return Option.Some(new ArrayToArrayAdapter(target, newType));
                    }
                }
            }
            else if (target.ReturnType.AsArrayType().TryGetValue(out var arrayType)) {
                if (newType.AsArrayType().TryGetValue(out var arrayType2) && arrayType.ElementType == arrayType2.ElementType) {
                    if (!(arrayType.IsReadOnly && !arrayType2.IsReadOnly)) {
                        return Option.Some(new ArrayToArrayAdapter(target, newType));
                    }
                }
            }
            else if (target.ReturnType.AsVariableType().TryGetValue(out var varRef1)) {
                if (newType.AsVariableType().TryGetValue(out var varRef2)) {
                    if (varRef1.InnerType == varRef2.InnerType && varRef2.IsReadOnly) {
                        return Option.Some(new VarToRefAdapter(target, varRef2));
                    }
                }
            }


            return Option.None<ISyntaxC>();
        }

        public void DeclareMethodPath(TrophyType type, string name, IdentifierPath path) {
            if (!this.methods.ContainsKey(type)) {
                this.methods[type] = new Dictionary<string, IdentifierPath>();
            }

            this.methods[type][name] = path;
        }

        public IOption<IdentifierPath> TryGetMethodPath(TrophyType type, string name) {
            return this.methods.GetValueOption(type).SelectMany(x => x.GetValueOption(name));
        }

        public void DeclareUnion(IdentifierPath path, AggregateSignature sig) {
            this.unions[path] = sig;
        }

        public IOption<AggregateSignature> TryGetUnion(IdentifierPath path) {
            return this.unions.GetValueOption(path);
        }
    };
}
