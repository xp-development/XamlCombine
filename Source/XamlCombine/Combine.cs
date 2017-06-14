using Microsoft.Build.Utilities;

namespace XamlCombine
{
  public class Combine : Task
  {
    public string SourcePath { get; set; }
    public string TargetPath { get; set; }

    public override bool Execute()
    {
      var combiner = new Combiner(Log);
      combiner.Combine(SourcePath, TargetPath);
      return Log.HasLoggedErrors;
    }
  }
}