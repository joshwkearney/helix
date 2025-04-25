using Helix.Analysis.Types;
using Helix.Analysis;
using Helix.Features.Primitives;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Predicates;

namespace Helix.Features.FlowControl {
    public record IfParse : IParseSyntax {
        private readonly IParseSyntax cond, iftrue, iffalse;
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }
        
        public bool IsPure { get; }

        public IfParse(
            TokenLocation location,
            IParseSyntax cond,
            IParseSyntax iftrue) {

            this.Location = location;
            this.cond = cond;

            this.iftrue = new BlockParse(
                iftrue.Location, 
                iftrue, 
                new VoidLiteral(iftrue.Location));

            this.iffalse = new VoidLiteral(location);
            this.IsPure = cond.IsPure && iftrue.IsPure;
            this.path = new IdentifierPath("$if" + ifTempCounter++);
        }

        public IfParse(TokenLocation location, IParseSyntax cond, IParseSyntax iftrue,
                        IParseSyntax iffalse) 
            : this(location, cond, iftrue, iffalse, new IdentifierPath("$if" + ifTempCounter++)) {}

        public IfParse(TokenLocation location, IParseSyntax cond, IParseSyntax iftrue,
                        IParseSyntax iffalse, IdentifierPath path) 
            : this(location, cond, iftrue) {

            this.Location = location;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
            this.path = path;
            this.IsPure = cond.IsPure && iftrue.IsPure && iffalse.IsPure;
        }

        public IParseSyntax CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var cond = this.cond.CheckTypes(types).ToRValue(types);
            var condPredicate = ISyntaxPredicate.Empty;

            if (cond.GetReturnType(types) is PredicateBool predBool) {
                condPredicate = predBool.Predicate;
            }

            cond = cond.UnifyTo(PrimitiveType.Bool, types);

            var name = this.path.Segments.Last();
            var iftrueTypes = new TypeFrame(types, name + "T");
            var iffalseTypes = new TypeFrame(types, name + "F");

            var ifTruePrepend = condPredicate.ApplyToTypes(this.cond.Location, iftrueTypes);
            var ifFalsePrepend = condPredicate.Negate().ApplyToTypes(this.cond.Location, iffalseTypes);

            var iftrue = BlockParse.FromMany(this.iftrue.Location, ifTruePrepend.Append(this.iftrue).ToArray());
            var iffalse = FlowControl.BlockParse.FromMany(this.iffalse.Location, ifFalsePrepend.Append(this.iffalse).ToArray());

            iftrue = iftrue.CheckTypes(iftrueTypes).ToRValue(iftrueTypes);
            iffalse = iffalse.CheckTypes(iffalseTypes).ToRValue(iffalseTypes);

            iftrue = iftrue.UnifyFrom(iffalse, types);
            iffalse = iffalse.UnifyFrom(iftrue, types);
            
            var resultType = iftrue.GetReturnType(types);

            var result = new IfParse(
                this.Location,
                cond,
                iftrue,
                iffalse,
                types.Scope.Append(name));

            types.SyntaxTags[result] = new SyntaxTagBuilder(types)
                .WithChildren(cond, iftrue, iffalse)
                .WithReturnType(resultType)
                .Build();

            return result;
        }

        public IParseSyntax ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            return this;
        }
        
        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var affirmList = new List<ICStatement>();
            var negList = new List<ICStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            var affirm = this.iftrue.GenerateCode(types, affirmWriter);
            var neg = this.iffalse.GenerateCode(types, negWriter);

            var tempName = writer.GetVariableName();
            var returnType = this.GetReturnType(types);

            if (returnType != PrimitiveType.Void) {
                affirmWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = affirm
                });

                negWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = neg
                });
            }

            var tempStat = new CVariableDeclaration() {
                Type = writer.ConvertType(returnType, types),
                Name = tempName
            };

            if (affirmList.Any() && affirmList.Last().IsEmpty) {
                affirmList.RemoveAt(affirmList.Count - 1);
            }

            if (negList.Any() && negList.Last().IsEmpty) {
                negList.RemoveAt(negList.Count - 1);
            }

            var expr = new CIf() {
                Condition = this.cond.GenerateCode(types, writer),
                IfTrue = affirmList,
                IfFalse = negList
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.cond.Location.Line}: If statement");

            // Don't bother writing the temp variable if we are returning void
            if (returnType != PrimitiveType.Void) {
                writer.WriteStatement(tempStat);
            }

            writer.WriteStatement(expr);
            writer.WriteEmptyLine();

            if (returnType != PrimitiveType.Void) {
                return new CVariableLiteral(tempName);
            }
            else {
                return new CIntLiteral(0);
            }
        }
    }
}