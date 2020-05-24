using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Attempt19.Evaluation;
using Attempt19.Features.Functions;
using Attempt19.Features.Variables;
using Attempt19.Parsing;
using Attempt19.Types;

namespace Attempt19 {
    public enum NameTarget {
        Variable, Function, Struct, Reserved
    }

    public class NameCache<T> {
        private readonly Dictionary<IdentifierPath, T> globalNames
            = new Dictionary<IdentifierPath, T>();

        private readonly Stack<Dictionary<IdentifierPath, T>> localNames
            = new Stack<Dictionary<IdentifierPath, T>>();

        public NameCache() { }

        public void AddGlobalName(IdentifierPath path, T data) {
            this.globalNames.Add(path, data);
        }

        public void AddLocalName(IdentifierPath path, T data) {
            this.localNames.Peek().Add(path, data);
        }

        public void PushLocalFrame() {
            this.localNames.Push(new Dictionary<IdentifierPath, T>());
        }

        public void PopLocalFrame() {
            this.localNames.Pop();
        }

        public bool GetName(IdentifierPath name, out T data) {
            foreach (var frame in this.localNames) {
                if (frame.TryGetValue(name, out data)) {
                    return true;
                }
            }

            return this.globalNames.TryGetValue(name, out data);
        }

        public bool FindName(IdentifierPath scope, string name, out IdentifierPath path, out T data) {
            while (true) {
                path = scope.Append(name);

                if (this.GetName(path, out data)) {
                    return true;
                }

                if (scope.Segments.IsEmpty) {
                    return false;
                }

                scope = scope.Pop();
            }
        }
    } 

    public class TypeChache {
        public Dictionary<IdentifierPath, VariableInfo> Variables { get; }
            = new Dictionary<IdentifierPath, VariableInfo>();

        public Dictionary<IdentifierPath, FunctionSignature> Functions { get; }
            = new Dictionary<IdentifierPath, FunctionSignature>();

        public Dictionary<IdentifierPath, StructSignature> Structs { get; }
            = new Dictionary<IdentifierPath, StructSignature>();

        public Dictionary<LanguageType, Dictionary<string, IdentifierPath>> Methods { get; }
            = new Dictionary<LanguageType, Dictionary<string, IdentifierPath>>();
    }

    public interface IFlowCache {
        public void SetVariableMoved(IdentifierPath variable, bool moved);

        public bool IsVariableMoved(IdentifierPath variable);

        public void RegisterDependency(IdentifierPath dependentVariable,
            IdentifierPath ancestorVariable);

        public IdentifierPath[] GetDependentVariables(IdentifierPath ancestorVariable);

        public IdentifierPath[] GetAncestorVariables(IdentifierPath dependentVariable);

        public IFlowCache Clone();
    }

    public interface ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        void ResolveScope(IdentifierPath containingScope);

        void DeclareNames(NameCache<NameTarget> names);

        void ResolveNames(NameCache<NameTarget> names);

        void DeclareTypes(TypeChache cache);

        ISyntax ResolveTypes(TypeChache types);

        void AnalyzeFlow(TypeChache types, IFlowCache flow);

        void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory);

        IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory);
    }

    class Program {
        static void Main(string[] args) {
            var input = File.ReadAllText("program.txt").Replace("\t", "    ");

            var sw = new Stopwatch();
            sw.Start();

            var lexer = new Lexer(input);
            var tokens = lexer.GetTokens();
            var parser = new Parser(tokens);
            var trees = parser.Parse();

            var scope = new IdentifierPath();
            var names = new NameCache<NameTarget>();
            var types = new TypeChache();
            var flow = new FlowCache();
            var memory = new Dictionary<IdentifierPath, IEvaluateResult>();

            foreach (var tree in trees) {
                tree.ResolveScope(scope);
            }

            foreach (var tree in trees) {
                tree.DeclareNames(names);
            }

            foreach (var tree in trees) {
                tree.ResolveNames(names);
            }

            foreach (var tree in trees) {
                tree.DeclareTypes(types);
            }

            trees = trees.Select(x => x.ResolveTypes(types)).ToArray();

            foreach (var tree in trees) {
                tree.AnalyzeFlow(types, flow);
            }

            foreach (var tree in trees) {
                tree.PreEvaluate(memory);
            }

            sw.Stop();
            Console.WriteLine("Parsed in " + sw.ElapsedMilliseconds + " ms");

            var main = trees
                .Select(x => x as FunctionDeclaration)
                .Where(x => x != null)
                .Where(x => x.Signature.Name == "main")
                .Where(x => x.Signature.Parameters.Length == 0)
                .FirstOrDefault();

            if (main == null) {
                throw new Exception("Can't find main method!");
            }

            var invoke = new FunctionInvoke() {
                Target = new VariableAccessSyntax() {
                    VariableName = "main"
                },
                Arguments = new ISyntax[0],
                TargetSignature = new FunctionSignature() {
                    Name = "main",
                    Parameters = new Parameter[0],
                    ReturnType = main.ReturnType
                }
            };

            invoke.ResolveScope(scope);
            invoke.DeclareNames(names);
            invoke.ResolveNames(names);
            invoke.DeclareTypes(types);
            invoke.ResolveTypes(types);
            invoke.AnalyzeFlow(types, flow);
            invoke.PreEvaluate(memory);

            sw.Restart();
            var result = invoke.Evaluate(memory);
            sw.Stop();

            Console.WriteLine("Evaluated in " + sw.ElapsedMilliseconds + " ms");

            Console.WriteLine("Result: " + result.Value);
            Console.Read();
        }
    }
}
