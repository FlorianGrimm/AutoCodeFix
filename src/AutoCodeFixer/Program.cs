using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
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
                string searchPath = System.Environment.CurrentDirectory;
                var lstsln = System.IO.Directory.EnumerateFiles(searchPath, ".sln").ToList();
                var lstcsprj = System.IO.Directory.EnumerateFiles(searchPath, ".csproj").ToList();
                if (lstsln.Count == 1) {
                    fileSolution = new FileInfo(lstcsprj[0]);
                } else if (lstsln.Count > 1) {
                    await System.Console.Error.WriteLineAsync($"{lstsln.Count} solutions found. Plese specify.");
                    return 1;
                } else {
                    if (lstcsprj.Count == 1) {
                        fileProject = new FileInfo(lstcsprj[0]);
                    } else if (lstcsprj.Count > 1) {
                        await System.Console.Error.WriteLineAsync($"{lstsln.Count} (cs) projects found. Plese specify.");
                        return 1;
                    } else {
                        await System.Console.Error.WriteLineAsync($"No solution or project found ín {searchPath}.");
                        return 1;
                    }
                }
            }

            {
                using var workspace = AnalyzerRunnerHelper.CreateWorkspace();
                Solution? solution = null;
                Project? project = null;
                var lstAllProjectId = new List<ProjectId>();
                var lstFilteredProjectId = new List<ProjectId>();

                if (fileSolution is object) {
                    if (!fileSolution.Exists) {
                        await System.Console.Error.WriteLineAsync($"Solution {fileSolution.FullName} does not exists.");
                        return 1;
                    }
                    solution = await workspace.OpenSolutionAsync(fileSolution.FullName, progress: null, cancellationToken).ConfigureAwait(false);
                    if (fileProject is object) {
                        var fullNameProject = fileProject.FullName;
                        var lstProjects = solution.Projects.Where(prj => string.Equals(prj.FilePath, fullNameProject, StringComparison.OrdinalIgnoreCase)).ToList();
                        if (lstProjects.Count == 1) {
                            project = lstProjects[0];
                            lstFilteredProjectId.Add(project.Id);
                        } else if (lstProjects.Count > 1) {
                            await System.Console.Error.WriteLineAsync($"For file {fileProject.FullName} found {lstProjects.Count} projects ?? Unexpected.");
                            return 1;
                        } else {
                            await System.Console.Error.WriteLineAsync($"For file {fileProject.FullName} no project found.");
                            return 1;
                        }
                    }
                    lstAllProjectId.AddRange(solution.GetProjectDependencyGraph().GetTopologicallySortedProjects(cancellationToken));
                } else if (fileProject is object) {
                    if (!fileProject.Exists) {
                        await System.Console.Error.WriteLineAsync($"Project {fileProject.FullName} does not exists.");
                        return 1;
                    }
                    project = await workspace.OpenProjectAsync(fileProject.FullName, null, cancellationToken).ConfigureAwait(false);
                    solution = project.Solution;
                    lstAllProjectId.Add(project.Id);
                    lstFilteredProjectId.Add(project.Id);
                } else {
                    await System.Console.Error.WriteLineAsync($"No solution nor project loaded.");
                    return 1;
                }
                await RunLoadAnalzerAsync(solution, project, lstFilteredProjectId, lstAllProjectId);
            }
            return 0;
        }

        private static async Task RunLoadAnalzerAsync(
                Solution solution,
                Project? project,
                List<ProjectId> lstFilteredProjectId,
                List<ProjectId> lstAllProjectId
            ) {
            foreach (var projectId in lstFilteredProjectId) {
                var prj = solution.GetProject(projectId);
                if (prj is null) { continue; }
                foreach (var analyzerReference in prj.AnalyzerReferences) {
                    var analyzers = analyzerReference.GetAnalyzers(prj.Language);
                    foreach(var analyzer in analyzers) {

                    }
                }
            }
            await Task.Yield();
        }
    }
}
