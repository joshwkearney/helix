namespace Helix.TypeChecking;

public record struct TypeCheckResult<T>(T Result, TypeFrame Types) {
}