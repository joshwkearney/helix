using System.Collections.Generic;

namespace JoshuaKearney.Attempt15.Compiling {
    public interface ICodeGenerator {
        Stack<List<string>> CodeBlocks { get; }
        List<string> GlobalCode { get; }

        string GetTempVariableName();
    }
}