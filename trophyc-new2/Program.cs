using Trophy;

var contents = File.ReadAllText("program.trophy");
var parse = new TrophyCompiler(contents).Compile();

Console.WriteLine(parse);