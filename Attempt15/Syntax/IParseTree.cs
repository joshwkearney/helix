using JoshuaKearney.Attempt15.Compiling;

namespace JoshuaKearney.Attempt15.Syntax {
    public interface IParseTree {
        ISyntaxTree Analyze(AnalyzeEventArgs args);
    }
}