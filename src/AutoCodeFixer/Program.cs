using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoCodeFixer {
    public static class Program {
        public static async Task<int> Main(string[] args) {
            // Create a root command with some options
            var rootCommand = new RootCommand
            {
                //new Option<int>(
                //    "--int-option",
                //    getDefaultValue: () => 42,
                //    description: "An option whose argument is parsed as an int"),
                //new Option<bool>(
                //    "--bool-option",
                //    "An option whose argument is parsed as a bool"),
                //new Option<FileInfo>(
                //    "--file-option",
                //    "An option whose argument is parsed as a FileInfo")
                new Option<FileInfo>(
                    new string[]{ "--solution", "-s"},
                    "Solution"),
                new Option<FileInfo>(
                    new string[]{"--project", "-p"},
                    "Project")
            };

            var assemblyVersion = typeof(Program).Assembly.GetName().Version;
            rootCommand.Description = $"AutoCodeFix Global Tool Version {assemblyVersion?.ToString()}";

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress +=
                (sender, e) => {
                    e.Cancel = true;
                    cts.Cancel();
                };

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create<FileInfo, FileInfo>(
                async (fileSolution, fileProject) => await RunAsync(fileSolution, fileProject, cts.Token)
            );
            AnalyzerRunnerHelper.Initialize();

            // Parse the incoming args and invoke the handler
            return await rootCommand.InvokeAsync(args);
        }
        public static async Task<int> RunAsync(FileInfo? fileSolution, FileInfo? fileProject, CancellationToken cancellationToken) {
            if (fileSolution is null && fileProject is null) {
                var lstsln = System.IO.Directory.EnumerateFiles(System.Environment.CurrentDirectory, ".sln").ToList();
                var lstcsprj = System.IO.Directory.EnumerateFiles(System.Environment.CurrentDirectory, ".csproj").ToList();
                if (lstsln.Count == 1) {
                } else if (lstsln.Count > 1) {
                } else {
                    if (lstcsprj.Count == 1) {
                    } else if (lstcsprj.Count > 1) {
                    } else {
                        System.Console.Error.WriteLine("Not found");
                        return 1;
                    }
                }

            }
            await Task.Yield();
            Console.WriteLine($"The value for --solution is: {fileSolution?.FullName ?? "null"}");
            Console.WriteLine($"The value for --project is: {fileProject?.FullName ?? "null"}");
            return 0;
        }
    }
}
