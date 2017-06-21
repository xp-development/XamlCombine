using System;
using System.IO;
using Microsoft.Build.Utilities;

namespace XamlCombine
{
  public class Combine : Task
  {
    public string SourcePath { get; set; }
    public string TargetPath { get; set; }

    public override bool Execute()
    {
      try
      {
        var path = Path.GetDirectoryName(BuildEngine.ProjectFileOfTaskNode);
        var combiner = new Combiner();
        combiner.Combine(path);
        return true;
      }
      catch (Exception exception)
      {
        Log.LogErrorFromException(exception);
        return false;
      }
    }
  }
}