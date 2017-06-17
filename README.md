XamlCombine
===========
[![Build status](https://ci.appveyor.com/api/projects/status/5bc2t7llki11my1o?svg=true)](https://ci.appveyor.com/project/jan-schubert/xamlcombine)

Description
===========
Combines multiple XAML resource dictionaries in one. Replaces DynamicResources to StaticResources. And sort them in order of usage.

Usage
===========
1. Install nuget package: Install-Package XamlCombine
2. Add msbuild task to Target BeforeBuild
<Target Name="BeforeBuild">
  <Combine SourcePath="Generic.txt" TargetPath="Generic.xaml" />
</Target>

Generic.txt contains a list of XAML filenames

Special thanks
===========
The original code was writting by [SableRaven](https://www.codeplex.com/site/users/view/SableRaven) and was copied from [xamlcombine.codeplex.com](https://xamlcombine.codeplex.com/). It was ported to [github](https://github.com/fluentribbon/XamlCombine) by [batzen](https://github.com/batzen).
