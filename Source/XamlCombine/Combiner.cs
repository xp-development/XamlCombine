using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Microsoft.Build.Utilities;

namespace XamlCombine
{
  public class Combiner
  {
    /// <summary>
    ///   Dynamic resource string.
    /// </summary>
    private const string DynamicResourceString = "{DynamicResource ";

    /// <summary>
    ///   Static resource string.
    /// </summary>
    private const string StaticResourceString = "{StaticResource ";

    private readonly Lazy<string> _appPath;

    private readonly TaskLoggingHelper _log;

    public Combiner(TaskLoggingHelper log)
    {
      _log = log;
      _appPath = new Lazy<string>(GetAppPath);
    }

    /// <summary>
    ///   Combines multiple XAML resource dictionaries in one.
    /// </summary>
    /// <param name="sourceFile">Filename of list of XAML's.</param>
    /// <param name="resultFile">Result XAML filename.</param>
    public void Combine(string sourceFile, string resultFile)
    {
      try
      {
        sourceFile = GetFilePath(sourceFile);
        var resources = File.ReadAllLines(sourceFile);

        var finalDocument = new XmlDocument();
        var rootNode = finalDocument.CreateElement("ResourceDictionary",
          "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
        finalDocument.AppendChild(rootNode);

        var keys = new List<string>();
        var resourceElements = new Dictionary<string, ResourceElement>();
        var resourcesList = new List<ResourceElement>();

        foreach (var resource in resources)
        {
          var current = new XmlDocument();
          current.Load(GetFilePath(resource));

          var root = current.DocumentElement;
          if (root == null)
            continue;

          for (var j = 0; j < root.Attributes.Count; j++)
          {
            var attr = root.Attributes[j];
            if (rootNode.HasAttribute(attr.Name))
            {
              if (attr.Value == rootNode.Attributes[attr.Name].Value || attr.Prefix != "xmlns")
                continue;

              const int index = 0;
              string name;
              do
              {
                name = attr.LocalName + "_" + index.ToString(CultureInfo.InvariantCulture);
              } while (rootNode.HasAttribute("xmlns:" + name));

              root.SetAttribute("xmlns:" + name, attr.Value);
              ChangeNamespacePrefix(root, attr.LocalName, name);
              var a = finalDocument.CreateAttribute("xmlns", name, attr.NamespaceURI);
              a.Value = attr.Value;
              rootNode.Attributes.Append(a);
            }
            else
            {
              var exists = false;
              if (attr.Prefix == "xmlns")
              {
                foreach (XmlAttribute attribute in rootNode.Attributes)
                {
                  if (attr.Value != attribute.Value)
                    continue;

                  root.SetAttribute(attr.Name, attr.Value);
                  ChangeNamespacePrefix(root, attr.LocalName, attribute.LocalName);
                  exists = true;
                  break;
                }
              }

              if (exists)
                continue;

              var a = finalDocument.CreateAttribute(attr.Prefix, attr.LocalName, attr.NamespaceURI);
              a.Value = attr.Value;
              rootNode.Attributes.Append(a);
            }
          }

          foreach (XmlNode node in root.ChildNodes)
          {
            if (!(node is XmlElement) || node.Name == "ResourceDictionary.MergedDictionaries")
              continue;

            var importedElement = finalDocument.ImportNode(node, true) as XmlElement;

            var key = string.Empty;
            if (importedElement.HasAttribute("Key"))
              key = importedElement.Attributes["Key"].Value;
            else if (importedElement.HasAttribute("x:Key"))
              key = importedElement.Attributes["x:Key"].Value;
            else if (importedElement.HasAttribute("TargetType"))
              key = importedElement.Attributes["TargetType"].Value;

            if (string.IsNullOrEmpty(key))
              continue;

            if (keys.Contains(key))
              continue;

            keys.Add(key);

            var res = new ResourceElement(key, importedElement, FillKeys(importedElement));
            resourceElements.Add(key, res);
            resourcesList.Add(res);
          }
        }

        var finalOrderList = new List<ResourceElement>();

        for (var i = 0; i < resourcesList.Count; i++)
        {
          if (resourcesList[i].UsedKeys.Length != 0)
            continue;

          finalOrderList.Add(resourcesList[i]);
          resourcesList.RemoveAt(i);
          i--;
        }

        while (resourcesList.Count > 0)
        {
          for (var i = 0; i < resourcesList.Count; i++)
          {
            var containsAll = resourcesList[i].UsedKeys.All(usedKey => !resourceElements.ContainsKey(usedKey) || finalOrderList.Contains(resourceElements[usedKey]));


            if (!containsAll)
              continue;

            finalOrderList.Add(resourcesList[i]);
            resourcesList.RemoveAt(i);
            i--;
          }
        }

        foreach (var resourceElement in finalOrderList)
          rootNode.AppendChild(resourceElement.Element);

        WriteResultFile(Path.Combine(_appPath.Value, resultFile), finalDocument);
      }
      catch (Exception exception)
      {
        _log.LogErrorFromException(exception);
      }
    }

    private static string GetAppPath()
    {
      return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
    }

    private string GetFilePath(string file)
    {
      var filePath = file;

      if (File.Exists(filePath))
        return filePath;

      filePath = Path.Combine(_appPath.Value, filePath);

      if (File.Exists(filePath) == false)
        throw new FileNotFoundException("Unable to find file.", file);

      return filePath;
    }

    /// <summary>
    ///   Changes namespace prefix for XML node.
    /// </summary>
    /// <param name="element">XML node.</param>
    /// <param name="oldPrefix">Old namespace prefix.</param>
    /// <param name="newPrefix">New namespace prefix.</param>
    private static void ChangeNamespacePrefix(XmlElement element, string oldPrefix, string newPrefix)
    {
      // String for search
      var oldString = oldPrefix + ":";
      var newString = newPrefix + ":";
      var oldStringSpaced = " " + oldString;
      var newStringSpaced = " " + newString;

      foreach (XmlNode child in element.ChildNodes)
      {
        var childElement = child as XmlElement;

        if (childElement == null)
          continue;

        if (child.Prefix == oldPrefix)
          child.Prefix = newPrefix;

        foreach (XmlAttribute attr in childElement.Attributes)
        {
          if (attr.Prefix == oldPrefix)
            attr.Prefix = newPrefix;

          if ((attr.Value.Contains("{x:Type") || attr.Value.Contains("{x:Static")) && attr.Value.Contains(oldStringSpaced))
            attr.Value = attr.Value.Replace(oldStringSpaced, newStringSpaced);


          if (attr.Name == "Property" && attr.Value.StartsWith(oldString))
            attr.Value = attr.Value.Replace(oldString, newString);
        }

        ChangeNamespacePrefix(childElement, oldPrefix, newPrefix);
      }
    }

    /// <summary>
    ///   Find all used keys for resource.
    /// </summary>
    /// <param name="element">Xml element which contains resource.</param>
    /// <returns>Array of keys used by resource.</returns>
    private static string[] FillKeys(XmlElement element)
    {
      var result = new List<string>();

      foreach (XmlAttribute attr in element.Attributes)
      {
        if (attr.Value.StartsWith(DynamicResourceString))
        {
          var key = attr.Value.Substring(DynamicResourceString.Length, attr.Value.Length - DynamicResourceString.Length - 1).Trim();
          
          if (result.Contains(key) == false)
            result.Add(key);
        }
        else if (attr.Value.StartsWith(StaticResourceString))
        {
          var key = attr.Value.Substring(StaticResourceString.Length, attr.Value.Length - StaticResourceString.Length - 1).Trim();
          
          if (result.Contains(key) == false)
            result.Add(key);
        }
      }

      foreach (XmlNode node in element.ChildNodes)
      {
        var nodeElement = node as XmlElement;
        if (nodeElement != null)
          result.AddRange(FillKeys(nodeElement));
      }

      return result.ToArray();
    }

    private static void WriteResultFile(string resultFile, XmlDocument finalDocument)
    {
      try
      {
        var tempFile = resultFile + ".tmp";
        finalDocument.Save(tempFile);

        if (File.Exists(resultFile) == false || File.ReadAllText(resultFile) != File.ReadAllText(tempFile))
        {
          File.Copy(tempFile, resultFile, true);
        }

        if (File.Exists(tempFile))
          File.Delete(tempFile);
      }
      catch (Exception e)
      {
        throw new Exception("Error during Resource Dictionary saving: {0}", e);
      }
    }
  }
}