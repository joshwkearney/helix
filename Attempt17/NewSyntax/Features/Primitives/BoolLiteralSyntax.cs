using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.NewSyntax.Features.Primitives {
    public class BoolLiteralSyntax : IUndeclaredParseTree, IUnresolvedParseTree, 
            IUndeclaredSyntaxTree, IUnresolvedSyntaxTree, IUnflowedTree, ITree {

        public ITree AnalyzeFlow() {
            throw new NotImplementedException();
        }

        public IUnresolvedParseTree DeclareNames() {
            throw new NotImplementedException();
        }

        public IUnresolvedSyntaxTree DeclareTypes() {
            throw new NotImplementedException();
        }

        public void GenerateCode() {
            throw new NotImplementedException();
        }

        public IUndeclaredSyntaxTree ResolveNames() {
            throw new NotImplementedException();
        }

        public IUnflowedTree ResolveTypes() {
            throw new NotImplementedException();
        }
    }
}