using System;
using System.Linq;
using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Variables {
    public class VariablesCodeGenerator
        : IVariablesVisitor<CBlock, TypeCheckTag, CodeGenerationContext> {

        public CBlock VisitMove(MoveSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            context.Scope.SetVariableMoved(syntax.VariableName);

            var mover = new ValueMoveVisitor(syntax.VariableName, context.Scope, context.Generator);
            return syntax.Tag.ReturnType.Accept(mover);
        }

        public CBlock VisitStore(StoreSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var writer = new CWriter();
            var target = syntax.Target.Accept(visitor, context);
            var value = syntax.Value.Accept(visitor, context);
            var innerType = context.Generator.Generate(syntax.Value.Tag.ReturnType);

            writer.Lines(value.SourceLines);
            writer.Lines(target.SourceLines);

            writer.Line("// Variable store");

            var destructorOp = context.Generator.GetDestructor(syntax.Value.Tag.ReturnType);
            if (destructorOp.TryGetValue(out var destructor)) {
                writer.Line($"{destructor}({CWriter.Dereference(target.Value, innerType)});");
            }

            writer.VariableAssignment(CWriter.Dereference(target.Value, innerType), value.Value);
            writer.EmptyLine();

            return writer.ToBlock("0");
        }

        public CBlock VisitVariableAccess(VariableAccessSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var name = syntax.VariableInfo.Path.Segments.Last();

            if (syntax.Kind == VariableAccessKind.RemoteAccess) {
                if (syntax.VariableInfo.DefinitionKind == VariableDefinitionKind.Alias) {
                    var varType = new VariableType(syntax.VariableInfo.Type);

                    return context.Generator.CopyValue(name, varType, context);
                }
                else if (syntax.VariableInfo.DefinitionKind == VariableDefinitionKind.Local) {
                    return new CBlock(CWriter.AddressOf(name));
                }
            }
            else if (syntax.Kind == VariableAccessKind.ValueAccess) {
                if (syntax.VariableInfo.DefinitionKind == VariableDefinitionKind.Alias) {
                    var varTypeName = context.Generator.Generate(syntax.VariableInfo.Type);
                    var deref = CWriter.Dereference(name, varTypeName);

                    return context.Generator.CopyValue(deref, syntax.VariableInfo.Type, context);
                }
                else if (syntax.VariableInfo.DefinitionKind == VariableDefinitionKind.Local) {
                    return context.Generator.CopyValue(name, syntax.VariableInfo.Type, context);
                }
            }

            throw new Exception("This should never happen");
        }

        public CBlock VisitVariableInit(VariableInitSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var value = syntax.Value.Accept(visitor, context);
            var type = context.Generator.Generate(syntax.Value.Tag.ReturnType);

            var writer = new CWriter();
            writer.Lines(value.SourceLines);
            writer.Line("// Variable initalization");
            writer.VariableInit(type, syntax.VariableName, value.Value);
            writer.EmptyLine();

            context.Scope.SetVariableUndestructed(syntax.VariableName, syntax.Value.Tag.ReturnType);

            if (value.Value.StartsWith("$")) {
                context.Scope.SetVariableDestructed(value.Value);

                return writer.ToBlock("0");
            }
            else {
                return writer.ToBlock("0");
            }
        }

        public CBlock VisitVariableParseAccess(VariableAccessParseSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            throw new InvalidOperationException();
        }
    }
}
