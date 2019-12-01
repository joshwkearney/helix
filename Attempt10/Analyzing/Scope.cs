using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt12 {
    public class TypeScope {
        public ImmutableDictionary<string, FunctionLiteralSyntax> FunctionMembers { get; }

        public TypeScope() {
            this.FunctionMembers = ImmutableDictionary<string, FunctionLiteralSyntax>.Empty;
        }

        public TypeScope(ImmutableDictionary<string, FunctionLiteralSyntax> funcs) {
            this.FunctionMembers = funcs;
        }

        public TypeScope SetFunctionMember(string name, FunctionLiteralSyntax func) {
            return new TypeScope(this.FunctionMembers.SetItem(name, func));
        }
    }

    public class Scope {
        public int ClosureLevel { get; } = 0;

        public ImmutableDictionary<string, VariableInfo> Variables { get; }

        public ImmutableDictionary<ITrophyType, TypeScope> TypeScopes { get; }

        public ITrophyType EnclosedFunctionType { get; } = null;

        public ImmutableDictionary<string, ITrophyType> Types { get; }

        public Scope() {
            this.ClosureLevel = 0;
            this.Variables = ImmutableDictionary<string, VariableInfo>.Empty;
            this.TypeScopes = ImmutableDictionary<ITrophyType, TypeScope>.Empty;
        }

        public Scope(ImmutableDictionary<string, VariableInfo> vars, ImmutableDictionary<ITrophyType, TypeScope> types, int closureLevel, ITrophyType funcType) {
            this.ClosureLevel = closureLevel;
            this.Variables = vars;
            this.TypeScopes = types;
            this.EnclosedFunctionType = funcType;
        }

        public Scope SetVariable(string name, ITrophyType type, int closureLevel) {
            return new Scope(
                this.Variables.SetItem(name, new VariableInfo(name, type, closureLevel)),
                this.TypeScopes,
                this.ClosureLevel,
                this.EnclosedFunctionType);
        }

        public Scope SetTypeScope(ITrophyType type, TypeScope info) {
            return new Scope(
                this.Variables,
                this.TypeScopes.SetItem(type, info),
                this.ClosureLevel,
                this.EnclosedFunctionType);
        }

        public Scope IncrementClosureLevel() {
            return new Scope(
                this.Variables, 
                this.TypeScopes,
                this.ClosureLevel + 1,
                this.EnclosedFunctionType);
        }

        public Scope SetEnclosedFunctionType(TrophyFunctionType type) {
            return new Scope(this.Variables, this.TypeScopes, this.ClosureLevel, type);
        }
    }
}