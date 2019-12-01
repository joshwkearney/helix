using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System.Collections.Immutable;

namespace JoshuaKearney.Attempt15.Syntax {

    public interface ISyntaxTree {
        ITrophyType ExpressionType { get; }

        ExternalVariablesCollection ExternalVariables { get; }

        string GenerateCode(CodeGenerateEventArgs args);

        bool DoesVariableEscape(string variableName);
    }
}