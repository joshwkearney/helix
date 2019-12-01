using JoshuaKearney.FileSystem;
using JoshuaKearney.Attempt15.Compiling;
using System.IO;

namespace JoshuaKearney.Attempt15 {
    public class Program {
        public static void Main(string[] args) {
            var file = File.ReadAllText("program.trophy");
            var compiler = new TrophyCompiler(file);

            var outFile = StoragePath.CurrentDirectory + @"..\..\..\..\" + @"CTesting\Program.c";
            var output = compiler.Compile();

            File.WriteAllText(outFile.ToString(), output);
        }
    }
}