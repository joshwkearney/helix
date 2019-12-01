using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Generation {
    public class ExpressionCGProvider : ISyntaxVisitor<IExpressionCodeGenerator> {
        private readonly IExpressionCodeGenerator intLiteralBehavior = new IntLiteralGCBehavior();
        private readonly BinaryExpressionGCBehavior binaryExpressionBehavior;
        private readonly BlockGCBehavior blockBehavior;
        private readonly ValueofCGBehavior valueofBehavior;
        private readonly FunctionCallGCBehavior functionCallBehavior;
        private readonly IfGCBehavior ifGCBehavior;
        private readonly StoreGCBehavior storeBehavior;
        private readonly VariableLiteralCGBehavior variableLiteralBehavior;
        private readonly VariableLocationLiteralCGBehavior variableLocationLiteralCGBehavior;
        private readonly WhileCGBehavior whileBehavior;
        private readonly VariableInitializationCGBehavior variableInitializationBehavior;
        private readonly StructInitializationCGBehavior structInitiazationBehavior;
        private readonly MemberAccessCGBehavior memberAccessBehavior;

        public ExpressionCGProvider(TypeGenerator typeGen) {
            this.binaryExpressionBehavior = new BinaryExpressionGCBehavior(this, typeGen);
            this.blockBehavior = new BlockGCBehavior(this, typeGen);
            this.valueofBehavior = new ValueofCGBehavior(this, typeGen);
            this.functionCallBehavior = new FunctionCallGCBehavior(this, typeGen);
            this.ifGCBehavior = new IfGCBehavior(this, typeGen);
            this.storeBehavior = new StoreGCBehavior(this, typeGen);
            this.variableLiteralBehavior = new VariableLiteralCGBehavior(this, typeGen);
            this.variableLocationLiteralCGBehavior = new VariableLocationLiteralCGBehavior(this, typeGen);
            this.whileBehavior = new WhileCGBehavior(this, typeGen);
            this.variableInitializationBehavior = new VariableInitializationCGBehavior(this, typeGen);
            this.structInitiazationBehavior = new StructInitializationCGBehavior(this, typeGen);
            this.memberAccessBehavior = new MemberAccessCGBehavior(this, typeGen);
        }

        public IExpressionCodeGenerator VisitBinaryExpression(BinaryExpression syntax) => this.binaryExpressionBehavior;

        public IExpressionCodeGenerator VisitBlock(BlockSyntax syntax) => this.blockBehavior;

        public IExpressionCodeGenerator VisitValueof(ValueofSyntax syntax) => this.valueofBehavior;

        public IExpressionCodeGenerator VisitFunctionCall(FunctionCallSyntax syntax) => this.functionCallBehavior;

        public IExpressionCodeGenerator VisitIf(IfSyntax syntax) => this.ifGCBehavior;

        public IExpressionCodeGenerator VisitIntLiteral(IntLiteral syntax) => this.intLiteralBehavior;

        public IExpressionCodeGenerator VisitStore(StoreSyntax syntax) => this.storeBehavior;

        public IExpressionCodeGenerator VisitVariableLiteral(VariableLiteral syntax) => this.variableLiteralBehavior;

        public IExpressionCodeGenerator VisitVariableLocationLiteral(VariableLocationLiteral syntax) => this.variableLocationLiteralCGBehavior;

        public IExpressionCodeGenerator VisitWhileStatement(WhileStatement syntax) => this.whileBehavior;

        public IExpressionCodeGenerator VisitVariableInitialization(VariableStatement syntax) => this.variableInitializationBehavior;

        public IExpressionCodeGenerator VisitStructInitialization(StructInitializationSyntax syntax) => this.structInitiazationBehavior;

        public IExpressionCodeGenerator VisitMemberAccessSyntax(MemberAccessSyntax syntax) => this.memberAccessBehavior;
    }
}
