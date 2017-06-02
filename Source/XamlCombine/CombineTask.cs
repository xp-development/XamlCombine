using Microsoft.Build.Utilities;

namespace XamlCombine
{
  public class CombineTask : Task
  {
    public string SourcePath { get; set; } = "Generic.txt";
    public string TargetPath { get; set; } = "Generic.xaml";

    public override bool Execute()
    {
      var combiner = new Combiner(Log);
      combiner.Combine(SourcePath, TargetPath);
      return Log.HasLoggedErrors;
    }
  }
}