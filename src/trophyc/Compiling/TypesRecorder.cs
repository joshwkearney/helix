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
using Trophy.Features.Containers;

namespace Trophy.Compiling {
    public class TypesRecorder : ITypesRecorder {
        private readonly Stack<Dictionary<IdentifierPath, NamePayload>> payloads = new();
        private readonly Stack<Dictionary<ITrophyType, Dictionary<string, IdentifierPath>>> methods = new();
        private readonly Stack<Dictionary<ITrophyType, MetaTypeGenerator>> metaTypes = new();
        private readonly Stack<TypesContext> contexts = new();

        public TypesContext Context => this.contexts.Peek();

        public event EventHandler<IDeclarationC> DeclarationGenerated;

        public TypesRecorder() {
            this.payloads.Push(new Dictionary<IdentifierPath, NamePayload>());
            this.methods.Push(new Dictionary<ITrophyType, Dictionary<string, IdentifierPath>>());
            this.metaTypes.Push(new Dictionary<ITrophyType, MetaTypeGenerator>());
            this.contexts.Push(new TypesContext(ContainingFunction.None));
        }

        public void DeclareName(IdentifierPath path, NamePayload payload) {
            this.payloads.Peek()[path] = payload;
        }

        public IOption<NamePayload> TryGetName(IdentifierPath path) {
            return this.payloads
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
                    if (this.TryGetName(path).SelectMany(x => x.AsStruct()).TryGetValue(out var sig) && newType.HasDefaultValue(this)) {
                        return Option.Some(new VoidToStructAdapterC(target, sig, newType, this));
                    }
                    else if (this.TryGetName(path).SelectMany(x => x.AsUnion()).TryGetValue(out var unionSig) && newType.HasDefaultValue(this)) {
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
            /*else if (target.ReturnType.AsSingularFunctionType().TryGetValue(out var singFunc)) {
                if (newType.AsFunctionType().TryGetValue(out var func)) {
                    var singSig = this.TryGetName(singFunc.FunctionPath).SelectMany(x => x.AsFunction()).GetValue();
                    var singPars = singSig.Parameters.Select(x => x.Type).ToArray();

                    if (singSig.ReturnType.Equals(func.ReturnType) && singPars.SequenceEqual(func.ParameterTypes)) {
                        return Option.Some(new SingularFunctionToFunctionAdapter(target, singFunc.FunctionPath, newType));
                    }
                }
            }*/
            else if (newType.AsNamedType().TryGetValue(out var path) && this.TryGetName(path).TryGetValue(out var payload)) {
                if (payload.AsUnion().TryGetValue(out var unionSig)) {
                    var mems = unionSig
                        .Members
                        .Where(x => x.MemberType.Equals(target.ReturnType))
                        .ToArray();

                    if (mems.Length == 1) {
                        var mem = mems
                            .First()
                            .MemberName;

                        var tag = unionSig
                            .Members
                            .Select(x => x.MemberName)
                            .ToList()
                            .IndexOf(mem);

                        var arg = new StructArgument<ISyntaxC>() {
                            MemberName = mem,
                            MemberValue = target
                        };

                        var put = new NewUnionSyntaxC(arg, tag, newType);

                        return Option.Some(put);
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

        public void DeclareMetaType(GenericType meta, MetaTypeGenerator generator) {
            this.metaTypes.Peek()[meta] = generator;
        }

        public ITrophyType InstantiateMetaType(GenericType type, ITrophyType[] args) {
            var (newType, decl) = this.metaTypes.Peek()[type](args);

            this.DeclarationGenerated?.Invoke(this, decl);

            return newType;
        }

        public T WithContext<T>(TypesContext context, Func<ITypesRecorder, T> func) {
            this.payloads.Push(new Dictionary<IdentifierPath, NamePayload>());
            this.methods.Push(new Dictionary<ITrophyType, Dictionary<string, IdentifierPath>>());
            this.metaTypes.Push(new Dictionary<ITrophyType, MetaTypeGenerator>());
            this.contexts.Push(context);

            var result = func(this);

            this.payloads.Pop();
            this.methods.Pop();
            this.metaTypes.Pop();
            this.contexts.Pop();

            return result;
        }
    }
}