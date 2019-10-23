using System.Threading;
#addin "nuget:?package=CodeFileSanity&version=0.0.33"
#addin "nuget:?package=JetBrains.ReSharper.CommandLineTools&version=2019.2.1"
#tool "nuget:?package=NVika.MSBuild&version=1.0.1"
#tool "nuget:?package=Python&version=3.7.2"
var nVikaToolPath = GetFiles("./tools/NVika.MSBuild.*/tools/NVika.exe").First();
var pythonPath = GetFiles("./tools/python.*/tools/python.exe").First();
var waitressPath = pythonPath.GetDirectory().CombineWithFilePath("Scripts/waitress-serve.exe");

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Build");
var configuration = Argument("configuration", "Debug");
var version = "0.0.0";

var rootDirectory = new DirectoryPath("..");
var tempDirectory = new DirectoryPath("temp");
var artifactsDirectory = rootDirectory.Combine("artifacts");

var desktopBuilds = rootDirectory.CombineWithFilePath("build/Desktop.proj");
var desktopSlnf = rootDirectory.CombineWithFilePath("osu-framework.Desktop.slnf");
var frameworkProject = rootDirectory.CombineWithFilePath("osu.Framework/osu.Framework.csproj");
var iosFrameworkProject = rootDirectory.CombineWithFilePath("osu.Framework.iOS/osu.Framework.iOS.csproj");
var androidFrameworkProject = rootDirectory.CombineWithFilePath("osu.Framework.Android/osu.Framework.Android.csproj");
var nativeLibsProject = rootDirectory.CombineWithFilePath("osu.Framework.NativeLibs/osu.Framework.NativeLibs.csproj");

///////////////////////////////////////////////////////////////////////////////
// Setup
///////////////////////////////////////////////////////////////////////////////

IProcess waitressProcess;

