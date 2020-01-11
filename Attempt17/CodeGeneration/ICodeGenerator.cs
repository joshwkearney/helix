using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.CodeGeneration {
    public interface ICodeGenerator {
        ICodeWriter Header1Writer { get; }

        ICodeWriter Header2Writer { get; }

        ICodeWriter Header3Writer { get; }

        CBlock Generate(ISyntax<TypeCheckTag> syntax, ICScope scope);

        string Generate(LanguageType type);

        IOption<string> GetDestructor(LanguageType type);

        CBlock CopyValue(string value, LanguageType type, ICScope scope);
    }
}