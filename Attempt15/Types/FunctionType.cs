using JoshuaKearney.Attempt15.Compiling;
using System.Collections.Generic;
using System.Linq;

namespace JoshuaKearney.Attempt15.Types {
    public class FunctionType : ITrophyType, IFunctionType {
        public TrophyTypeKind Kind => TrophyTypeKind.Function;

        public ITrophyType ReturnType { get; }

        public IReadOnlyList<ITrophyType> ArgTypes { get; }

        public IReadOnlyList<VariableInfo> ClosedVariables { get; }

        public bool IsReferenceCounted => true;

        public FunctionType(ITrophyType returnType, IEnumerable<ITrophyType> argTypes, IEnumerable<VariableInfo> closed) {
            this.ReturnType = returnType;
            this.ArgTypes = argTypes.ToArray();
            this.ClosedVariables = closed.ToArray();
        }

        public string GenerateName(CodeGenerateEventArgs args) => args.FunctionGenerator.GenerateFunctionTypeName(this);

        public FunctionInterfaceType GetCompatibleInterface() {
            return new FunctionInterfaceType(this.ReturnType, this.ArgTypes);
        }
    }
}