using JoshuaKearney.Attempt15.Syntax.Functions;
using JoshuaKearney.Attempt15.Types;
using System.Collections.Generic;
using System.Linq;

namespace JoshuaKearney.Attempt15.Compiling {
    public class FunctionCodeGenerator {
        private int currentFunctionId = 0;
        private int currentFunctionInterfaceId = 0;

        private readonly Dictionary<FunctionType, int> functionIds = new Dictionary<FunctionType, int>();
        private readonly Dictionary<FunctionInterfaceType, int> functionInterfaceIds = new Dictionary<FunctionInterfaceType, int>();
        private readonly Stack<string> functionNames = new Stack<string>();

        public string GenerateFunctionTypeName(FunctionType type) {
            if (this.functionIds.TryGetValue(type, out int id)) {
                return $"$function{id}_environment*";
            }
            else {
                id = this.currentFunctionId++;
                functionIds[type] = id;

                return $"$function{id}_environment*";
            }
        }

        public string GenerateFunctionInterfaceTypeName(FunctionInterfaceType type, CodeGenerateEventArgs args) {
            if (this.functionInterfaceIds.TryGetValue(type, out int id)) {
                return $"$function_interface{id}*";
            }
            else {
                this.GenerateFunctionInterface(type, args);
                return $"$function_interface{functionInterfaceIds[type]}*";
            }
        }

        public string GenerateFunctionLiteral(FunctionLiteralSyntaxTree syntax, CodeGenerateEventArgs args) {
            // Assign the generated type and function to the trophy type
            if (!this.functionIds.TryGetValue(syntax.ExpressionType, out int id)) {
                id = this.currentFunctionId++;
                this.functionIds[syntax.ExpressionType] = id;
            }

            // Generate the environment type
            GenerateFunctionEnvironment(id, syntax, args);

            // Generate the function 
            GenerateFunctionDeclaration(id, syntax, args);

            // Generate a destructor if the environment is reference counted
            GenerateFunctionDecrementor(id, syntax, args);
            GenerateFunctionIncrementor(id, syntax, args);

            // Generate code to instantiate the closure environment
            return GenerateFunctionInstantiation(id, syntax, args);
        }

        public string GenerateFunctionCall(FunctionCallSyntaxTree syntax, CodeGenerateEventArgs args) {
            var funcType = (IFunctionType)syntax.Target.ExpressionType;
            var target = syntax.Target.GenerateCode(args);
            var generatedArgs = syntax.Arguments.Select(x => x.GenerateCode(args)).ToArray();
            var temp = args.CodeGenerator.GetTempVariableName();

            string call;
            if (funcType is FunctionType concreteType) {
                var id = this.functionIds[concreteType];
                call = args.CodeGenerator.FunctionCall(
                    "$function" + id,
                    generatedArgs.Prepend(target)
                );
            }
            else {
                call = args.CodeGenerator.FunctionCall(
                    $"{target}->$function",
                    generatedArgs.Prepend($"{target}->$environment")
                );
            }

            args.CodeGenerator.Declaration(
                syntax.ExpressionType.GenerateName(args),
                temp,
                call
            );

            // Reference count the return value if necessary
            if (funcType.ReturnType.IsReferenceCounted) {
                args.MemoryManager.RegisterVariable(temp, funcType.ReturnType);
            }

            return temp;
        }

        public string GenerateEvoke(EvokeSyntaxTree syntax, CodeGenerateEventArgs args) {
            var generatedArgs = syntax.Arguments
                .Select(x => x.GenerateCode(args))
                .ToArray();

            string funcName = this.functionNames.Peek();
            string temp = args.CodeGenerator.GetTempVariableName();

            string call = args.CodeGenerator.FunctionCall(
                funcName,
                generatedArgs.Prepend("$environment")
            );

            args.CodeGenerator.Declaration(
                syntax.ExpressionType.GenerateName(args),
                temp,
                call
            );

            // Reference count the return value if necessary
            if (syntax.ExpressionType.IsReferenceCounted) {
                args.MemoryManager.RegisterVariable(temp, syntax.ExpressionType);
            }

            return temp;
        }

