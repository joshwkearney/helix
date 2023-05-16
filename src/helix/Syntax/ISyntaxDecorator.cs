using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Generation;
using Helix.Generation.Syntax;

namespace Helix.Syntax {
    public interface ISyntaxDecorator {
        public void PreCheckTypes(ISyntaxTree syntax, TypeFrame types) { }

        public void PostCheckTypes(ISyntaxTree syntax, TypeFrame types) { }

        public void PreAnalyzeFlow(ISyntaxTree syntax, FlowFrame flow) { }

        public void PostAnalyzeFlow(ISyntaxTree syntax, FlowFrame flow) { }

        public void PreGenerateCode(ISyntaxTree syntax, FlowFrame flow, ICStatementWriter writer) { }

        public void PostGenerateCode(ISyntaxTree syntax, ICSyntax result, FlowFrame flow, ICStatementWriter writer) { }
    }
}
