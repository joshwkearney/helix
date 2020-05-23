using Attempt18.CodeGeneration;
using Attempt18.Features;
using Attempt18.TypeChecking;
using Attempt18.Types;

namespace Attempt18.CodeGeneration {
    public interface ICodeGenerator {
        ICodeWriter Header1Writer { get; }

        ICodeWriter Header2Writer { get; }

        ICodeWriter Header3Writer { get; }

        string Generate(LanguageType type);

        IOption<string> GetDestructor(LanguageType type);

        CBlock CopyValue(string value, LanguageType type, CodeGenerationContext scope);
    }
}