        public string GenerateFunctionInterfaceInstantiation(FunctionBoxSyntaxTree syntax, CodeGenerateEventArgs args) {
            this.GenerateFunctionInterface(syntax.ExpressionType, args);

            int id = this.functionInterfaceIds[syntax.ExpressionType];
            string temp = args.CodeGenerator.GetTempVariableName();

            // Get the target function
            var target = syntax.Operand.GenerateCode(args);

            // Generate different code based on if an allocation is required
            if (args.DoesValueEscape) {
                args.MemoryManager.RegisterVariable(temp, syntax.ExpressionType);

                // Malloc up the interface
                args.CodeGenerator.Declaration(
                    $"$function_interface{id}*",
                    temp,
                    args.CodeGenerator.Malloc($"$function_interface{id}")
                );

                // Assign the counter variable
                args.CodeGenerator.Assignment(
                    $"{temp}->$counter",
                    "1"
                );

                // Assign the environment
                args.CodeGenerator.Assignment(
                    $"{temp}->$environment",
                    target
                );
                args.MemoryManager.IncrementValue(syntax.Operand.ExpressionType, target);

                // Assign the destructor
                args.CodeGenerator.Assignment(
                    $"{temp}->$destructor",
                    args.MemoryManager.ReferenceDecrementors[syntax.Operand.ExpressionType]
                );

                // Assign the function pointer
                args.CodeGenerator.Assignment(
                    $"{temp}->$function",
                    $"$function{this.functionIds[(FunctionType)syntax.Operand.ExpressionType]}"
                );
            }
            else {
                // Get another temp variable
                var stackPointerTemp = args.CodeGenerator.GetTempVariableName();

                // Allocate the interface on the stack
                args.CodeGenerator.Declaration(
                    $"$function_interface{id}",
                    stackPointerTemp
                );

                // Assign the environment
                args.CodeGenerator.Assignment(
                    $"{stackPointerTemp}.$environment",
                    target
                );

                // Assign the function pointer
                args.CodeGenerator.Assignment(
                    $"{stackPointerTemp}.$function",
                    $"$function{this.functionIds[(FunctionType)syntax.Operand.ExpressionType]}"
                );

                // Assign the counter variable to never be freed
                args.CodeGenerator.Assignment(
                    $"{stackPointerTemp}.$counter",
                    "-1"
                );

                // Make the actual temp variable from the address of the stack position
                args.CodeGenerator.Declaration(
                    $"$function_interface{id}*",
                    temp,
                    "&" + stackPointerTemp
                );
            }            

            return temp;
        }

        private void GenerateFunctionInterface(FunctionInterfaceType faceType, CodeGenerateEventArgs args) {
            if (functionInterfaceIds.ContainsKey(faceType)) {
                return;
            }

            int newId = this.currentFunctionInterfaceId++;

            args.CodeGenerator.TypedefFunctionPointer(
                $"$function_interface{newId}_ptr",
                faceType.ReturnType.GenerateName(args),
                faceType.ArgTypes.Select(x => x.GenerateName(args)).Prepend("void*")
            );

            args.CodeGenerator.TypedefStruct(
                $"$function_interface{newId}",
                new[] {
                    new CMember("$counter", "int"),
                    new CMember("$environment", $"void*"),
                    new CMember("$function", $"$function_interface{newId}_ptr"),
                    new CMember("$destructor", "$Destructor")
                }
            );

            // Create the decrementor
            // Cast the void pointer to the correct type
            args.CodeGenerator.CodeBlocks.Push(new List<string>());
            args.CodeGenerator.Declaration(
                $"$function_interface{newId}*",
                "obj",
                $"($function_interface{newId}*)obj_void"
            );

            // Cleanup pointers
            args.CodeGenerator.Statement("(obj->$counter)--;");
            args.CodeGenerator.IfStatement(
                "obj->$counter == 0",
                new[] {
                    "obj->$destructor(obj->$environment);",
                    "free(obj);"
                }
            );

            // Create the function
            args.CodeGenerator.Function(
                $"$function_interface{newId}_decrementor",
                "void",
                new[] {
                    new CMember("obj_void", $"void*")
                },
                args.CodeGenerator.CodeBlocks.Pop(),
                "",
                CModifier.Inline
            );

            // Add it to the code
            args.MemoryManager.ReferenceDecrementors[faceType] = $"$function_interface{newId}_decrementor";

            // Create the incrementor
            args.CodeGenerator.CodeBlocks.Push(new List<string>());
            args.CodeGenerator.Declaration(
                $"$function_interface{newId}*",
                "obj",
                $"($function_interface{newId}*)obj_void"
            );

            args.CodeGenerator.Statement("(obj->$counter)++;");

            // Create the function
            args.CodeGenerator.Function(
                $"$function_interface{newId}_incrementor",
                "void",
                new[] {
                    new CMember("obj_void", $"void*")
                },
                args.CodeGenerator.CodeBlocks.Pop(),
                "",
                CModifier.Inline
            );

            // Add it to the code
            args.MemoryManager.ReferenceIncrementors[faceType] = $"$function_interface{newId}_incrementor";

            this.functionInterfaceIds[faceType] = newId;
        }

        private void GenerateFunctionEnvironment(int id, FunctionLiteralSyntaxTree syntax, CodeGenerateEventArgs args) {
            List<CMember> members = new List<CMember>();

            // Add all the closed variables
            foreach (var closedVar in syntax.ExpressionType.ClosedVariables) {
                var type = closedVar.Type.GenerateName(args);
                members.Add(new CMember(closedVar.Name, type));
            }

            // Add a reference counter
            members.Add(new CMember("$counter", "int"));

            // The struct holds the closed variables
            args.CodeGenerator.TypedefStruct(
                $"$function{id}_environment",
                members
            );
        }

