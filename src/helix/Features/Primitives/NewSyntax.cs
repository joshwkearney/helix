using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Parsing {
    public partial class Parser {
        private int newTempCounter = 0;

        private ISyntaxTree NewExpression() {
            TokenLocation start;
            //  bool isStackAllocated;

            //if (this.Peek(TokenKind.NewKeyword)) {
            //    start = this.Advance(TokenKind.NewKeyword).Location;
            //    isStackAllocated = false;
            //}
            //else {
            start = this.Advance(TokenKind.NewKeyword).Location;
            // isStackAllocated = true;
            //}

            var targetType = this.TopExpression();
            var loc = start.Span(targetType.Location);

            return new NewParseSyntax(
                loc, 
                targetType, 
                new IdentifierPath("$new_temp_" + newTempCounter++));
        }
    }
}

namespace Helix.Features.Primitives {
    public record NewParseSyntax : ISyntaxTree {
        private readonly ISyntaxTree type;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.type };

        public bool IsPure => this.type.IsPure;

        public NewParseSyntax(TokenLocation loc, ISyntaxTree type, 
            IdentifierPath tempPath) {

            this.Location = loc;
            this.type = type;
            this.tempPath = tempPath;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            if (!this.type.AsType(types).TryGetValue(out var type)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.type.Location);
            }

            var pointerType = new PointerType(type, true);
            var lifetime = new Lifetime(this.tempPath, 0);
            var result = new NewSyntax(this.Location, pointerType, lifetime, types.LifetimeGraph.AllLifetimes);

            types.ReturnTypes[result] = pointerType;
            types.Lifetimes[result] = new[] { lifetime };

            return result;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record NewSyntax : ISyntaxTree {
        private readonly PointerType returnType;
        private readonly Lifetime lifetime;
        private readonly IReadOnlySet<Lifetime> validRoots;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Array.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public NewSyntax(TokenLocation loc, PointerType type, Lifetime lifetime, IReadOnlySet<Lifetime> validRoots) {
            this.Location = loc;
            this.returnType = type;
            this.lifetime = lifetime;
            this.validRoots = validRoots;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            var roots = types.LifetimeGraph.GetDerivedLifetimes(this.lifetime, this.validRoots).ToValueList();
            if (roots.Any() && !roots.All(x => this.validRoots.Contains(x))) {
                // TODO: Write error message
                throw new Exception("Oops");
            }

            var innerType = writer.ConvertType(this.returnType.InnerType);
            var pointerTemp = writer.GetVariableName();

            var tempName = writer.GetVariableName();
            ICSyntax allocLifetime;
            ICSyntax pointerExpr;

            if (roots.Any()) {
                // Allocate on the heap
                allocLifetime = writer.GetSmallestLifetime(roots);
                pointerExpr = new CVariableLiteral(tempName);

                writer.WriteEmptyLine();
                writer.WriteComment($"Line {this.Location.Line}: New '{this.returnType}'");

                writer.WriteStatement(new CVariableDeclaration() {
                    Name = tempName,
                    Type = new CPointerType(innerType),
                    Assignment = new CVariableLiteral($"({innerType.WriteToC()}*)_pool_malloc(_pool, {allocLifetime.WriteToC()}, sizeof({innerType.WriteToC()}))")
                });
            }
            else {
                writer.WriteEmptyLine();
                writer.WriteComment($"Line {this.Location.Line}: New '{this.returnType}'");

                // Allocate on the stack
                writer.WriteStatement(new CVariableDeclaration() {
                    Name = tempName,
                    Type = innerType,
                    Assignment = new CIntLiteral(0)
                });

                pointerExpr = new CAddressOf() {
                    Target = new CVariableLiteral(tempName)
                };

                allocLifetime = new CIntLiteral(short.MaxValue);
            }

            var fatPointerName = writer.GetVariableName();
            var fatPointerType = writer.ConvertType(this.returnType);

            var fatPointerDecl = new CVariableDeclaration() {
                Name = fatPointerName,
                Type = fatPointerType,
                Assignment = new CCompoundExpression() {
                    Arguments = new ICSyntax[] {
                        pointerExpr,
                        new CIntLiteral(1),
                        allocLifetime
                    }
                }
            };

            writer.WriteStatement(fatPointerDecl);
            writer.RegisterLifetime(this.lifetime, new CMemberAccess() { 
                Target = new CVariableLiteral(fatPointerName),
                MemberName = "pool"
            });

            writer.WriteEmptyLine();

            return new CVariableLiteral(fatPointerName);
        }
    }
}
