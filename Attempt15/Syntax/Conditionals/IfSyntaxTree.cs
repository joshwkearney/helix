using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace JoshuaKearney.Attempt15.Syntax.Conditionals {
    public class IfSyntaxTree : ISyntaxTree {
        public ISyntaxTree Condition { get; }

        public ISyntaxTree Affirmative { get; }

        public ISyntaxTree Negative { get; }

        public ITrophyType ExpressionType { get; }

        public ExternalVariablesCollection ExternalVariables { get; }

        public IfSyntaxTree(ISyntaxTree condition, ISyntaxTree affirm, ISyntaxTree neg, ITrophyType returnType) {
            this.Condition = condition;
            this.Affirmative = affirm;
            this.Negative = neg;
            this.ExpressionType = returnType;
            this.ExternalVariables = condition.ExternalVariables
                .Union(affirm.ExternalVariables)
                .Union(neg.ExternalVariables);
        }

        public string GenerateCode(CodeGenerateEventArgs args) {
            // Setup the conditional code
            var condValue = this.Condition.GenerateCode(args);

            // Cleanup counted variables in the condition that are not in either branch
            args.MemoryManager.CleanupMemoryBlock(
                this.Affirmative
                    .ExternalVariables
                    .Union(this.Negative.ExternalVariables)
                    .VariableInfos
                    .Select(x => x.ToIdentifierInfo())
            );

            // Get a temp variable
            string tempName = args.CodeGenerator.GetTempVariableName();

            // Get the temp type
            string tempType = this.Affirmative.ExpressionType.GenerateName(args);

            // Add the temp variable declaration
            args.CodeGenerator.Declaration(tempType, tempName);

            // Keep track of the variables that must be decremented in both branches
            var varsToDecrement = args.MemoryManager.MemoryBlocks.Pop().CountedVariables;
            args.MemoryManager.OpenMemoryBlock();

            // Setup affirmative block
            args.CodeGenerator.CodeBlocks.Push(new List<string>());
            args.MemoryManager.OpenMemoryBlock(varsToDecrement);

            // Remove the return value from the reference counting list
            var affirmValue = this.Affirmative.GenerateCode(args);
            args.MemoryManager.UnregisterVariable(affirmValue, this.Affirmative.ExpressionType);

            // Finish up the block
            args.CodeGenerator.Assignment(tempName, affirmValue);
            args.MemoryManager.CloseMemoryBlock();
            var affirmBlock = args.CodeGenerator.CodeBlocks.Pop();

            // Setup affirmative block
            args.CodeGenerator.CodeBlocks.Push(new List<string>());
            args.MemoryManager.OpenMemoryBlock(varsToDecrement);

            // Remove the return value from the reference counting list
            var negValue = this.Negative.GenerateCode(args);
            args.MemoryManager.UnregisterVariable(negValue, this.Negative.ExpressionType);

            // Finish up the block
            args.CodeGenerator.Assignment(tempName, negValue);
            args.MemoryManager.CloseMemoryBlock();
            var negBlock = args.CodeGenerator.CodeBlocks.Pop();

            // Push the if statement
            args.CodeGenerator.IfStatement(condValue, affirmBlock, negBlock);

            return tempName;
        }

        public bool DoesVariableEscape(string variableName) {
            return this.Affirmative.DoesVariableEscape(variableName) || this.Negative.DoesVariableEscape(variableName);
        }
    }
}
