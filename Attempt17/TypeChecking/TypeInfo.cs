using System;
using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public abstract class TypeInfo {
        public abstract T Match<T>(
            Func<VariableInfo, T> ifVarInfo,
            Func<FunctionInfo, T> ifFuncInfo,
            Func<CompositeInfo, T> ifStructInfo);

        public LanguageType Type {
            get {
                return this.Match(
                    x => x.VariableType,
                    x => x.FunctionType,
                    x => x.StructType);
            }
        }

        public IOption<VariableInfo> AsVariableInfo() {
            return this.Match(
                Option.Some,
                _ => Option.None<VariableInfo>(),
                _ => Option.None<VariableInfo>());
        }

        public IOption<FunctionInfo> AsFunctionInfo() {
            return this.Match(
                _ => Option.None<FunctionInfo>(),
                Option.Some,
                _ => Option.None<FunctionInfo>());
        }

        public IOption<CompositeInfo> AsStructInfo() {
            return this.Match(
                _ => Option.None<CompositeInfo>(),
                _ => Option.None<CompositeInfo>(),
                Option.Some);
        }
    }

    public class FunctionInfo : TypeInfo {
        public FunctionSignature Signature { get; }

        public IdentifierPath Path { get; }

        public NamedType FunctionType => new NamedType(this.Path);

        public FunctionInfo(IdentifierPath path, FunctionSignature sig) {
            this.Path = path;
            this.Signature = sig;
        }

        public override T Match<T>(
            Func<VariableInfo, T> ifVarInfo,
            Func<FunctionInfo, T> ifFuncInfo,
            Func<CompositeInfo, T> ifStructInfo) {

            return ifFuncInfo(this);
        }
    }

    public class VariableInfo : TypeInfo {
        public VariableDefinitionKind DefinitionKind { get; }

        public LanguageType VariableType { get; }

        public IdentifierPath Path { get; }

        public bool IsFunctionParameter { get; }

        public VariableInfo(LanguageType type, VariableDefinitionKind kind, IdentifierPath path, bool isFuncParameter = false) {
            this.VariableType = type;
            this.DefinitionKind = kind;
            this.Path = path;
            this.IsFunctionParameter = isFuncParameter;
        }

        public override T Match<T>(
            Func<VariableInfo, T> ifVarInfo,
            Func<FunctionInfo, T> ifFuncInfo,
            Func<CompositeInfo, T> ifStructInfo) {

            return ifVarInfo(this);
        }
    }

    public enum CompositeKind {
        Struct, Class
    }

    public class CompositeInfo : TypeInfo {
        public CompositeSignature Signature { get; }

        public IdentifierPath Path { get; }

        public LanguageType StructType => new NamedType(this.Path);

        public CompositeKind Kind { get; }

        public CompositeInfo(CompositeSignature sig, IdentifierPath path, CompositeKind kind) {
            this.Signature = sig;
            this.Path = path;
            this.Kind = kind;
        }

        public override T Match<T>(
            Func<VariableInfo, T> ifVarInfo,
            Func<FunctionInfo, T> ifFuncInfo,
            Func<CompositeInfo, T> ifStructInfo) {

            return ifStructInfo(this);
        }
    }
}