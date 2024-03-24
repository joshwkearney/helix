using Helix.Common.Types;
using Helix.Common.Types.Visitors;

namespace Helix.MiddleEnd.TypeVisitors
{
    internal class HasDefaultValueVisitor : ITypeVisitor<bool> {
        private readonly AnalysisContext context;

        public HasDefaultValueVisitor(AnalysisContext context) {
            this.context = context;
        }

        public bool VisitArrayType(ArrayType type) => true;

        public bool VisitBoolType(BoolType type) => true;

        public bool VisitFunctionType(FunctionSignature type) {
            throw new NotImplementedException();
        }

        public bool VisitNominalType(NominalType type) {
            return context.Types[type.Name].Accept(this);
        }

        public bool VisitPointerType(PointerType type) => false;

        public bool VisitSingularBoolType(SingularBoolType type) => false;

        public bool VisitSingularUnionType(SingularUnionType type) => type.Value.Accept(this);

        public bool VisitSingularWordType(SingularWordType type) => type.Value == 0;

        public bool VisitStructType(StructSignature type) => type.Members.All(x => x.Type.Accept(this));

        public bool VisitUnionType(UnionSignature type) => type.Members.Count == 0 || type.Members.First().Type.Accept(this);

        public bool VisitVoidType(VoidType type) => true;

        public bool VisitWordType(WordType type) => true;
    }
}
