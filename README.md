<h1 align="center">
    LocBaml/BamlLocalizer for WPF on .NET Core 
</h1>
<div align="center">
    <i align="center">Taken from the original LocBaml and mostly rewritten</i>

![GitHub language count](https://img.shields.io/github/languages/count/h3xds1nz/LocBamlCore?style=plastic) ![GitHub top language](https://img.shields.io/github/languages/top/h3xds1nz/LocBamlCore?style=plastic)
</div>

### Preface
___
* This is an updated version of the ```LocBaml``` tool, originally taken from [Microsoft WPF Samples](https://github.com/microsoft/WPF-Samples/)
* Tested with resources using both ```ResourceReader``` and ```DeserializingResourceReader```
* Currently the expected input is a satellite resource assembly with generated .resources stream
* Base information can be found on [MSDN](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/how-to-localize-an-application?view=netframeworkdesktop-4.8) but I'd for recommend doing ```x:uid``` manually
* Currently only CLI variant is available but I plan to create a simple GUI editor as well
* The assemblies are currently loaded in ```AssemblyLoadContext.Default```, this is required for ```BamlReader```

### Usage (Parsing neutral assembly)
* Put ```LocBamlCore``` files into the main executable folder (e.g. Debug/Release) folders
* Use the following command-line arguments to generate the translations file:
* ```/parse en-US\WpfAppDemo.resources.dll /out:ExtractUIDs.csv /verbose```
* In most cases, your ```BAML``` streams depend on controls/types outside standard WPF assemblies
* Therefore, pass paths to those assemblies using ```/asmpath:WpfAppDemo.dll``` arguments

### Usage (Generating localized assembly)
* Put ```LocBamlCore``` files into the main executable folder (e.g. Debug/Release) folders
* Use the following command-line arguments to generate the satellite assembly:
* ```/generate en-US\WpfAppDemo.resources.dll /translation:ExtractUIDs.csv /culture:no-NO /out:no-NO /verbose```
* In most cases, your ```BAML``` streams depend on controls/types outside standard WPF assemblies
* Therefore, pass paths to those assemblies using ```/asmpath:WpfAppDemo.dll``` arguments
* **Note:** The output directory should be currently created manually beforehand

### Issues
* In case you encounter any issues, please create one in [LocBamlCore - GitHub issues](https://github.com/h3xds1nz/LocBamlCore/issues)
* Do **include** a proper **description** and **reproduction project/files** in case you're creating one

### PRs
* Before creating a PR, please start an ```issue``` explaining what is it you want to fix/improve