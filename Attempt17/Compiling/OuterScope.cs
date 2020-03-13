using Attempt17.TypeChecking;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Compiling {
    public class OuterScope : ITypeCheckScope {
        public Dictionary<IdentifierPath, IIdentifierTarget> TypeInfo { get; }
            = new Dictionary<IdentifierPath, IIdentifierTarget>();

        private Dictionary<(LanguageType type, string methodName), IdentifierPath> methods
            = new Dictionary<(LanguageType type, string methodName), IdentifierPath>();

        public IdentifierPath Path => new IdentifierPath();

        public IOption<IIdentifierTarget> FindTypeInfo(IdentifierPath path) {
            return this.TypeInfo.GetValueOption(path);
        }

        public ImmutableHashSet<VariableCapture> GetCapturingVariables(IdentifierPath path) {
            return ImmutableHashSet<VariableCapture>.Empty;
        }

        public bool IsVariableMovable(IdentifierPath path) => false;

        public bool IsVariableMoved(IdentifierPath path) => false;

        public void SetCapturingVariable(VariableCapture capturing, IdentifierPath captured) { throw new InvalidOperationException(); }

        public void SetTypeInfo(IdentifierPath path, IIdentifierTarget info) {
            this.TypeInfo[path] = info;
        }

        public void SetVariableMovable(IdentifierPath path, bool isMovable) { throw new InvalidOperationException(); }

        public void SetVariableMoved(IdentifierPath path, bool isMoved) { throw new InvalidOperationException(); }

        public void SetMethod(LanguageType type, string methodName, IdentifierPath methodLocation) {
            this.methods[(type, methodName)] = methodLocation;
        }

        public IOption<FunctionInfo> FindMethod(LanguageType type, string methodName) {
            return this.methods
                .GetValueOption((type, methodName))
                .SelectMany(this.FindFunction);
        }
    }
}