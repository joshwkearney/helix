using Attempt17.CodeGeneration;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Experimental {
    public interface ISyntax<T> {
        T Tag { get; }
    }

    public interface ICodeGenerator {
        ICodeWriter HeaderWriter { get; }

        ICodeWriter ForwardDeclarationsWriter { get; }

        CBlock Generate(ISyntax<TypeCheckInfo> syntax);

        string Generate(LanguageType type);
    }

    public interface ITypeChecker {
        ISyntax<TypeCheckInfo> Check(ISyntax<ParseInfo> syntax, Scope scope);
    }

    public delegate ISyntax<TypeCheckInfo> SyntaxTypeChecker<T>(T syntax, Scope scope, ITypeChecker checker) where T : ISyntax<ParseInfo>;

    public delegate CBlock SyntaxCodeGenerator<T>(T syntax, ICodeGenerator gen) where T : ISyntax<TypeCheckInfo>;

    public interface ISyntaxRegistry {
        void RegisterParseTree<T>(SyntaxTypeChecker<T> typeChecker) where T : ISyntax<ParseInfo>;

        void RegisterSyntaxTree<T>(SyntaxCodeGenerator<T> codeGen) where T : ISyntax<TypeCheckInfo>;
    }

    public interface ILanguageFeature {
        void RegisterSyntax(ISyntaxRegistry registry);
    }
}