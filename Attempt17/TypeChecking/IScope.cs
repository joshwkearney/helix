﻿using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public interface IScope {
        IdentifierPath Path { get; }

        void SetTypeInfo(IdentifierPath path, TypeInfo info);

        void SetCapturingVariable(VariableCapture capturing, IdentifierPath captured);

        void SetVariableMovable(IdentifierPath path, bool isMovable);

        void SetVariableMoved(IdentifierPath path, bool isMoved);

        IOption<TypeInfo> FindTypeInfo(IdentifierPath path);

        ImmutableHashSet<VariableCapture> GetCapturingVariables(IdentifierPath path);

        bool IsVariableMoved(IdentifierPath path);

        bool IsVariableMovable(IdentifierPath path);

        void SetMethod(LanguageType type, string methodName, IdentifierPath methodLocation);

        IOption<FunctionInfo> FindMethod(LanguageType type, string methodName);
    }
}