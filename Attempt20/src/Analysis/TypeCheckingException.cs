using Attempt20.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt20.Analysis {
    public class TypeCheckingException : TrophyException {
        public TypeCheckingException(TokenLocation location, string title, string message) : base(location, title, message) { }
    }
}