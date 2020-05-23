using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Attempt18.CodeGeneration;
using Attempt18.TypeChecking;
using Attempt18.Types;

namespace Attempt18.Compiling {
    public class OuterCScope : ICScope {
        private readonly IReadOnlyDictionary<IdentifierPath, IIdentifierTarget> typeinfo;

        public OuterCScope(IReadOnlyDictionary<IdentifierPath, IIdentifierTarget> typeinfo) {
            this.typeinfo = typeinfo;
        }

        public IOption<IIdentifierTarget> GetTypeInfo(IdentifierPath path) {
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