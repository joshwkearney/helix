namespace Helix.MiddleEnd.Interpreting {
    public interface IValueLocationVisitor<T> {
        public T VisitUnknown(UnknownLocation lvalue);

        public T VisitLocal(NamedLocation lvalue);

        public T VisitMemberAccess(MemberAccessLocation lvalue);
    }
}
