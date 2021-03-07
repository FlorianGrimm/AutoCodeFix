using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
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

        public static Task<int> RunAsync(FileInfo? fileSolution, FileInfo? fileProject, CancellationToken cancellationToken)
            => CommandRun.ExecuteAsync(fileSolution, fileProject, cancellationToken);
    }
}
