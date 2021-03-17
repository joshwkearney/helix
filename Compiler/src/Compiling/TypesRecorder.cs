using System.Collections.Generic;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Features.Containers.Arrays;
using Trophy.Features.Containers.Structs;
using Trophy.Features.Containers.Unions;
using Trophy.Features.Primitives;
using Trophy.Features.Variables;
using System.Linq;
using Trophy.Features.Functions;
using System;

namespace Trophy.Compiling {
    public class TypesRecorder : ITypeRecorder {
        private readonly Stack<Dictionary<IdentifierPath, VariableInfo>> variables = new Stack<Dictionary<IdentifierPath, VariableInfo>>();
        private readonly Stack<Dictionary<IdentifierPath, FunctionSignature>> functions = new Stack<Dictionary<IdentifierPath, FunctionSignature>>();
        private readonly Stack<Dictionary<IdentifierPath, AggregateSignature>> structs = new Stack<Dictionary<IdentifierPath, AggregateSignature>>();
        private readonly Stack<Dictionary<IdentifierPath, AggregateSignature>> unions = new Stack<Dictionary<IdentifierPath, AggregateSignature>>();
        private readonly Stack<Dictionary<ITrophyType, Dictionary<string, IdentifierPath>>> methods = new Stack<Dictionary<ITrophyType, Dictionary<string, IdentifierPath>>>();
        private readonly Stack<Dictionary<ITrophyType, MetaTypeGenerator>> metaTypes = new Stack<Dictionary<ITrophyType, MetaTypeGenerator>>();
        private readonly Stack<ContainingFunction> containingFuncs = new Stack<ContainingFunction>();

        public event EventHandler<IDeclarationC> DeclarationGenerated;

        public TypesRecorder() {
            this.variables.Push(new Dictionary<IdentifierPath, VariableInfo>());
            this.functions.Push(new Dictionary<IdentifierPath, FunctionSignature>());
            this.structs.Push(new Dictionary<IdentifierPath, AggregateSignature>());
            this.unions.Push(new Dictionary<IdentifierPath, AggregateSignature>());
            this.methods.Push(new Dictionary<ITrophyType, Dictionary<string, IdentifierPath>>());
            this.metaTypes.Push(new Dictionary<ITrophyType, MetaTypeGenerator>());
        }

        public void DeclareVariable(IdentifierPath path, VariableInfo info) {
            this.variables.Peek()[path] = info;
        }

        public void DeclareFunction(IdentifierPath path, FunctionSignature sig) {
            this.functions.Peek()[path] = sig;
        }

        public void DeclareStruct(IdentifierPath path, AggregateSignature sig) {
            this.structs.Peek()[path] = sig;
        }

        public IOption<FunctionSignature> TryGetFunction(IdentifierPath path) {
            return this.functions
                .Select(x => x.GetValueOption(path))
                .SelectMany(x => x.AsEnumerable())
                .FirstOrNone();
        }

        public IOption<VariableInfo> TryGetVariable(IdentifierPath path) {
            return this.variables
                .Select(x => x.GetValueOption(path))
                .SelectMany(x => x.AsEnumerable())
                .FirstOrNone();
        }

        public IOption<AggregateSignature> TryGetStruct(IdentifierPath path) {
            return this.structs
                .Select(x => x.GetValueOption(path))
                .SelectMany(x => x.AsEnumerable())
                .FirstOrNone();
        }

