using Helix.Analysis;
using Helix.Generation;
using Helix.Generation.Syntax;

namespace helix.Syntax {
    public interface ISyntaxDecorator {
        public void PreCheckTypes(ISyntaxTree syntax, EvalFrame types) { }

        public void PostCheckTypes(ISyntaxTree syntax, EvalFrame types) { }

        public void PreAnalyzeFlow(ISyntaxTree syntax, FlowFrame flow) { }

        public void PostAnalyzeFlow(ISyntaxTree syntax, FlowFrame flow) { }

        public void PreGenerateCode(ISyntaxTree syntax, FlowFrame flow, ICStatementWriter writer) { }

        public void PostGenerateCode(ISyntaxTree syntax, ICSyntax result, FlowFrame flow, ICStatementWriter writer) { }
    }
}
