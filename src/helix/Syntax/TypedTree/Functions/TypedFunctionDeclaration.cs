using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.Syntax.IR;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Functions;

public record TypedFunctionDeclaration : IDeclaration {
    public TokenLocation Location { get; init; }
        
    public required ITypedStatement Body { get; init; }

    public required FunctionSignature Signature { get; init; }

    public required IdentifierPath Path { get; init; }

    public TypeFrame DeclareNames(TypeFrame names) {
        throw new InvalidOperationException();
    }

    public TypeFrame DeclareTypes(TypeFrame paths) {
        throw new InvalidOperationException();
    }

    public TypeCheckResult<IDeclaration> CheckTypes(TypeFrame types) {
        throw new InvalidOperationException();
    }

    public void GenerateIR(IRWriter writer, IRFrame context) {
        var startBlock = writer.GetBlockName("function_start");
        var endBlock = writer.GetBlockName("function_end");
        var returnLocal = writer.GetName();
            
        writer.PushBlock(startBlock);
        context.PushFunction(endBlock, returnLocal);

        // Declare a variable for the return value if we don't return void
        if (this.Signature.ReturnType != PrimitiveType.Void) {
            writer.CurrentBlock.Add(new CreateLocalInstruction {
                LocalName = returnLocal,
                ReturnType = this.Signature.ReturnType
            });
        }

        foreach (var par in this.Signature.Parameters) {
            var name = writer.GetName(par.Name);
            var path = this.Path.Append(par.Name);
                
            context.SetVariable(path, name);
        }
            
        this.Body.GenerateIR(writer, context);

        // If this is a void function without a return statement, insert a goto at the end
        if (!writer.CurrentBlock.IsTerminated) {
            writer.CurrentBlock.Terminate(new JumpInstruction {
                BlockName = endBlock
            });
        }
            
        writer.PopBlock();
        writer.PushBlock(endBlock);

        if (this.Signature.ReturnType == PrimitiveType.Void) {
            writer.CurrentBlock.Terminate(new ReturnInstruction {
                ReturnValue = new Immediate.Void()
            });
        }
        else {
            writer.CurrentBlock.Terminate(new ReturnInstruction {
                ReturnValue = returnLocal
            });
        }

        writer.PopBlock();
        context.PopFunction();
    }
        
    public void GenerateCode(TypeFrame types, ICWriter writer) {
        writer.ResetTempNames();

        var returnType = this.Signature.ReturnType == PrimitiveType.Void
            ? new CNamedType("void")
            : writer.ConvertType(this.Signature.ReturnType, types);

        var pars = this.Signature
            .Parameters
            .Select((x, i) => new CParameter { 
                Type = writer.ConvertType(x.Type, types),
                Name = writer.GetVariableName(this.Path.Append(x.Name))
            })
            .ToArray();

        var funcName = writer.GetVariableName(this.Path);
        var body = new List<ICStatement>();
        var bodyWriter = new CStatementWriter(writer, body);

        // Register the parameters as local variables
        foreach (var par in this.Signature.Parameters) {
            foreach (var (relPath, _) in par.Type.GetMembers(types)) {
                var path = this.Path.Append(par.Name).Append(relPath);

                bodyWriter.VariableKinds[path] = CVariableKind.Local;
            }
        }

        // Generate the body
        this.Body.GenerateCode(types, bodyWriter);

        // If the body ends with an empty line, trim it
        if (body.Any() && body.Last().IsEmpty) {
            body = body.SkipLast(1).ToList();
        }

        var decl = new CFunctionDeclaration {
            ReturnType = returnType,
            Name = funcName,
            Parameters = pars,
            Body = body
        };

        var forwardDecl = new CFunctionDeclaration {
            ReturnType = returnType,
            Name = funcName,
            Parameters = pars
        };

        writer.WriteDeclaration2(forwardDecl);
        writer.WriteDeclaration4(decl);
        writer.WriteDeclaration4(new CEmptyLine());
    }
}