using Helix.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace helix.Syntax.Decorators
{
    public class ShadowingPreventer : ISyntaxDecorator
    {
        public IEnumerable<string> Names { get; }

        public ShadowingPreventer(IEnumerable<string> names)
        {
            Names = names;
        }

        public void PreCheckTypes(ISyntaxTree syntax, EvalFrame types)
        {
            foreach (var name in Names)
            {
                if (types.TryResolveName(syntax.Location.Scope, name, out _))
                {
                    throw TypeCheckingErrors.IdentifierDefined(syntax.Location, name);
                }
            }
        }
    }
}