using Attempt19.Types;

namespace Attempt19.CodeGeneration {
    public interface ICodeGenerator {
        ICodeWriter Header1Writer { get; }

        ICodeWriter Header2Writer { get; }

        ICodeWriter Header3Writer { get; }

        string Generate(LanguageType type);
    }
}