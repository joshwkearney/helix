using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attempt4 {
    public interface IAnalyzedSyntax {
        LanguageType ExpressionType { get; }

        void Accept(IAnalyzedSyntaxVisitor visitor);
    }
}