        private void GenerateFunctionDeclaration(int id, FunctionLiteralSyntaxTree syntax, CodeGenerateEventArgs args) {
            args.MemoryManager.OpenMemoryBlock();
            args.CodeGenerator.CodeBlocks.Push(new List<string>());

            args.CodeGenerator.Assignment(
                $"$function{id}_environment* $environment",
                $"($function{id}_environment*)$environment_void"
            );

            // Assign the closures to the local scope
            foreach (var closedVar in syntax.ExpressionType.ClosedVariables) {
                var type = closedVar.Type.GenerateName(args);

                args.CodeGenerator.Assignment(
                    $"{type} {closedVar.Name}",
                    $"$environment->{closedVar.Name}"
                );
            }

            this.functionNames.Push($"$function{id}");
            var bodyValue = syntax.Body.GenerateCode(args.WithEscape(true));
            this.functionNames.Pop();

            // Don't reference count the return object, but do count everthing else
            args.MemoryManager.UnregisterVariable(bodyValue, syntax.Body.ExpressionType);
            args.MemoryManager.CloseMemoryBlock();

            string returnType = syntax.ExpressionType.ReturnType.GenerateName(args);

            // Generate the entire function
            args.CodeGenerator.Function(
                $"$function{id}",
                returnType,
                syntax.Parameters
                    .Select(x => new CMember(x.Name, x.Type.GenerateName(args)))
                    .Prepend(new CMember("$environment_void", "void*")),
                args.CodeGenerator.CodeBlocks.Pop(),
                bodyValue
            );
        }

        private void GenerateFunctionDecrementor(int id, FunctionLiteralSyntaxTree syntax, CodeGenerateEventArgs args) {
            args.CodeGenerator.CodeBlocks.Push(new List<string>());

            // Get the function type
            string strFuncType = syntax.ExpressionType.GenerateName(args);

            // Decrement the count
            args.CodeGenerator.Declaration(
                strFuncType,
                "obj",
                $"({strFuncType})obj_void"
            );

            args.CodeGenerator.Statement("(obj->$counter)--;");
            args.CodeGenerator.CodeBlocks.Push(new List<string>());

            // Free closed variables that are reference counted
            foreach (var closedVar in syntax.ExpressionType.ClosedVariables) {
                if (closedVar.Type.IsReferenceCounted) {
                    args.MemoryManager.DecrementValue(closedVar.Type, $"obj->{closedVar.Name}");
                }
            }

            args.CodeGenerator
                .Statement("free(obj);")
                .IfStatement("obj->$counter == 0", args.CodeGenerator.CodeBlocks.Pop());

            // Create the destructor function
            string name = $"$function{id}_destructor";

            args.CodeGenerator.Function(
                name,
                "void",
                new[] { new CMember("obj_void", "void*") },
                args.CodeGenerator.CodeBlocks.Pop(),
                "",
                CModifier.Inline
            );

            args.MemoryManager.ReferenceDecrementors[syntax.ExpressionType] = name;
        }

        private void GenerateFunctionIncrementor(int id, FunctionLiteralSyntaxTree syntax, CodeGenerateEventArgs args) {
            // Create the destructor function
            string name = $"$function{id}_incrementor";
            string strFuncType = syntax.ExpressionType.GenerateName(args);

            args.CodeGenerator.Function(
                name,
                "void",
                new[] { new CMember("obj", strFuncType) },
                new List<string>() {
                            "(obj->$counter)++;"
                },
                "",
                CModifier.Inline
            );

            args.MemoryManager.ReferenceIncrementors[syntax.ExpressionType] = name;
        }

        private string GenerateFunctionInstantiation(int id, FunctionLiteralSyntaxTree syntax, CodeGenerateEventArgs args) {
            string tempId = args.CodeGenerator.GetTempVariableName();

            if (args.DoesValueEscape) {
                // Alloc up the environment
                args.CodeGenerator.Declaration(
                    $"$function{id}_environment*",
                    tempId,
                    args.CodeGenerator.Malloc($"$function{id}_environment")
                );

                // Assign the closed variables
                foreach (var closedVar in syntax.ExpressionType.ClosedVariables) {
                    args.CodeGenerator.Assignment($"{tempId}->{closedVar.Name}", closedVar.Name);

                    // Increment the closed variables reference counter
                    if (closedVar.Type.IsReferenceCounted) {
                        args.MemoryManager.IncrementValue(closedVar.Type, closedVar.Name);
                    }
                }

                // Add the temp variable to the reference counting list
                args.MemoryManager.RegisterVariable(tempId, syntax.ExpressionType);

                // Assign the environment's counter
                args.CodeGenerator.Assignment($"{tempId}->$counter", "1");
            }
            else {
                var stackEnvironmentTemp = args.CodeGenerator.GetTempVariableName();
                args.CodeGenerator.Declaration(
                    $"$function{id}_environment",
                    stackEnvironmentTemp
                );

                // Assign the closed variables
                foreach (var closedVar in syntax.ExpressionType.ClosedVariables) {
                    args.CodeGenerator.Assignment($"{stackEnvironmentTemp}.{closedVar.Name}", closedVar.Name);
                }

                // Assign the environment's counter to never be freed
                args.CodeGenerator.Assignment($"{stackEnvironmentTemp}.$counter", "-1");

                args.CodeGenerator.Declaration(
                    $"$function{id}_environment*",
                    tempId,
                    "&" + stackEnvironmentTemp
                );
            }

            return tempId;
        }
    }
}
