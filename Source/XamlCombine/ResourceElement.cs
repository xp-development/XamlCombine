using System.Xml;

namespace XamlCombine
{
  /// <summary>
  ///   Represents XAML resource.
  /// </summary>
  public struct ResourceElement
  {
    public ResourceElement(string key, XmlElement element, string[] usedKeys)
      : this()
    {
      Key = key;
      Element = element;
      UsedKeys = usedKeys;
    }

    /// <summary>
    ///   Resource name.
    /// </summary>
    public string Key { get; }

    /// <summary>
    ///   Resource XML node.
    /// </summary>
    public XmlElement Element { get; }

    /// <summary>
    ///   XAML keys used in this resource.
    /// </summary>
    public string[] UsedKeys { get; }
  }
}