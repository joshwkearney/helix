using Attempt17.CodeGeneration;
using Attempt17.Features;
using Attempt17.Parsing;
using Attempt17.Types;
using System.Collections.Immutable;

namespace Attempt17 {
    public interface IDeclarationParseTree {
        TokenLocation Location { get; }
        
        Scope ModifyLateralScope(Scope scope);

        void ValidateTypes(Scope scope);

        IDeclarationSyntaxTree Analyze(Scope scope);
    }

    public interface IDeclarationSyntaxTree {
        void GenerateForwardDeclarations(CodeGenerator gen);

        ImmutableList<string> GenerateCode(CodeGenerator gen);
    }

    public interface IParseTree {
        TokenLocation Location { get; }

        ISyntaxTree Analyze(Scope scope);
    }

    public interface ISyntaxTree {
        LanguageType ReturnType { get; }

        ImmutableHashSet<IdentifierPath> CapturedVariables { get; }

        Scope ModifyLateralScope(Scope scope);

        CBlock GenerateCode(CodeGenerator gen);
    }
}
