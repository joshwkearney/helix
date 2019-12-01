namespace Attempt6.Syntax {
    public interface IProtoSyntaxVisitor {
        void Visit(ProtoBinaryExpression syntax);
        void Visit(Int32Literal syntax);
        void Visit(ProtoVariableDeclaration syntax);
        void Visit(ProtoStatement syntax);
        void Visit(ProtoVariableLiteral syntax);
        void Visit(ProtoVariableStore syntax);
    }
}