using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt18.Types;

namespace Attempt18.TypeChecking {
    public interface ITypeCheckScope : IScope {
        IdentifierPath Path { get; }

        void SetTypeInfo(IdentifierPath path, IIdentifierTarget info);

        void SetCapturingVariable(VariableCapture capturing, IdentifierPath captured);

        void SetVariableMovable(IdentifierPath path, bool isMovable);

        void SetVariableMoved(IdentifierPath path, bool isMoved);

        ImmutableHashSet<VariableCapture> GetCapturingVariables(IdentifierPath path);

        bool IsVariableMoved(IdentifierPath path);

        bool IsVariableMovable(IdentifierPath path);

        void SetMethod(LanguageType type, string methodName, IdentifierPath methodLocation);

        IOption<FunctionInfo> FindMethod(LanguageType type, string methodName);
    }
}