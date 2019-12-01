using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System.Collections.Immutable;
using System.Linq;

namespace JoshuaKearney.Attempt15.Syntax.Variables {
    public class VariableDeclarationSyntaxTree : ISyntaxTree {
        public string VariableName { get; }

        public ISyntaxTree Assignment { get; }

        public ISyntaxTree Appendix { get; }

        public ITrophyType ExpressionType => this.Appendix.ExpressionType;

        public ExternalVariablesCollection ExternalVariables { get; }

        public bool IsImmutable { get; }

        public VariableDeclarationSyntaxTree(string name, ISyntaxTree assign, bool isImmutable, ISyntaxTree appendix) {
            this.VariableName = name;
            this.Assignment = assign;
            this.Appendix = appendix;
            this.IsImmutable = isImmutable;

            this.ExternalVariables = appendix.ExternalVariables
                .Except(this.VariableName)
                .Union(assign.ExternalVariables);
        }

        public string GenerateCode(CodeGenerateEventArgs args) {
            bool doesEscape = this.Appendix.DoesVariableEscape(this.VariableName);
            var type = this.Assignment.ExpressionType.GenerateName(args);
            var assign = this.Assignment.GenerateCode(args.WithEscape(doesEscape));

            // Store the value in a c variable
            args.CodeGenerator.Declaration(type, this.VariableName, assign);

            // Correctly reference count the new variable
            if (this.Assignment.ExpressionType.IsReferenceCounted && doesEscape) {
                args.MemoryManager.RegisterVariable(this.VariableName, this.Assignment.ExpressionType);

                if (assign.StartsWith("$temp")) {
                    args.MemoryManager.UnregisterVariable(assign, this.Assignment.ExpressionType);
                }
                else {
                    args.MemoryManager.IncrementValue(this.Assignment.ExpressionType, this.VariableName);
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
            if (this.VariableName == variableName) {
                // If the variable is used to create the new variable, and the new variable escapes
                return this.Assignment.DoesVariableEscape(variableName) && this.Appendix.DoesVariableEscape(variableName);
            }
            else if (this.Assignment.DoesVariableEscape(variableName)) {
                // This variable is dependent on the given one, so if either are used the given one escapes
                return this.Appendix.DoesVariableEscape(this.VariableName) || this.Appendix.DoesVariableEscape(variableName);
            }
            else {
                return this.Appendix.DoesVariableEscape(variableName);
            }
        }
    }
}