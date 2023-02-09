//dotnet tool install Cake.Tool -g
#addin nuget:?package=Cake.FileHelpers
#tool nuget:?package=7-Zip.CommandLine
#addin nuget:?package=Cake.7zip
#addin nuget:?package=ISI.Cake.AddIn&loaddependencies=true

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

var assemblyVersions = GetAssemblyVersionFiles(rootAssemblyVersionKey, buildRevision);
var assemblyVersion = assemblyVersions[rootAssemblyVersionKey].AssemblyVersion;

var buildDateTimeStampVersion = new ISI.Extensions.Scm.DateTimeStampVersion(buildDateTimeStamp, assemblyVersions[rootAssemblyVersionKey].AssemblyVersion);

Information("BuildDateTimeStampVersion: {0}", buildDateTimeStampVersion);

var nugetPackOutputDirectory = Argument("NugetPackOutputDirectory", "../Nuget");

Task("Clean")
	.Does(() =>
	{
		Information("Cleaning Projects ...");

		foreach(var projectPath in new HashSet<string>(solution.Projects.Select(p => p.Path.GetDirectory().ToString())))
		{
			Information("Cleaning {0}", projectPath);
			CleanDirectories(projectPath + "/**/bin/" + configuration);
			CleanDirectories(projectPath + "/**/obj/" + configuration);
		}

		Information("Cleaning {0}", nugetPackOutputDirectory);
		CleanDirectories(nugetPackOutputDirectory);
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

		CreateAssemblyInfo(assemblyVersionFile, new AssemblyInfoSettings()
		{
			Version = GetAssemblyVersion(assemblyVersion, "*"),
		});
	});


Task("Sign")
	.IsDependentOn("Build")
	.Does(() =>
	{
		if (settings.CodeSigning.DoCodeSigning && configuration.Equals("Release"))
		{
			var files = GetFiles("./**/bin/" + configuration + "/**/ISI.ServiceExample.*.dll");
			files.Add(GetFiles("./**/bin/" + configuration + "/**/ISI.ServiceExample.dll"));
			files.Add(GetFiles("./**/bin/" + configuration + "/**/ISI.ServiceExample.*.exe"));
			files.Add(GetFiles("./**/bin/" + configuration + "/**/ISI.Services.ServiceExample.dll"));

			if(files.Any())
			{
				using(var tempDirectory = GetNewTempDirectory())
				{
					tempDirectory.DeleteDirectory = false;

					foreach(var file in files)
					{
						var tempFile = File(tempDirectory.FullName + "/" + file.GetFilename());

						if(!tempFile.Path.FullPath.EndsWith(".Tests.dll", StringComparison.InvariantCultureIgnoreCase) &&
							 !tempFile.Path.FullPath.EndsWith("ISI.ServiceExample.MigrationTool.CosmosDB.dll", StringComparison.InvariantCultureIgnoreCase) &&
							 !tempFile.Path.FullPath.EndsWith("ISI.ServiceExample.MigrationTool.CosmosDB.exe", StringComparison.InvariantCultureIgnoreCase))
						{
							if(System.IO.File.Exists(tempFile.Path.FullPath))
							{
								DeleteFile(tempFile);
							}

							CopyFile(file, tempFile);
						}
					}

					var tempFiles = GetFiles(tempDirectory.FullName + "/*.dll");
					tempFiles.Add(GetFiles(tempDirectory.FullName + "/*.exe"));

					SignAssemblies(new ISI.Cake.Addin.CodeSigning.SignAssembliesUsingSettingsRequest()
					{
						AssemblyPaths = tempFiles,
						Settings = settings,
					});

					foreach(var file in files)
					{
						var tempFile = File(tempDirectory.FullName + "/" + file.GetFilename());

						if(System.IO.File.Exists(tempFile.Path.FullPath))
						{
							DeleteFile(file);

							CopyFile(tempFile, file);
						}
					}
				}
			}
		}
	});

Task("Nuget")
	.IsDependentOn("Sign")
	.Does(() =>
	{
		var sourceControlUrl = GetSolutionSourceControlUrl();
		var nupkgFiles = new FilePathCollection();

		foreach(var project in solution.Projects.Where(project => project.Path.FullPath.EndsWith(".csproj") && 
																															project.Name.StartsWith("ISI.Services.") && 
																															!project.Name.EndsWith(".Tests"))
																						.OrderBy(project => project.Name, StringComparer.InvariantCultureIgnoreCase))
		{
			Information(project.Name);

			var nuspec = GenerateNuspecFromProject(new ISI.Cake.Addin.Nuget.GenerateNuspecFromProjectRequest()
			{
				ProjectFullName = project.Path.FullPath,
				TryGetPackageVersion = (string package, out string version) =>
				{
					if (package.StartsWith("ISI.Services", StringComparison.InvariantCultureIgnoreCase))
					{
						version =  assemblyVersion;
						return true;
					}

					version = string.Empty;
					return false;
				}
			}).Nuspec;

			var files = new List<ISI.Extensions.Nuget.NuspecFile>(nuspec.Files ?? new ISI.Extensions.Nuget.NuspecFile[0]);

			{
				var pdbFile = File(project.Path.GetDirectory() + "/bin/" + configuration + "/" + project.Name + ".pdb");
				if(FileExists(pdbFile))
				{
					files.Add(new ISI.Extensions.Nuget.NuspecFile()
					{
						Target = "lib/net48",
						SourcePattern = pdbFile.Path.FullPath,
					});
				}
			}

			{
				var pdbFile = File(project.Path.GetDirectory() + "/bin/" + configuration + "/netstandard2.0/" + project.Name + ".pdb");
				if(FileExists(pdbFile))
				{
					files.Add(new ISI.Extensions.Nuget.NuspecFile()
					{
						Target = "lib/netstandard2.0",
						SourcePattern = pdbFile.Path.FullPath,
					});
				}
			}

			nuspec.Files = files;

			nuspec.Version = assemblyVersion;
			nuspec.IconUri = GetNullableUri(@"https://nuget.isi-net.com/Images/Lantern.png");
			nuspec.ProjectUri = GetNullableUri(sourceControlUrl);
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

			nupkgFiles.Add(File(nugetPackOutputDirectory + "/" + project.Name + "." + assemblyVersion + ".nupkg"));
		}

		if(settings.CodeSigning.DoCodeSigning)
		{
			SignNupkgs(new ISI.Cake.Addin.CodeSigning.SignNupkgsUsingSettingsRequest()
			{
				NupkgPaths = nupkgFiles,
				Settings = settings,
			});
		}
	});

Task("Publish")
	.IsDependentOn("Nuget")
	.Does(() =>
	{
		var nupkgFiles = GetFiles(MakeAbsolute(Directory(nugetPackOutputDirectory)) + "/*.nupkg");

		NupkgPush(new ISI.Cake.Addin.Nuget.NupkgPushRequest()
		{
			NupkgPaths = nupkgFiles,
			ApiKey = settings.Nuget.ApiKey,
			RepositoryName = settings.Nuget.RepositoryName,
			RepositoryUri = GetNullableUri(settings.Nuget.RepositoryUrl),
			PackageChunksRepositoryUri = GetNullableUri(settings.Nuget.PackageChunksRepositoryUrl),
		});

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