using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Compiling {
    public class OuterCScope : ICScope {
        private readonly IReadOnlyDictionary<IdentifierPath, TypeInfo> typeinfo;

        public OuterCScope(IReadOnlyDictionary<IdentifierPath, TypeInfo> typeinfo) {
            this.typeinfo = typeinfo;
        }

        public IOption<TypeInfo> FindTypeInfo(IdentifierPath path) {
            return this.typeinfo.GetValueOption(path);
        }

        public ImmutableDictionary<string, LanguageType> GetUndestructedVariables() {
            throw new InvalidOperationException();
        }

        public void SetVariableDestructed(string name) {
            throw new InvalidOperationException();
        }

        public void SetVariableMoved(string name) {
            throw new InvalidOperationException();
        }

        public void SetVariableUndestructed(string name, LanguageType type) {
            throw new InvalidOperationException();
        }
    }
}