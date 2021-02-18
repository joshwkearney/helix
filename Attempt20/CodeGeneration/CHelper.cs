using System.Text;

namespace Attempt20.CodeGeneration {
    public static class CHelper {
        public static void Indent(int level, StringBuilder sb) {
            sb.Append(' ', level * 4);
        }
    }
}
