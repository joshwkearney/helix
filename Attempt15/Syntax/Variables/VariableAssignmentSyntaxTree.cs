using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;

namespace JoshuaKearney.Attempt15.Syntax.Variables {
    public class VariableAssignmentSyntaxTree : ISyntaxTree {
        public ITrophyType ExpressionType => this.Appendix.ExpressionType;

        public ExternalVariablesCollection ExternalVariables { get; }

        public VariableInfo Variable { get; }

        public ISyntaxTree Assignment { get; }

        public ISyntaxTree Appendix { get; }

        public VariableAssignmentSyntaxTree(VariableInfo var, ISyntaxTree assignment, ISyntaxTree appendix) {
            this.Variable = var;
            this.Assignment = assignment;
            this.Appendix = appendix;
            this.ExternalVariables = assignment.ExternalVariables.Union(appendix.ExternalVariables);
        }

        public string GenerateCode(CodeGenerateEventArgs args) {
            var assign = this.Assignment.GenerateCode(args);

            // Store the value in a c variable
            args.CodeGenerator.Assignment(this.Variable.Name, assign);

            // Correctly reference count the new variable
            if (this.Assignment.ExpressionType.IsReferenceCounted) {
                // Decrement the old value
                args.MemoryManager.DecrementValue(this.Variable.Type, this.Variable.Name);

                if (assign.StartsWith("$temp")) {
                    args.MemoryManager.UnregisterVariable(assign, this.Assignment.ExpressionType);
                }
                else {
                    args.MemoryManager.IncrementValue(this.Assignment.ExpressionType, this.Variable.Name);
                }
            }

            // Cleanup any counted variables in the assignment that are not used in the appendix
            args.MemoryManager.CleanupMemoryBlock(
                this.Appendix.ExternalVariables
                    .VariableInfos
                    .Select(x => x.ToIdentifierInfo())
            );

            return this.Appendix.GenerateCode(args);
        }

        public bool DoesVariableEscape(string variableName) {
            return this.Assignment.DoesVariableEscape(variableName) || this.Appendix.DoesVariableEscape(variableName);
        }
    }
}