using System;
using Attempt17.Features.Functions;
using Attempt17.Features.Structs;
using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public abstract class TypeInfo {
        public abstract T Match<T>(
            Func<VariableInfo, T> ifVarInfo,
            Func<FunctionInfo, T> ifFuncInfo,
            Func<StructInfo, T> ifStructInfo);

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

        public IOption<StructInfo> AsStructInfo() {
            return this.Match(
                _ => Option.None<StructInfo>(),
                _ => Option.None<StructInfo>(),
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
            Func<StructInfo, T> ifStructInfo) {

            return ifFuncInfo(this);
        }
    }

    public class VariableInfo : TypeInfo {
        public VariableDefinitionKind DefinitionKind { get; }

        public LanguageType Type { get; }

        public IdentifierPath Path { get; }

        public VariableInfo(LanguageType type, VariableDefinitionKind kind, IdentifierPath path) {
            this.Type = type;
            this.DefinitionKind = kind;
            this.Path = path;
        }

        public override T Match<T>(
            Func<VariableInfo, T> ifVarInfo,
            Func<FunctionInfo, T> ifFuncInfo,
            Func<StructInfo, T> ifStructInfo) {

            return ifVarInfo(this);
        }
    }

    public class StructInfo : TypeInfo {
        public StructSignature Signature { get; }

        public IdentifierPath Path { get; }

        public LanguageType StructType => new NamedType(this.Path);

        public StructInfo(StructSignature sig, IdentifierPath path) {
            this.Signature = sig;
            this.Path = path;
        }

        public override T Match<T>(
            Func<VariableInfo, T> ifVarInfo,
            Func<FunctionInfo, T> ifFuncInfo,
            Func<StructInfo, T> ifStructInfo) {

            return ifStructInfo(this);
        }
    }
}