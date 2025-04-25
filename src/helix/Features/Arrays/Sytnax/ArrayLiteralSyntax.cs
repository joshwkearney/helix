using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Arrays {
    public record ArrayLiteralSyntax : ISyntax {
        public required TokenLocation Location { get; init; }

        public required IReadOnlyList<ISyntax> Arguments { get; init; }
        
        public required ArrayType ArraySignature { get; init; }
        
        public required bool AlwaysJumps { get; init; }

        public HelixType ReturnType => this.ArraySignature;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            writer.WriteComment($"Line {this.Location.Line}: Array literal");

            return this.GenerateStackCode(types, writer);
        }

        /*private ICSyntax GenerateRegionCode(TypeFrame types, ICStatementWriter writer) {
            var args = this.args.Select(x => x.GenerateCode(types, writer)).ToArray();
            var helixArrayType = (ArrayType)this.GetReturnType(types);

            var cArrayType = writer.ConvertType(helixArrayType, types);
            var cInnerType = writer.ConvertType(helixArrayType.InnerType, types);

            var backingName = writer.GetVariableName();
            var tempName = writer.GetVariableName(this.tempPath);

            var backingAssign = new CVariableDeclaration() {
                Name = backingName,
                Type = new CPointerType(cInnerType),
                Assignment = new CRegionAllocExpression() {
                    Type = cInnerType,
                    Amount = args.Length,
                    Lifetime = cLifetime
                }
            };

            writer.WriteStatement(backingAssign);

            for (uint i = 0; i < args.Length; i++) {
                var arg = args[i];

                var argAssign = new CAssignment() {
                    Left = new CIndexExpression() {
                        Target = new CVariableLiteral(backingName),
                        Index = new CIntLiteral(i)
                    },
                    Right = arg
                };

                writer.WriteStatement(argAssign);
            }

            var assign = new CVariableDeclaration() {
                Type = cArrayType,
                Name = tempName,
                Assignment = new CCompoundExpression() {
                    Type = cArrayType,
                    Arguments = new[] {
                            new CVariableLiteral(backingName),
                            cLifetime
                        }
                }
            };

            writer.WriteStatement(assign);
            writer.WriteEmptyLine();

            return new CVariableLiteral(tempName);
        }*/

        private ICSyntax GenerateStackCode(TypeFrame types, ICStatementWriter writer) {
            var args = this.Arguments.Select(x => x.GenerateCode(types, writer)).ToArray();
            var cArrayType = writer.ConvertType(this.ArraySignature, types);
            var cInnerType = writer.ConvertType(this.ArraySignature.InnerType, types);

            var backingName = writer.GetVariableName();
            var tempName = writer.GetVariableName();

            var backingAssign = new CArrayDeclaration {
                Name = backingName,
                ElementType = cInnerType,
                Elements = args
            };

            var assign = new CVariableDeclaration() {
                Type = cArrayType,
                Name = tempName,
                Assignment = new CCompoundExpression() {
                    Type = cArrayType,
                    Arguments = new[] {
                            new CVariableLiteral(backingName),
                            new CVariableLiteral("_return_region")
                        }
                }
            };

            writer.WriteStatement(backingAssign);
            writer.WriteStatement(assign);
            writer.WriteEmptyLine();

            return new CVariableLiteral(tempName);
        }
    }
}