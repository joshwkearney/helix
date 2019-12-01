using Attempt16.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt16.Analysis {
    public class LiteralVariableReplacer : ISyntaxVisitor<ISyntax> {
        public ISyntax VisitBinaryExpression(BinaryExpression syntax) => syntax;

        public ISyntax VisitBlock(BlockSyntax syntax) => syntax;

        public ISyntax VisitFunctionCall(FunctionCallSyntax syntax) => syntax;

        public ISyntax VisitIf(IfSyntax syntax) => syntax;

        public ISyntax VisitIntLiteral(IntLiteral syntax) => syntax;

        public ISyntax VisitMemberAccessSyntax(MemberAccessSyntax syntax) {
            syntax.IsLiteralAccess = true;
            return syntax;
        }

        public ISyntax VisitStore(StoreSyntax syntax) => syntax;

        public ISyntax VisitStructInitialization(StructInitializationSyntax syntax) => syntax;

        public ISyntax VisitValueof(ValueofSyntax syntax) => syntax;

        public ISyntax VisitVariableInitialization(VariableStatement syntax) => syntax;

        public ISyntax VisitVariableLiteral(VariableLiteral syntax) {
            return new VariableLocationLiteral() {
                Source = syntax.Source,
                VariableName = syntax.VariableName
            };
        }

        public ISyntax VisitVariableLocationLiteral(VariableLocationLiteral syntax) => syntax;

        public ISyntax VisitWhileStatement(WhileStatement syntax) => syntax;
    }
}