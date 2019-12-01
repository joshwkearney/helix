using JoshuaKearney.Attempt15.Compiling;

namespace JoshuaKearney.Attempt15.Types {
    public enum TrophyTypeKind {
        Int = 11,
        Float = 13,
        Function = 17,
        FunctionInterface = 19,
        Boolean = 23,
        Tuple = 29
    }

    public interface ITrophyType {
        bool IsReferenceCounted { get; }

        TrophyTypeKind Kind { get; }

        string GenerateName(CodeGenerateEventArgs args);
    }
}