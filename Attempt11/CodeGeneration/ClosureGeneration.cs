using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt10 {
    public class GeneratedClosureInfo {
        public string FunctionName { get; }

        public string EnvironmentTypeName { get; }

        public GeneratedClosureInfo(string name, string envName) {
            this.FunctionName = name;
            this.EnvironmentTypeName = envName;
        }
    }

    public partial class CodeGenerator {
        private readonly Dictionary<ClosureTrophyType, GeneratedClosureInfo> closureInfo = new Dictionary<ClosureTrophyType, GeneratedClosureInfo>();

        public string GenerateClosureEnvironmentType(FunctionLiteralSyntax func, int id) {
            if (!func.ClosedVariables.Any()) {
                return "Unit";
            }

            string typeName = "$ClosureEnvironment" + id;

            // Get members
            var members = new List<CMember>();
            foreach (var info in func.ClosedVariables) {
                info.Value.Accept(this);
                string memberTypeName = this.types.Pop();

                members.Add(new CMember(info.Key, memberTypeName));
            }

            // Write struct
            this.globalScope.AddRange(CSyntax.TypedefStruct(typeName, members));
            return typeName;
        }

        public string GenerateClosureFunction(FunctionLiteralSyntax func, string environmentType, int id) {
            // Get return type name
            func.ReturnType.Accept(this);
            string returnTypeName = this.types.Pop();

            // Get the function name
            string funcName = "$ClosureFunction" + id;

            // Create type-name pairs of parameters
            var pars = func.Parameters.Select(x => {
                x.Type.Accept(this);
                return new CMember(x.Name, this.types.Pop());
            })
            .ToArray()
            .Prepend(new CMember("environment", environmentType));

            // Generate the closure-undoing part of the body
            var body = new List<string>();

            if (environmentType != null) {
                // Assign environment variables to local variables
                foreach (var info in func.ClosedVariables) {
                    info.Value.Accept(this);
                    string typeName = this.types.Pop();

                    body.Add(CSyntax.Declaration(typeName, info.Key, "environment." + info.Key));
                }
            }

            // Generate the rest of the body
            this.localScope.Push(body);
            func.Body.Accept(this);

            // Get the return value
            string ret = this.values.Pop();
            this.localScope.Pop();

            // Generate the function
            this.globalScope.AddRange(CSyntax.Function(funcName, returnTypeName, pars, body, ret));

            return funcName;
        }

        public string CreateClosureEnvironment(FunctionLiteralSyntax func, string environmentType) {
            if (!func.ClosedVariables.Any()) {
                return "NULL";
            }

            string envVariable = "$temp" + this.currentTempId++;

            this.localScope.Peek().Add(CSyntax.Declaration(
                environmentType,
                envVariable,
                false
            ));

            // Assign each closed variable
            foreach (var info in func.ClosedVariables) {
                this.localScope.Peek().Add(CSyntax.Assignment(envVariable + "." + info.Key, info.Key));
            }

            return envVariable;
        }

        public void Visit(FunctionLiteralSyntax syntax) {
            int id = this.currentClosureId++;

            string envTypeName = this.GenerateClosureEnvironmentType(syntax, id);
            string func = this.GenerateClosureFunction(syntax, envTypeName, id);

            var info = new GeneratedClosureInfo(func, envTypeName);
            this.closureInfo[(ClosureTrophyType)syntax.ExpressionType] = info;

            string envVariable = null;
            envVariable = this.CreateClosureEnvironment(syntax, info.EnvironmentTypeName);

            this.values.Push(envVariable);
        }

        public void Visit(FunctionInvokeSyntax value) {
            // Get the environment
            value.Target.Accept(this);
            var environment = this.values.Pop();

            // Get the closure
            var info = this.closureInfo[(ClosureTrophyType)value.Target.ExpressionType];

            // Get the arguments
            var args = new List<string>();
            foreach (var arg in value.Arguments) {
                arg.Accept(this);
                args.Add(this.values.Pop());
            }

            // Assign the call to a temp variable
            string tempName = this.CreateTempVariable(value.ExpressionType, CSyntax.FunctionCall(info.FunctionName, args.Prepend(environment)));

            // Push the result
            this.values.Push(tempName);
        }

        public void Visit(ClosureTrophyType value) {
            this.types.Push(this.closureInfo[value].EnvironmentTypeName);
        }
    }
}