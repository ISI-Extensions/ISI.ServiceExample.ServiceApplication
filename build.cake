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

var solutionFile = File("./src/ISI.ServiceExample.ServiceApplication.slnx");
var solutionDetails = GetSolutionDetails(solutionFile);
var rootProjectFile = File("./src/ISI.ServiceExample.ServiceApplication/ISI.ServiceExample.ServiceApplication.csproj");
var rootAssemblyVersionKey = "ISI.ServiceExample";
var artifactName = "ISI.ServiceExample.ServiceApplication";

var buildDateTime = DateTime.UtcNow;
var buildDateTimeStamp = GetDateTimeStamp(buildDateTime);
var buildRevision = GetBuildRevision(buildDateTime);

var assemblyVersions = GetAssemblyVersionFiles(rootAssemblyVersionKey, buildRevision);
var assemblyVersion = assemblyVersions[rootAssemblyVersionKey].AssemblyVersion;

var buildDateTimeStampVersion = new ISI.Extensions.Scm.DateTimeStampVersion(buildDateTimeStamp, assemblyVersions[rootAssemblyVersionKey].AssemblyVersion);

Information("BuildDateTimeStampVersion: {0}", buildDateTimeStampVersion);

var nugetPackOutputDirectory = Argument("NugetPackOutputDirectory", System.IO.Path.GetFullPath("./Nuget"));

var buildArtifactZipFile = File(string.Format("./Publish/{0}.{1}.zip", artifactName, buildDateTimeStamp));

Task("Clean")
	.Does(() =>
	{
		Information("Cleaning Projects ...");

		foreach(var projectPath in new HashSet<string>(solutionDetails.ProjectDetailsSet.Select(project => project.ProjectDirectory)))
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
		using(GetNugetLock())
		{
			RestoreNugetPackages(solutionFile);
		}
	});

Task("Build")
	.IsDependentOn("NugetPackageRestore")
	.Does(() => 
	{
		using(SetAssemblyVersionFiles(assemblyVersions))
		{
			DotNetBuild(solutionFile, new DotNetBuildSettings()
			{
				Configuration = configuration,
			});
		}
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
			files.Add(GetFiles("./**/bin/" + configuration + "/**/ISI.Services.ServiceExample.*.dll"));

			if(files.Any())
			{
				using(var tempDirectory = GetNewTempDirectory())
				{
					foreach(var file in files)
					{
						var tempFile = File(tempDirectory.FullName + "/" + file.GetFilename());

						if(System.IO.File.Exists(tempFile.Path.FullPath))
						{
							DeleteFile(tempFile);
						}

						CopyFile(file, tempFile);
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

						DeleteFile(file);

						CopyFile(tempFile, file);
					}
				}
			}
		}
	});

Task("Package")
	.IsDependentOn("Sign")
	.Does(() =>
	{
		var sourceControlUrl = GetSolutionSourceControlUrl();
		var nupkgFiles = new FilePathCollection();

		foreach(var project in solutionDetails.ProjectDetailsSet.Where(project => project.ProjectFullName.EndsWith(".csproj") && 
																	project.ProjectName.StartsWith("ISI.Services.") && 
																	!project.ProjectFullName.EndsWith(".Test") && 
																	!project.ProjectFullName.EndsWith(".Tests") && 
																	!project.ProjectFullName.EndsWith(".T4LocalContent")).OrderBy(project => project.ProjectName, StringComparer.InvariantCultureIgnoreCase))
		{
			Information(project.ProjectName);

			var nuspec = GenerateNuspecFromProject(new ISI.Cake.Addin.Nuget.GenerateNuspecFromProjectRequest()
			{
				ProjectFullName =  System.IO.Path.GetFullPath(project.ProjectFullName),
				TryGetPackageVersion = (string package, out string version) =>
				{
					if (package.StartsWith("ISI.Services", StringComparison.InvariantCultureIgnoreCase))
					{
						version =  assemblyVersion;
						return true;
					}

					version = string.Empty;
					return false;
				},
				IncludeSBom = false,
				Settings = settings,
			}).Nuspec;
			nuspec.Version = assemblyVersion;
			nuspec.ProjectUri = GetNullableUri(sourceControlUrl);
			nuspec.Title = project.ProjectName;
			nuspec.Description = project.ProjectName;

			var nuspecFile = File(project.ProjectDirectory + "/" + project.ProjectName + ".nuspec");

			CreateNuspecFile(new ISI.Cake.Addin.Nuget.CreateNuspecFileRequest()
			{
				Nuspec = nuspec,
				NuspecFullName = nuspecFile.Path.FullPath,
			});

			NupkgPack(new ISI.Cake.Addin.Nuget.NupkgPackRequest()
			{
				NuspecFullName = System.IO.Path.GetFullPath(nuspecFile.Path.FullPath),
				CsProjFullName = System.IO.Path.GetFullPath(project.ProjectFullName),
				OutputDirectory = nugetPackOutputDirectory,
			});

			DeleteFile(nuspecFile);

			nupkgFiles.Add(File(nugetPackOutputDirectory + "/" + project.ProjectName + "." + assemblyVersion + ".nupkg"));
		}

		if(settings.CodeSigning.DoCodeSigning)
		{
			SignNupkgs(new ISI.Cake.Addin.CodeSigning.SignNupkgsUsingSettingsRequest()
			{
				NupkgPaths = nupkgFiles,
				Settings = settings,
			});
		}

		PackageComponents(new ISI.Cake.Addin.PackageComponents.PackageComponentsRequest()
		{
			Configuration = configuration,
			BuildPlatform = MSBuildPlatform.x64,
			SubDirectory = "ISI",
			PackageComponents = new ISI.Cake.Addin.PackageComponents.IPackageComponent[] 
			{
				new ISI.Cake.Addin.PackageComponents.PackageComponentWindowsService()
				{
					ProjectFullName = rootProjectFile.Path.FullPath,
					IconFileName = "Lantern.ico",
				},
			},
			PackageFullName = buildArtifactZipFile.Path.FullPath,
			PackageBuildDateTimeStampVersion = buildDateTimeStampVersion,
		});
		
		DeleteAgedPackages(new ISI.Cake.Addin.PackageComponents.DeleteAgedPackagesRequest()
		{
			PackagesDirectory = buildArtifactZipFile.Path.GetDirectory().FullPath,
			PackageName = artifactName,
			PackageNameExtension = "zip",
		});		
	});

