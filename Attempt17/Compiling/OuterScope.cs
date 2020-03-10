using Attempt17.TypeChecking;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Compiling {
    public class OuterScope : IScope {
        private readonly Dictionary<IdentifierPath, TypeInfo> typeInfo
            = new Dictionary<IdentifierPath, TypeInfo>();

        public IdentifierPath Path => new IdentifierPath();

        public IOption<TypeInfo> FindTypeInfo(IdentifierPath path) {
            return this.typeInfo.GetValueOption(path);
        }

        public ImmutableHashSet<VariableCapture> GetCapturingVariables(IdentifierPath path) {
            return ImmutableHashSet<VariableCapture>.Empty;
        }

        public bool IsVariableMovable(IdentifierPath path) => false;

        public bool IsVariableMoved(IdentifierPath path) => false;

        public void SetCapturingVariable(VariableCapture capturing, IdentifierPath captured) { }

        public void SetTypeInfo(IdentifierPath path, TypeInfo info) {
            this.typeInfo[path] = info;
        }

        public void SetVariable(IdentifierPath path, VariableInfo info) { }

        public void SetVariableMovable(IdentifierPath path, bool isMovable) { }

        public void SetVariableMoved(IdentifierPath path, bool isMoved) { }
    }
}