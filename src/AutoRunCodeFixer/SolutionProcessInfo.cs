using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    public class SolutionProcessInfo {
        public MSBuildWorkspace Workspace { get; }
        public Solution Solution;
        public readonly Dictionary<ProjectId, ProjectProcessInfo> ProjectInfoById;
        public readonly List<ProjectProcessInfo> ProjectInfos;

        public SolutionProcessInfo(MSBuildWorkspace workspace, Solution solution) {
            this.ProjectInfos = new List<ProjectProcessInfo>();
            this.ProjectInfoById = new Dictionary<ProjectId, ProjectProcessInfo>();
            this.Solution = solution;
            this.Workspace = workspace;
        }

        public bool TryGetProjectInfo(ProjectId projectId, [MaybeNullWhen(false)] out ProjectProcessInfo result)
            => this.ProjectInfoById.TryGetValue(projectId, out result);
        
        public ProjectProcessInfo AddProjectInfo(ProjectId projectId, bool includeInProcess) {
            if (this.TryGetProjectInfo(projectId, out var result)) {
                if (includeInProcess) { result.IncludeInProcess = true; }
                // includeInProcess
                return result;
            } else { 
                result = new ProjectProcessInfo(projectId, includeInProcess);
                this.ProjectInfoById.Add(result.ProjectId, result);
                this.ProjectInfos.Add(result);
                return result;
            }
        }
    }
    //
    public class ProjectProcessInfo {
        public ProjectId ProjectId;
        public bool IncludeInProcess;
        public ProjectProcessInfo(ProjectId projectId, bool includeInProcess) {
            this.ProjectId = projectId;
            this.IncludeInProcess = includeInProcess;
        }
    }
}
