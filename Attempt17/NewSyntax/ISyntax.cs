using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.NewSyntax {
    public interface IUndeclaredParseTree {
        public IUnresolvedParseTree DeclareNames(IdentifierPath scope, NameCache names);
    }

    public interface IUnresolvedParseTree {
        public IUndeclaredSyntaxTree ResolveNames(NameCache names);
    }

    public interface IUndeclaredSyntaxTree {
        public IUnresolvedSyntaxTree DeclareTypes(TypeCache types);
    }

    public interface IUnresolvedSyntaxTree {
        public IUnflowedTree ResolveTypes(TypeCache types);
    }

    public interface IUnflowedTree {
        public ITree AnalyzeFlow();
    }

    public interface ITree {
        public void GenerateCode();
    }
}
