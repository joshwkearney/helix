using System.Collections.Generic;

namespace Attempt9 {
    public class FunctionParameter {
        public ITrophyType Type { get; }

        public string Name { get; }
    }

    public class FunctionLiteral {
        public IReadOnlyList<FunctionParameter> Parameters { get; }

        public IParseTree Body { get; }

        public FunctionLiteral(IParseTree body, params FunctionParameter[] pars) {
            this.Body = body;
            this.Parameters = pars;
        }
    }
}