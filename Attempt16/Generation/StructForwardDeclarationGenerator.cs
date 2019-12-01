using Attempt16.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt16.Generation {
    public class StructForwardDeclarationGenerator : IDeclarationVisitor<CCode> {
        public CCode VisitFunctionDeclaration(FunctionDeclaration decl) {
            return new CCode(null);
        }

        public CCode VisitStructDeclaration(StructDeclaration decl) {
            return new CCode(
                null,
                new string[0],
                new[] { "typedef struct " + decl.Name + " " + decl.Name + ";", "" }
            );
        }
    }
}
