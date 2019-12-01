using JoshuaKearney.Attempt15.Types;
using System.Collections.Immutable;

namespace JoshuaKearney.Attempt15 {
    public class Scope {
        public ImmutableDictionary<string, VariableInfo> Variables { get; }

        public ImmutableDictionary<string, ITrophyType> TypeDeclarations { get; }

        public ImmutableStack<IFunctionType> EnclosingFunctionReturnType { get; }

        public Scope() {
            this.Variables = ImmutableDictionary<string, VariableInfo>.Empty;
            this.TypeDeclarations = ImmutableDictionary<string, ITrophyType>.Empty;
            this.EnclosingFunctionReturnType = ImmutableStack<IFunctionType>.Empty;
        }

        public Scope(
            ImmutableDictionary<string, VariableInfo> vars, 
            ImmutableDictionary<string, ITrophyType> types,
            ImmutableStack<IFunctionType> funcType
        ) {
            this.Variables = vars;
            this.TypeDeclarations = types;
            this.EnclosingFunctionReturnType = funcType;
        }

        public Scope SetVariable(string name, VariableInfo variableInfo) {
            return new Scope(
                this.Variables.SetItem(name, variableInfo), 
                this.TypeDeclarations,
                this.EnclosingFunctionReturnType
            );
        }

        public Scope SetType(string name, ITrophyType type) {
            return new Scope(
                this.Variables, 
                this.TypeDeclarations.SetItem(name, type),
                this.EnclosingFunctionReturnType
            );
        }

        public Scope PushFunctionType(IFunctionType type) {
            return new Scope(
                this.Variables,
                this.TypeDeclarations, 
                this.EnclosingFunctionReturnType.Push(type)
            );
        }
    }
}