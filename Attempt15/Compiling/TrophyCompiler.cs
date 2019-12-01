using JoshuaKearney.Attempt15.Parsing;
using JoshuaKearney.Attempt15.Syntax.Arithmetic;
using JoshuaKearney.Attempt15.Syntax.Functions;
using JoshuaKearney.Attempt15.Syntax.Logic;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JoshuaKearney.Attempt15.Compiling {
    public class TrophyCompiler {
        private readonly Parser parser;

        public TrophyCompiler(string text) {
            this.parser = new Parser(new Lexer(text));
        }

        public string Compile() {
            var funcGen = new FunctionCodeGenerator();
            var codeGen = new CodeGenerator();
            var tupleGen = new TupleCodeGenerator();
            var memory = new MemoryManager(codeGen);

            var unifier = new TypeUnifier(
                ArithmeticUnifiers.Unifiers
                    .Concat(BooleanUnifiers.Unifiers)
                    .Concat(FunctionUnifiers.Unifiers)
            );

            var parseTree = this.parser.Parse();
            var syntaxTree = parseTree.Analyze(new AnalyzeEventArgs(unifier, new Scope()));

            codeGen.CodeBlocks.Push(new List<string>());
            memory.OpenMemoryBlock();
            var resultExpression = syntaxTree.GenerateCode(new CodeGenerateEventArgs(codeGen, funcGen, tupleGen, memory));
            memory.CloseMemoryBlock();

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("#include \"Trophy.h\"");
            sb.AppendLine();
            sb.AppendLine("typedef void (*$Destructor)(void*);");
            sb.AppendLine();

            foreach (string line in codeGen.GlobalCode) {
                sb.AppendLine(line);
            }

            sb.AppendLine("int main() {");
            foreach (var line in codeGen.CodeBlocks.Pop().Select(x => "    " + x)) {
                sb.AppendLine(line);
            }

            sb.Append("    int result = ").Append(resultExpression).AppendLine(";");

            sb.AppendLine("    return 0;");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private class CodeGenerator : ICodeGenerator {
            private int currentTempId = 0;

            public Stack<List<string>> CodeBlocks { get; } = new Stack<List<string>>();

            public List<string> GlobalCode { get; } = new List<string>();

            public string GetTempVariableName() => $"$temp{this.currentTempId++}";
        }
    }
}