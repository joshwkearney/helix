using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Attempt20.CodeGeneration;

namespace Attempt20 {
    public interface IParsedSyntax {
        public TokenLocation Location { get; }

        public IParsedSyntax CheckNames(INameRecorder names);

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types);
    }

    public interface ITypeCheckedSyntax {
        public TokenLocation Location { get; }

        public LanguageType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter);
    }

    public interface IParsedDeclaration {
        public TokenLocation Location { get; }

        public void DeclareNames(INameRecorder names);

        public void ResolveNames(INameRecorder names);

        public void DeclareTypes(INameRecorder names, ITypeRecorder types);

        public ITypeCheckedDeclaration ResolveTypes(INameRecorder names, ITypeRecorder types);
    }

    public interface ITypeCheckedDeclaration {
        public TokenLocation Location { get; }

        public void GenerateCode(ICDeclarationWriter declWriter);
    }

    public interface ICStatementWriter {
        public void WriteStatement(CStatement stat);
    }

    public interface ICDeclarationWriter {
        public void WriteDeclaration(CDeclaration decl);

        public void WriteForwardDeclaration(CDeclaration decl);

        public void RequireRegions();

        public CType ConvertType(LanguageType type);
    }

    public interface INameRecorder {
        public IdentifierPath CurrentScope { get; }

        public IdentifierPath CurrentRegion { get; }

        public void DeclareGlobalName(IdentifierPath path, NameTarget target);

        public void DeclareLocalName(IdentifierPath path, NameTarget target);

        public bool TryGetName(IdentifierPath path, out NameTarget nameTarget);

        public bool TryFindName(string name, out NameTarget nameTarget, out IdentifierPath path);

        public void PushScope(IdentifierPath newScope);

        public void PopScope();

        public void PushRegion(IdentifierPath newRegion);

        public void PopRegion();

        public LanguageType ResolveTypeNames(LanguageType type, TokenLocation loc);
    }

    public interface ITypeRecorder {
        public void DeclareVariable(IdentifierPath path, VariableInfo info);

        public void DeclareFunction(IdentifierPath path, FunctionSignature sig);

        public void DeclareStruct(IdentifierPath path, StructSignature sig);

        public IOption<VariableInfo> TryGetVariable(IdentifierPath path);

        public IOption<FunctionSignature> TryGetFunction(IdentifierPath path);

        public IOption<StructSignature> TryGetStruct(IdentifierPath path);

        public IOption<ITypeCheckedSyntax> TryUnifyTo(ITypeCheckedSyntax target, LanguageType newType);
    }

    public enum VariableDefinitionKind {
        Local, LocalAllocated, Parameter
    }

    public class VariableInfo {
        public VariableDefinitionKind DefinitionKind { get; }

        public LanguageType Type { get; }

        public ImmutableHashSet<IdentifierPath> ValueLifetimes { get; }

        public ImmutableHashSet<IdentifierPath> VariableLifetimes { get; }

        public VariableInfo(
            LanguageType innerType,
            VariableDefinitionKind alias,
            ImmutableHashSet<IdentifierPath> valueLifetimes,
            ImmutableHashSet<IdentifierPath> variableLifetimes) {

            this.Type = innerType;
            this.DefinitionKind = alias;
            this.ValueLifetimes = valueLifetimes;
            this.VariableLifetimes = variableLifetimes;
        }
    }

    public class Parameter : IEquatable<Parameter> {
        public string Name { get; }

        public LanguageType Type { get; }

        public Parameter(string name, LanguageType type) {
            this.Name = name;
            this.Type = type;
        }

        public override bool Equals(object obj) {
            if (obj is Parameter par) {
                return this.Equals(par);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.Name.GetHashCode() + 7 * this.Type.GetHashCode();
        }

        public bool Equals(Parameter other) {
            if (other is null) {
                return false;
            }

            if (this.Name != other.Name) {
                return false;
            }

            if (this.Type != other.Type) {
                return false;
            }

            return true;
        }

        public static bool operator ==(Parameter par1, Parameter par2) {
            if (par1 is null) {
                return par2 is null;
            }
            else {
                return par1.Equals(par2);
            }
        }

        public static bool operator !=(Parameter par1, Parameter par2) {
            return !(par1 == par2);
        }
    }

    public class StructMember : IEquatable<StructMember> {
        public string MemberName { get; }

        public LanguageType MemberType { get; }

        public StructMember(string name, LanguageType type) {
            this.MemberName = name;
            this.MemberType = type;
        }

        public bool Equals(StructMember other) {
            if (other is null) {
                return false;
            }

            return this.MemberName == other.MemberName
                && this.MemberType == other.MemberType;
        }

        public override bool Equals(object obj) {
            if (obj is StructMember mem) {
                return this.Equals(mem);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.MemberName.GetHashCode()
                + 7 * this.MemberType.GetHashCode();
        }
    }

    public class StructSignature : IEquatable<StructSignature> {
        public string Name { get; }

        public IReadOnlyList<StructMember> Members { get; }

        public StructSignature(string name, IReadOnlyList<StructMember> mems) {
            this.Name = name;
            this.Members = mems;
        }

        public bool Equals(StructSignature other) {
            if (other is null) {
                return false;
            }

            return this.Name == other.Name && this.Members.SequenceEqual(other.Members);
        }

        public override bool Equals(object obj) {
            return obj is StructSignature sig && this.Equals(sig);
        }

        public override int GetHashCode() {
            return this.Name.GetHashCode() + 32 * this.Members.Aggregate(2, (x, y) => x + 11 * y.GetHashCode());
        }
    }

    public class FunctionSignature : IEquatable<FunctionSignature> {
        public LanguageType ReturnType { get; }

        public ImmutableList<Parameter> Parameters { get; }

        public string Name { get; }

        public FunctionSignature(string name, LanguageType returnType, ImmutableList<Parameter> pars) {
            this.Name = name;
            this.ReturnType = returnType;
            this.Parameters = pars;
        }

        public override bool Equals(object obj) {
            if (obj is FunctionSignature sig) {
                return this.Equals(sig);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.Name.GetHashCode()
                + 7 * this.ReturnType.GetHashCode()
                + 11 * this.Parameters.Aggregate(23, (x, y) => x + 101 * y.GetHashCode());
        }

        public bool Equals(FunctionSignature other) {
            if (other is null) {
                return false;
            }

            if (this.Name != other.Name) {
                return false;
            }

            if (other.ReturnType != this.ReturnType) {
                return false;
            }

            if (!this.Parameters.SequenceEqual(other.Parameters)) {
                return false;
            }

            return true;
        }

        public static bool operator ==(FunctionSignature sig1, FunctionSignature sig2) {
            if (sig1 is null) {
                return sig2 is null;
            }

            return sig1.Equals(sig2);
        }

        public static bool operator !=(FunctionSignature sig1, FunctionSignature sig2) {
            return !(sig1 == sig2);
        }
    }

    public enum NameTarget {
        Variable, Function, Region, Struct
    }
}