using Helix;
using Helix.Analysis;
using Helix.Analysis.Flow;

namespace helix_tests {
    [TestClass]
    public class LifetimeTests {
        public static IEnumerable<object[]> GoodLifetimeTests { get; private set; }

        public static IEnumerable<object[]> BadLifetimeTests { get; private set; }

        static LifetimeTests() {
            GoodLifetimeTests = GetTests("good_lifetimes.helix")
                .Select(x => new object[] { x })
                .ToArray();

            BadLifetimeTests = GetTests("bad_lifetimes.helix")
                .Select(x => new object[] { x })
                .ToArray();
        }

        [TestMethod]
        [DynamicData(nameof(BadLifetimeTests))]
        public void TestBadLifetimes(string contents) {
            Assert.ThrowsException<HelixException>(() => {
                CompileProgram(contents);
            });
        }

        [TestMethod]
        [DynamicData(nameof(GoodLifetimeTests))]
        public void TestGoodLifetimes(string contents) {
            CompileProgram(contents);
        }

        private static void CompileProgram(string contents) {
            var header = File.ReadAllText("../../../../../Resources/Helix.h");
            var parse = new HelixCompiler(header, contents);

            parse.Compile();
        }

        private static IEnumerable<string> GetTests(string file) {
            return File.ReadAllText("./Programs/" + file)
                .Split("// ###")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToArray();
        }
    }
}