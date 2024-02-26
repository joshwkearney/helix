using Helix.Parsing;

namespace Helix {
    public class HelixException : Exception {
        public string Title { get; }

        public TokenLocation Location { get; }

        public HelixException(TokenLocation location, string title, string message) : base(message) {
            this.Location = location;
            this.Title = title;
        }

        public string CreateConsoleMessage(string file_contents) {
            file_contents = file_contents
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Replace("\t", "    ");

            // Calculate line number and start index on that line
            var lines = file_contents.Split(new[] { "\n" }, StringSplitOptions.None);
            int line = 1 + file_contents.Substring(0, this.Location.StartIndex).Count(x => x == '\n');
            int start = this.Location.StartIndex - lines.Take(line - 1).Select(x => x.Length + 1).Sum();
            int minline = Math.Max(1, line - 2);
            int maxline = Math.Min(lines.Length, line + 2);

            // Calculate line number padding
            int padding = maxline.ToString().Length;
            var format = $"{{0,{padding}:{new string(Enumerable.Repeat('#', padding).ToArray())}}}|";

            // Print preamble
            var message = this.Title + "\n";
            message += this.Message + "\n\n";
            message += $"at 'program.txt' line {line} pos {start}\n";
            message += "\n";

            // Print the previous two lines
            for (int i = minline; i <= line; i++) {
                message += string.Format(format, i) + lines[i - 1] + "\n";
            }

            // Calculate the underlining
            var length = Math.Min(lines[line - 1].Length - start, this.Location.Length);
            var spaces = new string(Enumerable.Repeat(' ', start + padding + 1).ToArray());
            var arrows = new string(Enumerable.Repeat('^', length).ToArray());

            // Print underlining
            message += spaces + arrows + "\n";

            // Print the following two lines
            for (int i = line + 1; i <= maxline; i++) {
                message += string.Format(format, i) + lines[i - 1] + "\n";
            }

            return message;
        }
    }
}