using Trophy;

var contents = File.ReadAllText("Resources/Program.trophy");
var parse = new TrophyCompiler(contents).Compile();

Console.WriteLine(parse);