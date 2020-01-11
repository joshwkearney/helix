using System;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt17.TypeChecking {
    public interface IScope {
        IdentifierPath Path { get; }

        void SetVariable(IdentifierPath path, VariableInfo info);

        void SetFunction(IdentifierPath path, FunctionInfo info);

        void SetCapturingVariable(VariableCapture capturing, IdentifierPath captured);

        void SetVariableMovable(IdentifierPath path, bool isMovable);

        void SetVariableMoved(IdentifierPath path, bool isMoved);

        IOption<VariableInfo> FindVariable(IdentifierPath path);

        IOption<FunctionInfo> FindFunction(IdentifierPath path);

        ImmutableHashSet<VariableCapture> GetCapturingVariables(IdentifierPath path);

        bool IsVariableMoved(IdentifierPath path);

        bool IsVariableMovable(IdentifierPath path);
    }
}