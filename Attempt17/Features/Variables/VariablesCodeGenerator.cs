using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Attempt17.Features.Variables {
    public class VariablesCodeGenerator {
        public CBlock GenerateVariableAccess(VariableAccessSyntax syntax, ICScope scope, ICodeGenerator gen) {
            var name = syntax.VariableInfo.Path.Segments.Last();

            if (syntax.Kind == VariableAccessKind.RemoteAccess) {
                if (syntax.VariableInfo.DefinitionKind == VariableDefinitionKind.Alias) {
                    var varType = new VariableType(syntax.VariableInfo.Type);

                    return gen.CopyValue(name, varType, scope);
                }
                else if (syntax.VariableInfo.DefinitionKind == VariableDefinitionKind.Local) {
                    return new CBlock(CWriter.AddressOf(name));
                }
            }
            else if (syntax.Kind == VariableAccessKind.ValueAccess) {
                if (syntax.VariableInfo.DefinitionKind == VariableDefinitionKind.Alias) {
                    var varTypeName = gen.Generate(syntax.VariableInfo.Type);
                    var deref = CWriter.Dereference(name, varTypeName);

                    return gen.CopyValue(deref, syntax.VariableInfo.Type, scope);
                }
                else if (syntax.VariableInfo.DefinitionKind == VariableDefinitionKind.Local) {
                    return gen.CopyValue(name, syntax.VariableInfo.Type, scope);
                }
            }

            throw new Exception("This should never happen");
        }

        public CBlock GenerateVariableInit(VariableInitSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
            var value = gen.Generate(syntax.Value, scope);
            var type = gen.Generate(syntax.Value.Tag.ReturnType);

            var writer = new CWriter();
            writer.Lines(value.SourceLines);
            writer.Line("// Variable initalization");
            writer.VariableInit(type, syntax.VariableName, value.Value);
            writer.EmptyLine();

            scope.SetVariableUndestructed(syntax.VariableName, syntax.Value.Tag.ReturnType);

            if (value.Value.StartsWith("$")) {
                scope.SetVariableDestructed(value.Value);

                return writer.ToBlock("0");
            }
            else {
                return writer.ToBlock("0");
            }            
        }

        public CBlock GenerateStore(StoreSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
            var writer = new CWriter();
            var target = gen.Generate(syntax.Target, scope);
            var value = gen.Generate(syntax.Value, scope);
            var innerType = gen.Generate(syntax.Value.Tag.ReturnType);

            writer.Lines(value.SourceLines);
            writer.Lines(target.SourceLines);

            writer.Line("// Variable store");

            if (gen.GetDestructor(syntax.Value.Tag.ReturnType).TryGetValue(out var destructor)) {
                writer.Line($"{destructor}({CWriter.Dereference(target.Value, innerType)});");
            }

            writer.VariableAssignment(CWriter.Dereference(target.Value, innerType), value.Value);
            writer.EmptyLine();

            return writer.ToBlock("0");
        }

        public CBlock GenerateMove(MoveSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {           
            scope.SetVariableMoved(syntax.VariableName);

            return syntax.Tag.ReturnType.Accept(new ValueMoveVisitor(syntax.VariableName, gen));
        }
    }
}