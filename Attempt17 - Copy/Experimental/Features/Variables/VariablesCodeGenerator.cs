using Attempt17.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt17.Experimental.Features.Variables {
    public class VariablesCodeGenerator {
        public CBlock GenerateVariableAccess(VariableAccessSyntax syntax, ICodeGenerator gen) {
            var name = syntax.VariableInfo.Path.Segments.Last();

            if (syntax.Kind == VariableAccessKind.RemoteAccess) {
                if (syntax.VariableInfo.Source == VariableSource.Alias) {
                    return new CBlock(name);
                }
                else if (syntax.VariableInfo.Source == VariableSource.Local) {
                    return new CBlock(CWriter.AddressOf(name));
                }
            }
            else if (syntax.Kind == VariableAccessKind.ValueAccess) {
                if (syntax.VariableInfo.Source == VariableSource.Alias) {
                    return new CBlock(CWriter.Dereference(name));
                }
                else if (syntax.VariableInfo.Source == VariableSource.Local) {
                    return new CBlock(name);
                }
            }

            throw new Exception("This should never happen");
        }

        public CBlock GenerateVariableInit(VariableInitSyntax<TypeCheckInfo> syntax, ICodeGenerator gen) {
            var value = gen.Generate(syntax.Value);
            var type = gen.Generate(syntax.Tag.ReturnType);

            var writer = new CWriter();
            writer.Lines(value.SourceLines);
            writer.VariableInit(type, syntax.VariableName, value.Value);

            return writer.ToBlock("0");
        }

        public CBlock GenerateStore(StoreSyntax<TypeCheckInfo> syntax, ICodeGenerator gen) {
            var writer = new CWriter();
            var target = gen.Generate(syntax.Target);
            var value = gen.Generate(syntax.Target);

            writer.Lines(value.SourceLines);
            writer.Lines(target.SourceLines);
            writer.VariableAssignment(CWriter.Dereference(target.Value), value.Value);

            return writer.ToBlock("0");
        }
    }
}