using System.Collections.Generic;

namespace AnalyzeDotNetProject {
  public class SubDependencies {

    public HashSet<string> Projects { get; set; } = new HashSet<string>();

    public List<string> Dependencies { get; set; } = new List<string>();

    public string PackageName { get; set; }

  }
}
