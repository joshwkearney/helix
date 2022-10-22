using System.Text;

namespace Helix.Generation {
    public static class CHelper {
        public static void Indent(int level, StringBuilder sb) {
            sb.Append(' ', level * 4);
        }
    }
}