        public IOption<ISyntaxC> TryUnifyTo(ISyntaxC target, ITrophyType newType) {
            if (target.ReturnType.Equals(newType)) {
                return Option.Some(target);
            }

            if (target.ReturnType.IsVoidType) {
                if (newType.IsIntType) {
                    return Option.Some(new VoidToPrimitiveAdapterC(target, ITrophyType.Integer));
                }
                else if (newType.IsBoolType) {
                    return Option.Some(new VoidToPrimitiveAdapterC(target, ITrophyType.Boolean));
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
                        return Option.Some(new VoidToUnionAdapterC(target, unionSig, newType, this));
                    }
                }
            }
            else if (target.ReturnType.IsBoolType) {
                if (newType.IsIntType) {
                    return Option.Some(new BoolToIntAdapter(target));
                }
            }
            else if (target.ReturnType.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                if (newType.AsArrayType().TryGetValue(out var fixedArrayType2) && fixedArrayType.ElementType.Equals(fixedArrayType2.ElementType)) {
                    if (!(fixedArrayType.IsReadOnly && !fixedArrayType2.IsReadOnly)) {
                        return Option.Some(new ArrayToArrayAdapter(target, newType));
                    }
                }
            }
            else if (target.ReturnType.AsArrayType().TryGetValue(out var arrayType)) {
                if (newType.AsArrayType().TryGetValue(out var arrayType2) && arrayType.ElementType.Equals(arrayType2.ElementType)) {
                    if (!(arrayType.IsReadOnly && !arrayType2.IsReadOnly)) {
                        return Option.Some(new ArrayToArrayAdapter(target, newType));
                    }
                }
            }
            else if (target.ReturnType.AsVariableType().TryGetValue(out var varRef1)) {
                if (newType.AsVariableType().TryGetValue(out var varRef2)) {
                    if (varRef1.InnerType.Equals(varRef2.InnerType) && varRef2.IsReadOnly) {
                        return Option.Some(new VarToRefAdapter(target, varRef2));
                    }
                }
            }
            else if (target.ReturnType.AsSingularFunctionType().TryGetValue(out var singFunc)) {
                if (newType.AsFunctionType().TryGetValue(out var func)) {
                    var singSig = this.TryGetFunction(singFunc.FunctionPath).GetValue();
                    var singPars = singSig.Parameters.Select(x => x.Type).ToArray();

                    if (singSig.ReturnType.Equals(func.ReturnType) && singPars.SequenceEqual(func.ParameterTypes)) {
                        return Option.Some(new SingularFunctionToFunctionAdapter(target, singFunc.FunctionPath, newType));
                    }
                }
            }

            return Option.None<ISyntaxC>();
        }

        public void DeclareMethodPath(ITrophyType type, string name, IdentifierPath path) {
            var methods = this.methods.Peek();

            if (!methods.ContainsKey(type)) {
                methods[type] = new Dictionary<string, IdentifierPath>();
            }

            methods[type][name] = path;
        }

        public IOption<IdentifierPath> TryGetMethodPath(ITrophyType type, string name) {
            return this.methods
                .SelectMany(x => x.GetValueOption(type).AsEnumerable())
                .SelectMany(x => x.GetValueOption(name).AsEnumerable())
                .FirstOrNone();
        }

        public void DeclareUnion(IdentifierPath path, AggregateSignature sig) {
            this.unions.Peek()[path] = sig;
        }

        public IOption<AggregateSignature> TryGetUnion(IdentifierPath path) {
            return this.unions
                .Select(x => x.GetValueOption(path))
                .SelectMany(x => x.AsEnumerable())
                .FirstOrNone();
        }

        public void PushFlow() {
            this.variables.Push(new Dictionary<IdentifierPath, VariableInfo>());
            this.functions.Push(new Dictionary<IdentifierPath, FunctionSignature>());
            this.structs.Push(new Dictionary<IdentifierPath, AggregateSignature>());
            this.methods.Push(new Dictionary<ITrophyType, Dictionary<string, IdentifierPath>>()); this.variables.Push(new Dictionary<IdentifierPath, VariableInfo>());
            this.metaTypes.Push(new Dictionary<ITrophyType, MetaTypeGenerator>());
        }

        public void PopFlow() {
            this.variables.Pop();
            this.functions.Pop();
            this.structs.Pop();
            this.methods.Pop();
            this.metaTypes.Pop();
        }

        public void DeclareMetaType(GenericType meta, MetaTypeGenerator generator) {
            this.metaTypes.Peek()[meta] = generator;
        }

        public ITrophyType InstantiateMetaType(GenericType type, ITrophyType[] args) {
            var (newType, decl) = this.metaTypes.Peek()[type](args);

            this.DeclarationGenerated?.Invoke(this, decl);

            return newType;
        }

        public IOption<ContainingFunction> PopContainingFunction() {
            return this.containingFuncs.FirstOrNone();
        }

        public void PushContainingFunction(ContainingFunction func) {
            this.containingFuncs.Push(func);
        }
    }
}