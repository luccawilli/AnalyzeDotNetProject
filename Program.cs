using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.ProjectModel;

namespace AnalyzeDotNetProject {
  class Program {
    static void Main(string[] args) {
      // Replace to point to your project or solution
      string projectPath = @"A:\root-git\perigon\PerigonDesktop\Perigon\PerigonDesktop.sln";

      var dependencyGraphService = new DependencyGraphService();
      var dependencyGraph = dependencyGraphService.GenerateDependencyGraph(projectPath);
      Dictionary<string, SubDependencies> packagesWithSubs = new Dictionary<string, SubDependencies>();
      foreach (var project in dependencyGraph.Projects.Where(p => p.RestoreMetadata.ProjectStyle == ProjectStyle.PackageReference)) {
        // Generate lock file
        var lockFileService = new LockFileService();
        var lockFile = lockFileService.GetLockFile(project.FilePath, project.RestoreMetadata.OutputPath);

        List<string> subs = new List<string>();
        foreach (var targetFramework in project.TargetFrameworks.Take(1)) { // todo target framework support is disable at the moment
          var lockFileTargetFramework = lockFile.Targets.FirstOrDefault(t => t.TargetFramework.Equals(targetFramework.FrameworkName));
          if (lockFileTargetFramework == null) {
            continue;
          }
          string targetFrameworkName = targetFramework.FrameworkName.ToString();
          var projectKey = GetProjectNameKey(project.Name, targetFrameworkName);
          if (packagesWithSubs.ContainsKey(projectKey)) {
            packagesWithSubs[project.Name].Projects.Add(projectKey);
            continue;
          }
          foreach (var dependency in targetFramework.Dependencies) {
            var projectLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => library.Name == dependency.Name);

            ReportDependency(projectLibrary, lockFileTargetFramework, packagesWithSubs, targetFrameworkName, projectKey);
            subs.Add(GetDependencyNameKey(projectLibrary, targetFrameworkName));
          }
          SubDependencies subDependencies = new SubDependencies() {
            Dependencies = subs,
            Projects = new HashSet<string> { projectKey },
            PackageName = projectKey,
          };
          packagesWithSubs.Add(projectKey, subDependencies);
        }
      }

      var t = packagesWithSubs.Where(x => x.Value.Dependencies.Any(y => y.Contains("System.Web"))).ToList(); // todo search
      //StringBuilder sb = new StringBuilder();
      //foreach (var dependency in packagesWithSubs) {
      //  dependency.Value.Dependencies.ForEach(x => sb.AppendLine(GetMappingString(dependency.Key, x)));
      //}
      //var result = sb.ToString();
    }

    private static void ReportDependency(LockFileTargetLibrary projectLibrary, LockFileTarget lockFileTargetFramework, Dictionary<string, SubDependencies> packagesWithSubs, string targetFramework, string projectName) {
      string key = GetDependencyNameKey(projectLibrary, targetFramework);
      if (packagesWithSubs.ContainsKey(key)) {
        packagesWithSubs[key].Projects.Add(key);
        return;
      }
      List<string> subs = new List<string>();
      foreach (var childDependency in projectLibrary.Dependencies) {
        var childLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => library.Name == childDependency.Id);
        
        ReportDependency(childLibrary, lockFileTargetFramework, packagesWithSubs, targetFramework, projectName);
        subs.Add(GetDependencyNameKey(childLibrary, targetFramework));
      }
      SubDependencies subDependencies = new SubDependencies() { 
        Dependencies = subs,
        Projects = new HashSet<string> { projectName },
        PackageName = key,
      };
      packagesWithSubs.Add(key, subDependencies);
    }

    private static string GetDependencyNameKey(LockFileTargetLibrary projectLibrary, string targetFramework) {
      return $"{projectLibrary.Name}, v{projectLibrary.Version}";/* + (string.IsNullOrWhiteSpace(targetFramework) ? "" : $"target:{targetFramework}");*/
    }
    private static string GetProjectNameKey(string project, string targetFramework) {
      return $"{project}"; //", "; /*+ (string.IsNullOrWhiteSpace(targetFramework) ? "" : $"target:{targetFramework}");*/
    }


    private static string GetMappingString(string projectName, string referencingProjectName) {
      return $"[{projectName}] -> [{referencingProjectName}]";
    }
  }
}
