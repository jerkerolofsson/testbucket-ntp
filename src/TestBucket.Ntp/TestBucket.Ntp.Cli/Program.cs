using Spectre.Console.Cli;
using TestBucket.Ntp.Cli.Commands;

var app = new CommandApp<QueryCommand>();
return await app.RunAsync(args);
