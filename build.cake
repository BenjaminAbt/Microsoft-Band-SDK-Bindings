#tool nuget:?package=XamarinComponent

#addin nuget:?package=Cake.Xamarin
#addin nuget:?package=Cake.FileHelpers

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

FilePath XamarinComponentPath = "./tools/XamarinComponent/tools/xamarin-component.exe";

DirectoryPath outDir = "./output/";
if (!DirectoryExists(outDir)) {
    CreateDirectory(outDir);
}

void Build(string solution)
{
    if (IsRunningOnWindows()) {
        MSBuild(solution, s => s.SetConfiguration(configuration).SetMSBuildPlatform(MSBuildPlatform.x86));
    } else {
        XBuild(solution, s => s.SetConfiguration(configuration));
    }
}

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    var dirs = new [] { 
        "./output",
        // source
        "./packages",
        "./Microsoft.Band/*/bin", 
        "./Microsoft.Band/*/obj", 
        "./Microsoft.Band.Portable/*/bin", 
        "./Microsoft.Band.Portable/*/obj", 
        // samples
        "./Demos/*/packages",
        "./Demos/*/*/bin",
        "./Demos/*/*/obj",
    };
    foreach (var dir in dirs) {
        CleanDirectories(dir);
    }
});

Task("RestorePackages")
    .Does(() =>
{
    var solutions = new [] { 
        // source
        "./Microsoft.Band.sln", 
        "./Microsoft.Band.Portable.sln",
        // native samples
        "./Demos/Microsoft.Band.Sample/Microsoft.Band.Android.Sample.sln",
        "./Demos/Microsoft.Band.Sample/Microsoft.Band.iOS.Sample.sln",
        "./Demos/Microsoft.Band.Sample/Microsoft.Band.Sample.sln",
        // portable samples
        "./Demos/Microsoft.Band.Portable.Sample/Microsoft.Band.Portable.Sample.sln",
        // demos
        "./Demos/RotatingHand/RotatingHandAndroid.sln",
        "./Demos/RotatingHand/RotatingHandWPA.sln",
        "./Demos/RotatingHand/RotatingHand.sln",
    };
    foreach (var solution in solutions) {
        NuGetRestore(solution);
    }
});

Task("Build")
    .IsDependentOn("RestorePackages")
    .Does(() =>
{
    var solutions = new [] { 
        "./Microsoft.Band.sln",
        "./Microsoft.Band.Portable.sln",
    };
    foreach (var solution in solutions) {
        Build(solution);
    }
    
    var outputs = new Dictionary<string, string> { 
        // docs
        { "./README.md", "README.md" },
        { "./LICENSE.md", "LICENSE.md" },
        { "./GettingStarted.md", "GettingStarted.md" },
        // native
        { "./Microsoft.Band/Microsoft.Band.Android/bin/{0}/Microsoft.Band.Android.dll", "android/Microsoft.Band.Android.dll" }, 
        { "./Microsoft.Band/Microsoft.Band.iOS/bin/{0}/Microsoft.Band.iOS.dll", "ios/Microsoft.Band.iOS.dll" },
        // portable
        { "./Microsoft.Band.Portable/Microsoft.Band.Portable.Android/bin/{0}/Microsoft.Band.Portable.dll", "android/Microsoft.Band.Portable.dll" },
        { "./Microsoft.Band.Portable/Microsoft.Band.Portable.Android/bin/{0}/Microsoft.Band.Portable.xml", "android/Microsoft.Band.Portable.xml" },
        { "./Microsoft.Band.Portable/Microsoft.Band.Portable.iOS/bin/{0}/Microsoft.Band.Portable.dll", "ios/Microsoft.Band.Portable.dll" },
        { "./Microsoft.Band.Portable/Microsoft.Band.Portable.iOS/bin/{0}/Microsoft.Band.Portable.xml", "ios/Microsoft.Band.Portable.xml" },
        { "./Microsoft.Band.Portable/Microsoft.Band.Portable.Phone/bin/{0}/Microsoft.Band.Portable.dll", "wpa81/Microsoft.Band.Portable.dll" },
        { "./Microsoft.Band.Portable/Microsoft.Band.Portable.Phone/bin/{0}/Microsoft.Band.Portable.xml", "wpa81/Microsoft.Band.Portable.xml" },
        { "./Microsoft.Band.Portable/Microsoft.Band.Portable.Windows/bin/{0}/Microsoft.Band.Portable.dll", "netcore451/Microsoft.Band.Portable.dll" },
        { "./Microsoft.Band.Portable/Microsoft.Band.Portable.Windows/bin/{0}/Microsoft.Band.Portable.xml", "netcore451/Microsoft.Band.Portable.xml" },
        { "./Microsoft.Band.Portable/Microsoft.Band.Portable.UWP/bin/{0}/Microsoft.Band.Portable.dll", "uap10/Microsoft.Band.Portable.dll" },
        { "./Microsoft.Band.Portable/Microsoft.Band.Portable.UWP/bin/{0}/Microsoft.Band.Portable.xml", "uap10/Microsoft.Band.Portable.xml" },
        { "./Microsoft.Band.Portable/Microsoft.Band.Portable/bin/{0}/Microsoft.Band.Portable.dll", "pcl/Microsoft.Band.Portable.dll" },
        { "./Microsoft.Band.Portable/Microsoft.Band.Portable/bin/{0}/Microsoft.Band.Portable.xml", "pcl/Microsoft.Band.Portable.xml" },
    };
    foreach (var output in outputs) {
        var dest = outDir.CombineWithFilePath(string.Format(output.Value, configuration));
        var dir = dest.GetDirectory();
        if (!DirectoryExists(dir)) {
            CreateDirectory(dir);
        }
        CopyFile(string.Format(output.Key, configuration), dest);
    }
});

Task("BuildSamples")
    .IsDependentOn("RestorePackages")
    .Does(() =>
{
    var solutions = new [] { 
        "./Demos/Microsoft.Band.Sample/Microsoft.Band.Sample.sln",
        "./Demos/Microsoft.Band.Portable.Sample/Microsoft.Band.Portable.Sample.sln",
        "./Demos/RotatingHand/RotatingHand.sln",
    };
    foreach (var solution in solutions) {
        Build(solution);
    }
});

Task("PackageNuGet")
    .IsDependentOn("Build")
    .Does(() =>
{
    var nugets = new [] {
        "./Xamarin.Microsoft.Band.Native.nuspec",
        "./Xamarin.Microsoft.Band.nuspec",
    };
    foreach (var nuget in nugets) {
        NuGetPack(nuget, new NuGetPackSettings {
            OutputDirectory = outDir
        });
    }
});

Task("PackageComponent")
    .IsDependentOn("Build")
    .IsDependentOn("PackageNuGet")
    .IsDependentOn("BuildSamples")
    .Does(() =>
{
    DeleteFiles("./*.xam");
    PackageComponent("./", new XamarinComponentSettings { ToolPath = XamarinComponentPath });
    
    DeleteFiles("./output/*.xam");
    MoveFiles("./*.xam", outDir);
});

Task("Package")
    .IsDependentOn("PackageNuGet")
    .IsDependentOn("PackageComponent")
    .Does(() =>
{
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Package");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);