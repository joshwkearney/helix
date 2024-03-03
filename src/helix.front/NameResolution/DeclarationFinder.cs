using Helix.Common;
using Helix.Frontend.ParseTree;

namespace Helix.Frontend.NameResolution {
    internal class DeclarationFinder : IParseTreeVisitor<Unit> {
        private readonly Stack<IdentifierPath> scopes = new();
        private readonly DeclarationStore frame;
        private readonly NameMangler mangler;

        public DeclarationFinder(DeclarationStore frame, NameMangler mangler) {
            this.frame = frame;
            this.mangler = mangler;
        }

        private IdentifierPath GetPath(string name) {
            if (!this.scopes.Any()) {
                return new IdentifierPath(name);
            }
            else {
                return this.scopes.Peek().Append(name);
            }
        }

        public Unit VisitFunctionDeclaration(FunctionDeclaration syntax) {
            var path = this.GetPath(syntax.Name);

            if (frame.ContainsDeclaration(path)) {
                throw NameResolutionException.IdentifierDefined(syntax.Location, syntax.Name);
            }

            this.mangler.MangleGlobalName(path);
            this.frame.SetDeclaration(path);

            return Unit.Instance;
        }

        public Unit VisitStructDeclaration(StructDeclaration syntax) {
            var path = this.GetPath(syntax.Name);

            if (frame.ContainsDeclaration(path)) {
                throw NameResolutionException.IdentifierDefined(syntax.Location, syntax.Name);
            }

            this.mangler.MangleGlobalName(path);
            this.frame.SetDeclaration(path);

            return Unit.Instance;
        }

        public Unit VisitUnionDeclaration(UnionDeclaration syntax) {
            var path = this.GetPath(syntax.Name);

            if (frame.ContainsDeclaration(path)) {
                throw NameResolutionException.IdentifierDefined(syntax.Location, syntax.Name);
            }

            this.mangler.MangleGlobalName(path);
            this.frame.SetDeclaration(path);

            return Unit.Instance;
        }

        public Unit VisitArrayLiteral(ArrayLiteral syntax) => Unit.Instance;

        public Unit VisitAs(AsSyntax syntax) => Unit.Instance;

        public Unit VisitAssignment(AssignmentStatement syntax) => Unit.Instance;

        public Unit VisitBinarySyntax(BinarySyntax syntax) => Unit.Instance;

        public Unit VisitBlock(BlockSyntax syntax) => Unit.Instance;

        public Unit VisitBoolLiteral(BoolLiteral syntax) => Unit.Instance;

        public Unit VisitBreak(BreakSyntax syntax) => Unit.Instance;

        public Unit VisitContinue(ContinueSyntax syntax) => Unit.Instance;

        public Unit VisitFor(ForSyntax syntax) => Unit.Instance;

        public Unit VisitIf(IfSyntax syntax) => Unit.Instance;

        public Unit VisitInvoke(InvokeSyntax syntax) => Unit.Instance;

        public Unit VisitIs(IsSyntax syntax) => Unit.Instance;

        public Unit VisitMemberAccess(MemberAccessSyntax syntax) => Unit.Instance;

        public Unit VisitNew(NewSyntax syntax) => Unit.Instance;

        public Unit VisitReturn(ReturnSyntax syntax) => Unit.Instance;

        public Unit VisitUnarySyntax(UnarySyntax syntax) => Unit.Instance;

        public Unit VisitVariableAccess(VariableAccess syntax) => Unit.Instance;

        public Unit VisitVariableStatement(VariableStatement syntax) => Unit.Instance;

        public Unit VisitVoidLiteral(VoidLiteral syntax) => Unit.Instance;

        public Unit VisitWhile(WhileSyntax syntax) => Unit.Instance;

        public Unit VisitWordLiteral(WordLiteral syntax) => Unit.Instance;

        public Unit VisitLoop(LoopSyntax syntax) => Unit.Instance;
    }
}
