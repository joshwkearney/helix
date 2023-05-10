using Helix.Analysis;
using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Features.Memory;
using Helix.Features.Primitives;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Features.Memory {
    public record NewSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly Lifetime lifetime;
        private readonly IReadOnlySet<Lifetime> allowedRoots;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Array.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public NewSyntax(TokenLocation loc, ISyntaxTree target,
            Lifetime lifetime, IReadOnlySet<Lifetime> allowedRoots) {

            this.Location = loc;
            this.target = target;
            this.lifetime = lifetime;
            this.allowedRoots = allowedRoots;
        }

        public ISyntaxTree ToRValue(EvalFrame types) => this;

        public ISyntaxTree CheckTypes(EvalFrame types) {
            types.ReturnTypes[this] = new PointerType(types.ReturnTypes[this.target], true);

            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            this.target.AnalyzeFlow(flow);

            flow.Lifetimes[this] = new LifetimeBundle(this.lifetime);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            var roots = types
                .LifetimeGraph
                .GetDerivedLifetimes(this.lifetime, this.allowedRoots)
                .ToValueList();

            if (roots.Any() && roots.Any(x => !this.allowedRoots.Contains(x))) {
                throw new LifetimeException(
                    this.Location,
                    "Lifetime Inference Failed",
                    "The lifetime of this new object allocation has failed because it is " +
                    "dependent on a root that does not exist at this point in the program and " + 
                    "must be calculated at runtime. Please try moving the allocation " + 
                    "closer to the site of its use.");
            }

            var returnType = (PointerType)types.ReturnTypes[this];
            var target = this.target.GenerateCode(types, writer);

            // Register our member paths with the code generator
            foreach (var (relPath, _) in VariablesHelper.GetMemberPaths(returnType, types)) {
                writer.SetMemberPath(this.lifetime.Path, relPath);
            }

            var innerType = writer.ConvertType(returnType.InnerType);
            var pointerTemp = writer.GetVariableName();

            var tempName = writer.GetVariableName();
            ICSyntax allocLifetime;
            ICSyntax pointerExpr;

            if (roots.Any()) {
                // Allocate on the heap
                allocLifetime = writer.GetSmallestLifetime(roots);
                pointerExpr = new CVariableLiteral(tempName);

                writer.WriteEmptyLine();
                writer.WriteComment($"Line {this.Location.Line}: New '{returnType}'");

                writer.WriteStatement(new CVariableDeclaration() {
                    Name = tempName,
                    Type = new CPointerType(innerType),
                    Assignment = new CVariableLiteral($"({innerType.WriteToC()}*)_pool_malloc(_pool, {allocLifetime.WriteToC()}, sizeof({innerType.WriteToC()}))")
                });
            }
            else {
                writer.WriteEmptyLine();
                writer.WriteComment($"Line {this.Location.Line}: New '{returnType}'");

                // Allocate on the stack
                writer.WriteStatement(new CVariableDeclaration() {
                    Name = tempName,
                    Type = innerType,
                    Assignment = new CIntLiteral(0)
                });

                pointerExpr = new CAddressOf() {
                    Target = new CVariableLiteral(tempName)
                };

                allocLifetime = writer.GetLifetime(new Lifetime(new IdentifierPath("$heap"), 0));
            }

            var fatPointerName = writer.GetVariableName();
            var fatPointerType = writer.ConvertType(returnType);

            var fatPointerDecl = new CVariableDeclaration() {
                Name = fatPointerName,
                Type = fatPointerType,
                Assignment = new CCompoundExpression() {
                    Arguments = new ICSyntax[] {
                        pointerExpr,
                        allocLifetime
                    },
                    Type = fatPointerType
                }
            };

            var assignmentDecl = new CAssignment() {
                Left = new CPointerDereference() {
                    Target = new CMemberAccess() {
                        Target = new CVariableLiteral(fatPointerName),
                        MemberName = "data"
                    }
                },
                Right = target
            };

            writer.WriteStatement(fatPointerDecl);
            writer.RegisterLifetime(this.lifetime, new CMemberAccess() { 
                Target = new CVariableLiteral(fatPointerName),
                MemberName = "pool"
            });

            writer.WriteStatement(assignmentDecl);
            writer.WriteEmptyLine();

            return new CVariableLiteral(fatPointerName);
        }
    }
}