Teardown(ctx => {
    waitressProcess?.Kill();
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("DetermineAppveyorBuildProperties")
    .WithCriteria(AppVeyor.IsRunningOnAppVeyor)
    .Does(() => {
        version = $"0.0.{AppVeyor.Environment.Build.Number}";
        configuration = "Debug";
    });

Task("DetermineAppveyorDeployProperties")
    .WithCriteria(AppVeyor.IsRunningOnAppVeyor)
    .Does(() => {
        Environment.SetEnvironmentVariable("APPVEYOR_DEPLOY", "1");

        if (AppVeyor.Environment.Repository.Tag.IsTag)
        {
            AppVeyor.UpdateBuildVersion(AppVeyor.Environment.Repository.Tag.Name);
            version = AppVeyor.Environment.Repository.Tag.Name;
        }

        configuration = "Release";
    });

Task("Clean")
    .Does(() => {
        EnsureDirectoryExists(artifactsDirectory);
        CleanDirectory(artifactsDirectory);
    });

Task("RunHttpBin")
    .WithCriteria(IsRunningOnWindows())
    .Does(() => {
        StartProcess(pythonPath, "-m pip install httpbin waitress");

        waitressProcess = StartAndReturnProcess(waitressPath, new ProcessSettings {
            Arguments = "--listen=*:80 --threads=20 httpbin:app",
        });

        Thread.Sleep(5000); // we need to wait for httpbin to startup. :/

        Environment.SetEnvironmentVariable("LocalHttpBin", "true");
    });

Task("Compile")
    .Does(() => {
        DotNetCoreBuild(desktopBuilds.FullPath, new DotNetCoreBuildSettings {
            Configuration = configuration,
            Verbosity = DotNetCoreVerbosity.Minimal,
        });
    });

Task("Test")
    .IsDependentOn("RunHttpBin")
    .IsDependentOn("Compile")
    .Does(() => {
        var testAssemblies = GetFiles(rootDirectory + $"/*.Tests/bin/{configuration}/*/*.Tests.dll");

        var settings = new DotNetCoreVSTestSettings {
            Logger = AppVeyor.IsRunningOnAppVeyor ? "Appveyor" : "trx",
            Parallel = true,
            ToolTimeout = TimeSpan.FromHours(10),
            Settings = new FilePath("vstestconfig.runsettings"),
        };

        DotNetCoreVSTest(testAssemblies, settings);
    });

// windows only because both inspectcore and nvika depend on net45
Task("InspectCode")
    .WithCriteria(IsRunningOnWindows())
    .IsDependentOn("Compile")
    .Does(() => {
        var inspectcodereport = tempDirectory.CombineWithFilePath("inspectcodereport.xml");

        InspectCode(desktopSlnf, new InspectCodeSettings {
            CachesHome = tempDirectory.Combine("inspectcode"),
            OutputFile = inspectcodereport,
            ArgumentCustomization = args => args.Append("--verbosity=WARN")
        });

        int returnCode = StartProcess(nVikaToolPath, $@"parsereport ""{inspectcodereport}"" --treatwarningsaserrors");
        if (returnCode != 0)
            throw new Exception($"inspectcode failed with return code {returnCode}");
    });

Task("CodeFileSanity")
    .Does(() => {
        ValidateCodeSanity(new ValidateCodeSanitySettings {
            RootDirectory = rootDirectory.FullPath,
            IsAppveyorBuild = AppVeyor.IsRunningOnAppVeyor
        });
    });

Task("PackFramework")
    .Does(() => {
        DotNetCorePack(frameworkProject.FullPath, new DotNetCorePackSettings{
            OutputDirectory = artifactsDirectory,
            Configuration = configuration,
            Verbosity = DotNetCoreVerbosity.Quiet,
            ArgumentCustomization = args => {
                args.Append($"/p:Version={version}");
                args.Append($"/p:GenerateDocumentationFile=true");

                return args;
            }
        });
    });

Task("PackiOSFramework")
    .Does(() => {
        MSBuild(iosFrameworkProject, new MSBuildSettings {
            Restore = true,
            BinaryLogger = new MSBuildBinaryLogSettings{
                Enabled = true,
                FileName = tempDirectory.CombineWithFilePath("msbuildlog.binlog").FullPath
            },
            Verbosity = Verbosity.Minimal,
            ArgumentCustomization = args =>
            {
                args.Append($"/p:Configuration={configuration}");
                args.Append($"/p:Version={version}");
                args.Append($"/p:PackageOutputPath={artifactsDirectory.MakeAbsolute(Context.Environment)}");

                return args;
            }
        }.WithTarget("Pack"));
    });

Task("PackAndroidFramework")
    .Does(() => {
        MSBuild(androidFrameworkProject, new MSBuildSettings {
            Restore = true,
            BinaryLogger = new MSBuildBinaryLogSettings{
                Enabled = true,
                FileName = tempDirectory.CombineWithFilePath("msbuildlog.binlog").FullPath
            },
            Verbosity = Verbosity.Minimal,
            ArgumentCustomization = args =>
            {
                args.Append($"/p:Configuration={configuration}");
                args.Append($"/p:Version={version}");
                args.Append($"/p:PackageOutputPath={artifactsDirectory.MakeAbsolute(Context.Environment)}");

                return args;
            }
        }.WithTarget("Pack"));
    });

Task("PackNativeLibs")
    .Does(() => {
        DotNetCorePack(nativeLibsProject.FullPath, new DotNetCorePackSettings{
            OutputDirectory = artifactsDirectory,
            Configuration = configuration,
            Verbosity = DotNetCoreVerbosity.Quiet,
            ArgumentCustomization = args => {
                args.Append($"/p:Version={version}");
                args.Append($"/p:GenerateDocumentationFile=true");

                return args;
            }
        });
    });

Task("Publish")
    .WithCriteria(AppVeyor.IsRunningOnAppVeyor)
    .Does(() => {
        foreach (var artifact in GetFiles(artifactsDirectory.CombineWithFilePath("*").FullPath))
            AppVeyor.UploadArtifact(artifact);
    });

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("DetermineAppveyorBuildProperties")
    .IsDependentOn("CodeFileSanity")
    .IsDependentOn("InspectCode")
    .IsDependentOn("Test")
    .IsDependentOn("DetermineAppveyorDeployProperties")
    .IsDependentOn("PackFramework")
    .IsDependentOn("PackiOSFramework")
    .IsDependentOn("PackAndroidFramework")
    .IsDependentOn("PackNativeLibs")
    .IsDependentOn("Publish");

Task("DeployFramework")
    .IsDependentOn("Clean")
    .IsDependentOn("DetermineAppveyorDeployProperties")
    .IsDependentOn("PackFramework")
    .IsDependentOn("PackiOSFramework")
    .IsDependentOn("PackAndroidFramework")
    .IsDependentOn("Publish");

Task("DeployNativeLibs")
    .IsDependentOn("Clean")
    .IsDependentOn("DetermineAppveyorDeployProperties")
    .IsDependentOn("PackNativeLibs")
    .IsDependentOn("Publish");

RunTarget(target);;