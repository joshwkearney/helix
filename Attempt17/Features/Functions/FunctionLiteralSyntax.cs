using Attempt17.TypeChecking;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Functions {
    public class FunctionLiteralSyntax : ISyntax<TypeCheckTag> {
        public TypeCheckTag Tag { get; }

        public FunctionLiteralSyntax(TypeCheckTag tag) {
            this.Tag = tag;
        }
    }
}