using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoCodeFixer {
    public static class CommandRun {
        public static async Task<int> ExecuteAsync(FileInfo? fileSolution, FileInfo? fileProject, CancellationToken cancellationToken) {
#warning extract and move to lib
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
                SolutionProcessInfo? solutionInfo = null;
                Solution? solution = null;
                Project? project = null;

                if (fileSolution is object) {
                    if (!fileSolution.Exists) {
                        await System.Console.Error.WriteLineAsync($"Solution {fileSolution.FullName} does not exists.");
                        return 1;
                    }
                    solution = await workspace.OpenSolutionAsync(fileSolution.FullName, progress: null, cancellationToken).ConfigureAwait(false);
                    solutionInfo = new SolutionProcessInfo(workspace, solution);
                    foreach (var projectId in solution.GetProjectDependencyGraph().GetTopologicallySortedProjects(cancellationToken)) {
                        solutionInfo.AddProjectInfo(projectId, false);
                    }
                    if (fileProject is object) {
                        var fullNameProject = fileProject.FullName;
                        var lstProjects = solution.Projects.Where(prj => string.Equals(prj.FilePath, fullNameProject, StringComparison.OrdinalIgnoreCase)).ToList();
                        if (lstProjects.Count == 1) {
                            project = lstProjects[0];
                            //lstFilteredProjectId.Add(project.Id);
                            solutionInfo.AddProjectInfo(project.Id, true);
                        } else if (lstProjects.Count > 1) {
                            await System.Console.Error.WriteLineAsync($"For file {fileProject.FullName} found {lstProjects.Count} projects ?? Unexpected.");
                            return 1;
                        } else {
                            await System.Console.Error.WriteLineAsync($"For file {fileProject.FullName} no project found.");
                            return 1;
                        }
                    }
                } else if (fileProject is object) {
                    if (!fileProject.Exists) {
                        await System.Console.Error.WriteLineAsync($"Project {fileProject.FullName} does not exists.");
                        return 1;
                    }
                    project = await workspace.OpenProjectAsync(fileProject.FullName, null, cancellationToken).ConfigureAwait(false);
                    solution = project.Solution;
                    solutionInfo = new SolutionProcessInfo(workspace, solution);
                    solutionInfo.AddProjectInfo(project.Id, true);
                    //lstAllProjectId.Add(project.Id);
                    //lstFilteredProjectId.Add(project.Id);
                } else {
                    await System.Console.Error.WriteLineAsync($"No solution nor project loaded.");
                    return 1;
                }
                await RunLoadAnalzerAsync(solutionInfo, cancellationToken);
            }
            return 0;
        }

        private static async Task RunLoadAnalzerAsync(
            SolutionProcessInfo solutionInfo,
            CancellationToken cancellationToken
            ) {
            foreach (var projectInfo in solutionInfo.ProjectInfos) {
                var project = solutionInfo.Solution.GetProject(projectInfo.ProjectId);
                if (project is null) { continue; }
                //
                string projectLanguage = project.Language;
                // 
                if (project.SupportsCompilation) {
                    if (!((projectLanguage == LanguageNames.CSharp)
                        || (projectLanguage == LanguageNames.VisualBasic))) {
                        continue;
                    }

                    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    if (projectInfo.IncludeInProcess) {
                        //
                        foreach (var analyzerReference in project.AnalyzerReferences) {
                            var analyzers = analyzerReference.GetAnalyzers(projectLanguage);
                            // G:\github\dotnet\roslyn\src\Tools\AnalyzerRunner\DiagnosticAnalyzerRunner.cs
                            var autoRunDiagnosticAnalyzers = analyzers
                                .Where(a => a.GetType().CustomAttributes.Any(ca => ca.AttributeType.FullName == "AutoCodeFixLibrary.AutoRunDiagnosticAnalyzer"))
                                .ToImmutableArray();
                            //
                            //DiagnosticAnalyzer a =analyzers.First();
                            // everythin in Roslyn is internal ... we should call Roysln a internal service
                            //var workspaceAnalyzerOptions = new WorkspaceAnalyzerOptions(project.AnalyzerOptions, project.Solution);
                            //foreach (var analyzer in analyzers) {
                            //    AnalysisContext analysisContext = new AnalysisContext();
                            //    analyzer.Initialize(analysisContext);
                            //    //analyzer.Initialize
                            //}
                        }
                        //
                    } else {
                        //
                        //
                    }
                }
            }
            await Task.Yield();
        }
    }

}
