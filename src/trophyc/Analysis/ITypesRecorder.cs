using System;
using System.Collections.Generic;
using Trophy.Analysis.Types;

namespace Trophy.Analysis {
    public delegate (ITrophyType, IDeclarationC) MetaTypeGenerator(ITrophyType[] args);

    public interface ITypesRecorder {
        public TypesContext Context { get; }

        public IOption<ISyntaxC> TryUnifyTo(ISyntaxC target, ITrophyType newType);

        public T WithContext<T>(TypesContext context, Func<ITypesRecorder, T> func);

        public void DeclareName(IdentifierPath path, NamePayload payload);

        public IOption<NamePayload> TryGetName(IdentifierPath path);

        public void DeclareMethodPath(ITrophyType type, string name, IdentifierPath path);

        public IOption<IdentifierPath> TryGetMethodPath(ITrophyType type, string name);

        public void DeclareMetaType(GenericType meta, MetaTypeGenerator generator);

        public ITrophyType InstantiateMetaType(GenericType type, ITrophyType[] args);
    }

    public static class TypeRecorderExtensions {
        public static IOption<FunctionSignature> TryGetFunction(this ITypesRecorder types, IdentifierPath path) {
            return types.TryGetName(path).SelectMany(x => x.AsFunction());
        }

        public static IOption<VariableInfo> TryGetVariable(this ITypesRecorder types, IdentifierPath path) {
            return types.TryGetName(path).SelectMany(x => x.AsVariable());
        }

        public static IOption<AggregateSignature> TryGetStruct(this ITypesRecorder types, IdentifierPath path) {
            return types.TryGetName(path).SelectMany(x => x.AsStruct());
        }

        public static IOption<AggregateSignature> TryGetUnion(this ITypesRecorder types, IdentifierPath path) {
            return types.TryGetName(path).SelectMany(x => x.AsUnion());
        }
    }

    public class TypesContext {
        public ContainingFunction ContainingFunction { get; }

        public TypesContext(ContainingFunction func) {
            this.ContainingFunction = func;
        }

        public TypesContext WithContainingFunction(ContainingFunction func) {
            return new TypesContext(func);
        }
    }

    public class ContainingFunction {
        private readonly object value;

        public static ContainingFunction Lambda {
            get => new ContainingFunction(false);
        }

        public static ContainingFunction None {
            get => new ContainingFunction(true);
        }

        public static ContainingFunction FromDeclaration(FunctionSignature sig) {
            return new ContainingFunction(sig);
        }

        public IOption<FunctionSignature> AsFunctionDeclaration() {
            if (value is FunctionSignature sig) {
                return Option.Some(sig);
            }
            else {
                return Option.None<FunctionSignature>();
            }
        }

        public bool AsLambda() {
            return value is false;
        }

        public bool AsNone() {
            return value is true;
        }

        private ContainingFunction(object value) {
            this.value = value;
        }
    }

    public class NamePayload {
        private readonly NameTarget target;
        private readonly object payload;

        public static NamePayload FromVariable(VariableInfo info) {
            return new NamePayload(NameTarget.Variable, info);
        }

        public static NamePayload FromFunction(FunctionSignature sig) {
            return new NamePayload(NameTarget.Function, sig);
        }

        public static NamePayload FromStruct(AggregateSignature sig) {
            return new NamePayload(NameTarget.Struct, sig);
        }

        public static NamePayload FromUnion(AggregateSignature sig) {
            return new NamePayload(NameTarget.Union, sig);
        }

        private NamePayload(NameTarget target, object payload) {
            this.target = target;
            this.payload = payload;
        }

        public IOption<VariableInfo> AsVariable() {
            return Option.SomeNullable(this.payload as VariableInfo);
        }

        public IOption<FunctionSignature> AsFunction() {
            return Option.SomeNullable(this.payload as FunctionSignature);
        }

        public IOption<AggregateSignature> AsStruct() {
            if (this.target != NameTarget.Struct) {
                return Option.None<AggregateSignature>();
            }

            return Option.SomeNullable(this.payload as AggregateSignature);
        }

        public IOption<AggregateSignature> AsUnion() {
            if (this.target != NameTarget.Union) {
                return Option.None<AggregateSignature>();
            }

            return Option.SomeNullable(this.payload as AggregateSignature);
        }
    }
}