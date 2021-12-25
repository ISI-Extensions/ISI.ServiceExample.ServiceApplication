#addin nuget:?package=Cake.FileHelpers
#addin nuget:?package=ISI.Cake.AddIn&loaddependencies=true
#addin nuget:?package=Cake.Docker&version=1.0.0

//mklink /D Secrets S:\
var settingsFullName = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("LocalAppData"), "Secrets", "ISI.keyValue");
var settings = GetSettings(settingsFullName);

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var solutionPath = File("./ISI.ServiceExample.ServiceApplication.sln");
var solution = ParseSolution(solutionPath);

var assemblyVersionFile = File("./ISI.ServiceExample.Version.cs");
var rootProjectPath = File("./ISI.ServiceExample.ServiceApplication/ISI.ServiceExample.ServiceApplication.csproj");

var buildDateTime = DateTime.UtcNow;
var buildDateTimeStamp = GetDateTimeStamp(buildDateTime);
var buildRevision = GetBuildRevision(buildDateTime);
var assemblyVersion = GetAssemblyVersion(ParseAssemblyInfo(assemblyVersionFile).AssemblyVersion, buildRevision);
Information("AssemblyVersion: {0}", assemblyVersion);

var nugetPackOutputDirectory = Argument("NugetPackOutputDirectory", "../Nuget");

Task("Clean")
	.Does(() =>
	{
		foreach(var projectPath in solution.Projects.Select(p => p.Path.GetDirectory()))
		{
			Information("Cleaning {0}", projectPath);
			CleanDirectories(projectPath + "/**/bin/" + configuration);
			CleanDirectories(projectPath + "/**/obj/" + configuration);
		}

		Information("Cleaning Projects ...");
	});

Task("NugetPackageRestore")
	.IsDependentOn("Clean")
	.Does(() =>
	{
		Information("Restoring Nuget Packages ...");
		NuGetRestore(solutionPath);
	});

Task("Build")
	.IsDependentOn("NugetPackageRestore")
	.Does(() => 
	{
		CreateAssemblyInfo(assemblyVersionFile, new AssemblyInfoSettings()
		{
			Version = assemblyVersion,
		});

		//Docker Image
		MSBuild(rootProjectPath.Path.FullPath, configurator => configurator
			.SetVerbosity(Verbosity.Quiet)
			.SetConfiguration(configuration)
			.WithProperty("DeployOnBuild", "true")
			.WithProperty("PublishProfile", "FolderProfile.pubxml")
			.SetMaxCpuCount(0)
			.SetNodeReuse(false)
			.WithTarget("Rebuild"));


		//Nuget Package(s)
		MSBuild(solutionPath, configurator => configurator
			.SetConfiguration(configuration)
			.SetVerbosity(Verbosity.Quiet)
			.SetMSBuildPlatform(MSBuildPlatform.Automatic)
			.SetPlatformTarget(PlatformTarget.MSIL)
			.WithTarget("Build"));

		foreach(var project in solution.Projects.Where(project => project.Name.StartsWith("ISI.Services.")))
		{
			Information(project.Name);

			var nuspec = GenerateNuspecFromProject(new ISI.Cake.Addin.Nuget.GenerateNuspecFromProjectRequest()
			{
				ProjectFullName = project.Path.FullPath,
				TryGetPackageVersion = (string package, out string version) =>
				{
					if (package.StartsWith("ISI.Services", StringComparison.InvariantCultureIgnoreCase))
					{
						version = assemblyVersion;
						return true;
					}

					version = string.Empty;
					return false;
				}
			}).Nuspec;
			nuspec.Version = assemblyVersion;
			nuspec.IconUri = GetNullableUri(@"https://nuget.isi-net.com/Images/Lantern.png");
			nuspec.ProjectUri = GetNullableUri(@"https://git.isi-net.com/ISI/ISI.ServiceExample.ServiceApplication");
			nuspec.Title = project.Name;
			nuspec.Description = project.Name;
			nuspec.Copyright = string.Format("Copyright (c) {0}, Integrated Solutions, Inc.", DateTime.Now.Year);
			nuspec.Authors = new [] { "Integrated Solutions, Inc." };
			nuspec.Owners = new [] { "Integrated Solutions, Inc." };

			var nuspecFile = File(project.Path.GetDirectory() + "/" + project.Name + ".nuspec");

			CreateNuspecFile(new ISI.Cake.Addin.Nuget.CreateNuspecFileRequest()
			{
				Nuspec = nuspec,
				NuspecFullName = nuspecFile.Path.FullPath,
			});

			NuGetPack(project.Path.FullPath, new NuGetPackSettings()
			{
				Id = project.Name,
				Version = assemblyVersion, 
				Verbosity = NuGetVerbosity.Detailed,
				Properties = new Dictionary<string, string>
				{
					{ "Configuration", configuration }
				},
				NoPackageAnalysis = false,
				Symbols = false,
				OutputDirectory = nugetPackOutputDirectory,
			});

			DeleteFile(nuspecFile);

			var nupgkFile = File(nugetPackOutputDirectory + "/" + project.Name + "." + assemblyVersion + ".nupkg");

			NupkgPush(new ISI.Cake.Addin.Nuget.NupkgPushRequest()
			{
				NupkgFullNames = new [] { nupgkFile.Path.FullPath },
				ApiKey = settings.Nuget.ApiKey,
				RepositoryName = settings.Nuget.RepositoryName,
				RepositoryUri = GetNullableUri(settings.Nuget.RepositoryUrl),
				PackageChunksRepositoryUri = GetNullableUri(settings.Nuget.PackageChunksRepositoryUrl),
			});
		}

		CreateAssemblyInfo(assemblyVersionFile, new AssemblyInfoSettings()
		{
			Version = GetAssemblyVersion(assemblyVersion, "*"),
		});
	});

Task("Publish")
	.IsDependentOn("Build")
	.Does(() =>
	{
		//DockerTag("isiserviceexampleserviceapplication", "repo/isiserviceexampleserviceapplication:latest");
		//DockerPush("repo/isiserviceexampleserviceapplication:latest");
	});

Task("Production-Deploy")
	.Does(() => 
	{
		//DockerTag("isiserviceexampleserviceapplication", "repo/isiserviceexampleserviceapplication:production");
		//DockerTag("repo/isiserviceexampleserviceapplication:latest", "repo/isiserviceexampleserviceapplication:production");
		//DockerPush("repo/isiserviceexampleserviceapplication:production");
	});

Task("Default")
	.IsDependentOn("Publish")
	.Does(() => 
	{
		Information("No target provided. Starting default task");
	});

using(GetSolutionLock())
{
	RunTarget(target);
}