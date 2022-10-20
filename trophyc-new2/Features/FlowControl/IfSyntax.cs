using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;
using Trophy.Features.Variables;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree IfExpression(BlockBuilder block) {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression(block);
            var returnName = block.GetTempName();
            var loc = start.Location.Span(cond.Location);

            this.Advance(TokenKind.ThenKeyword);
            var affirmBlock = new BlockBuilder();
            var affirm = this.TopExpression(affirmBlock);

            affirmBlock.Statements.Add(affirm);

            if (this.TryAdvance(TokenKind.ElseKeyword)) {
                var negBlock = new BlockBuilder();
                var neg = this.TopExpression(negBlock);

                negBlock.Statements.Add(neg);
                loc = start.Location.Span(neg.Location);

                var expr = new IfParseSyntax(
                    loc,
                    returnName,
                    cond,
                    new BlockSyntax(loc, affirmBlock.Statements),
                    new BlockSyntax(loc, negBlock.Statements));

                block.Statements.Add(expr);
            }
            else {
                loc = start.Location.Span(affirm.Location);
                var expr = new IfParseSyntax(
                    loc,
                    returnName,
                    cond,
                    new BlockSyntax(loc, affirmBlock.Statements));

                block.Statements.Add(expr);
            }

            return new VariableAccessParseSyntax(loc, returnName);
        }
    }
}

namespace Trophy.Features.FlowControl {
    public record IfParseSyntax : ISyntaxTree {
        private readonly string returnVarName;
        private readonly ISyntaxTree cond, iftrue, iffalse;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.cond, this.iftrue, this.iffalse };

        public IfParseSyntax(TokenLocation location, string returnVar, ISyntaxTree cond, ISyntaxTree iftrue) {
            this.Location = location;
            this.cond = cond;

            this.iftrue = new BlockSyntax(iftrue.Location, new ISyntaxTree[] {
                iftrue, new VoidLiteral(iftrue.Location)
            });

            this.iffalse = new VoidLiteral(location);
            this.returnVarName = returnVar;
        }

        public IfParseSyntax(TokenLocation location, string returnVar, ISyntaxTree cond, 
            ISyntaxTree iftrue, ISyntaxTree iffalse) {

            this.Location = location;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
            this.returnVarName = returnVar;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var cond = this.cond.CheckTypes(types).ToRValue(types).UnifyTo(PrimitiveType.Bool, types);
            var iftrue = this.iftrue.CheckTypes(types).ToRValue(types);
            var iffalse = this.iffalse.CheckTypes(types).ToRValue(types);

            iftrue = iftrue.UnifyFrom(iffalse, types);
            iffalse = iffalse.UnifyFrom(iftrue, types);

            // Declare a variable for this if's return value. The parser will take care of 
            // giving the variable access to other syntax trees
            var sig = new VariableSignature(
                types.CurrentScope.Append(this.returnVarName),
                types.ReturnTypes[iftrue],
                true);

            types.Variables[sig.Path] = sig;
            types.Trees[sig.Path] = new DummySyntax(this.Location);

            var result = new IfSyntax(this.Location, cond, iftrue, iffalse, sig);

            types.ReturnTypes[result] = PrimitiveType.Void;
            return result;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record IfSyntax : ISyntaxTree {
        private readonly VariableSignature returnSig;
        private readonly ISyntaxTree cond, iftrue, iffalse;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.cond, this.iftrue, this.iffalse };

        public IfSyntax(TokenLocation loc, ISyntaxTree cond,
                         ISyntaxTree iftrue,
                         ISyntaxTree iffalse, VariableSignature returnSig) {

            this.Location = loc;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
            this.returnSig = returnSig;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            var affirmList = new List<ICStatement>();
            var negList = new List<ICStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            var affirm = this.iftrue.GenerateCode(affirmWriter);
            var neg = this.iffalse.GenerateCode(negWriter);

            var tempName = writer.GetVariableName(this.returnSig.Path);

            if (this.returnSig.Type != PrimitiveType.Void) {
                affirmWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = affirm
                });

                negWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = neg
                });
            }

            var expr = new CIf() {
                Condition = this.cond.GenerateCode(writer),
                IfTrue = affirmList,
                IfFalse = negList
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.cond.Location.Line}: If statement");

            if (this.returnSig.Type != PrimitiveType.Void) {
                var stat = new CVariableDeclaration() {
                    Type = writer.ConvertType(this.returnSig.Type),
                    Name = tempName
                };

                writer.WriteStatement(stat);
            }

            writer.WriteStatement(expr);
            writer.WriteEmptyLine();

            return new CIntLiteral(0);
        }
    }
}