Task("Nuget-Publish")
	.IsDependentOn("Package")
	.Does(() =>
	{
		var nupkgFiles = GetFiles(MakeAbsolute(Directory(nugetPackOutputDirectory)) + "/*.nupkg");

		NupkgPush(new ISI.Cake.Addin.Nuget.NupkgPushUsingSettingsActiveDirectoryRequest()
		{
			NupkgPaths = nupkgFiles,
			Settings = settings,
		});
	});

Task("Publish")
	.IsDependentOn("Nuget-Publish")
	.Does(() =>
	{
		var buildArtifactsApiKey = GetBuildArtifactsApiKey(new ISI.Cake.Addin.BuildArtifacts.GetBuildArtifactsApiKeyUsingSettingsActiveDirectoryRequest()
		{
			Settings = settings,
		}).BuildArtifactsApiKey;

		UploadBuildArtifact(new ISI.Cake.Addin.BuildArtifacts.UploadBuildArtifactRequest()
		{
			BuildArtifactsApiUri = GetNullableUri(settings.BuildArtifacts.ApiUrl),
			BuildArtifactsApiKey = buildArtifactsApiKey,
			SourceFileName = buildArtifactZipFile.Path.FullPath,
			BuildArtifactName = artifactName,
			DateTimeStampVersion = buildDateTimeStampVersion,
		});

		SetBuildArtifactEnvironmentDateTimeStampVersion(new ISI.Cake.Addin.BuildArtifacts.SetBuildArtifactEnvironmentDateTimeStampVersionRequest()
		{
			BuildArtifactsApiUri = GetNullableUri(settings.BuildArtifacts.ApiUrl),
			BuildArtifactsApiKey = buildArtifactsApiKey,
			BuildArtifactName = artifactName,
			Environment = "Build",
			DateTimeStampVersion = buildDateTimeStampVersion,
		});
	});
	
Task("Production-Deploy")
	.Does(() => 
	{
		var buildArtifactsApiKey = GetBuildArtifactsApiKey(new ISI.Cake.Addin.BuildArtifacts.GetBuildArtifactsApiKeyUsingSettingsActiveDirectoryRequest()
		{
			Settings = settings,
		}).BuildArtifactsApiKey;

		var dateTimeStampVersion = GetBuildArtifactEnvironmentDateTimeStampVersion(new ISI.Cake.Addin.BuildArtifacts.GetBuildArtifactEnvironmentDateTimeStampVersionRequest()
		{
			BuildArtifactsApiUri = GetNullableUri(settings.BuildArtifacts.ApiUrl),
			BuildArtifactsApiKey = buildArtifactsApiKey,
			BuildArtifactName = artifactName,
			Environment = "Build",
		}).DateTimeStampVersion;

		DeployBuildArtifact(new ISI.Cake.Addin.DeploymentManager.DeployBuildArtifactRequest()
		{
			WindowsDeploymentApiUri = GetNullableUri(settings.GetValue("PRODUCTION-SERVER-DeployManager-Url")),
			WindowsDeploymentApiKey = settings.GetValue("PRODUCTION-SERVER-DeployManager-Password"),

			BuildArtifactsApiUri = GetNullableUri(settings.BuildArtifacts.ApiUrl),
			BuildArtifactsApiKey = buildArtifactsApiKey,

			BuildArtifactName = artifactName,
			ToDateTimeStampVersion = dateTimeStampVersion,
			ToEnvironment = "Production",
			ConfigurationKey = "Production",
			Components = new ISI.Cake.Addin.DeploymentManager.IDeployComponent[]
			{
				new ISI.Cake.Addin.DeploymentManager.DeployComponentWindowsService()
				{
					PackageFolder = "ISI\\ISI.ServiceExample.ServiceApplication",
					DeployToSubfolder = "ISI.ServiceExample.ServiceApplication",
					DeployToSubfolderIconFileName = "Lantern.ico",
					WindowsServiceExe = "ISI.ServiceExample.ServiceApplication.exe",
				},
			},
		});
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