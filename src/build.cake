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

var solutionFile = File("./ISI.ServiceExample.ServiceApplication.sln");
var solution = ParseSolution(solutionFile);
var rootProjectFile = File("./ISI.ServiceExample.ServiceApplication/ISI.ServiceExample.ServiceApplication.csproj");
var rootAssemblyVersionKey = "ISI.ServiceExample";
var artifactName = "ISI.ServiceExample.ServiceApplication";

var buildDateTime = DateTime.UtcNow;
var buildDateTimeStamp = GetDateTimeStamp(buildDateTime);
var buildRevision = GetBuildRevision(buildDateTime);

var assemblyVersions = GetAssemblyVersionFiles(rootAssemblyVersionKey, buildRevision);
var assemblyVersion = assemblyVersions[rootAssemblyVersionKey].AssemblyVersion;

var buildDateTimeStampVersion = new ISI.Extensions.Scm.DateTimeStampVersion(buildDateTimeStamp, assemblyVersions[rootAssemblyVersionKey].AssemblyVersion);

Information("BuildDateTimeStampVersion: {0}", buildDateTimeStampVersion);

var nugetPackOutputDirectory = Argument("NugetPackOutputDirectory", "../Nuget");

var buildArtifactZipFile = File(string.Format("../Publish/{0}.{1}.zip", artifactName, buildDateTimeStamp));

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
		using(GetNugetLock())
		{
			NuGetRestore(solutionFile);
		}
	});

Task("Build")
	.IsDependentOn("NugetPackageRestore")
	.Does(() => 
	{
		SetAssemblyVersionFiles(assemblyVersions);

		try
		{
			MSBuild(solutionFile, configurator => configurator
				.SetConfiguration(configuration)
				.SetVerbosity(Verbosity.Quiet)
				.SetMSBuildPlatform(MSBuildPlatform.x64)
				.SetPlatformTarget(PlatformTarget.MSIL)
				.WithTarget("Rebuild"));
		}
		finally
		{
			ResetAssemblyVersionFiles(assemblyVersions);
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

		PackageComponents(new ISI.Cake.Addin.PackageComponents.PackageComponentsRequest()
		{
			Configuration = configuration,
			BuildPlatform = MSBuildPlatform.x64,
			SubDirectory = "ISI",
			PackageComponents = new ISI.Cake.Addin.PackageComponents.IPackageComponent[] 
			{
				new ISI.Cake.Addin.PackageComponents.PackageComponentConsoleApplication()
				{
					ProjectFullName = File("./ISI.ServiceExample.MigrationTool.SqlServer/ISI.ServiceExample.MigrationTool.SqlServer.csproj").Path.FullPath,
					IconFullName = File("./Lantern.ico").Path.FullPath,
				},
				new ISI.Cake.Addin.PackageComponents.PackageComponentWindowsService()
				{
					ProjectFullName = rootProjectFile.Path.FullPath,
					IconFullName = File("./Lantern.ico").Path.FullPath,
				},
			},
			PackageFullName = buildArtifactZipFile.Path.FullPath,
			PackageVersion = assemblyVersion,
			PackageBuildDateTimeStamp = buildDateTimeStamp,
		});
		
		DeleteAgedPackages(new ISI.Cake.Addin.PackageComponents.DeleteAgedPackagesRequest()
		{
			PackagesDirectory = buildArtifactZipFile.Path.GetDirectory().FullPath,
			PackageName = artifactName,
			PackageNameExtension = "zip",
		});		
	});

Task("Publish")
	.IsDependentOn("Package")
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

		var authenticationToken = GetBuildArtifactsAuthenticationToken(new ISI.Cake.Addin.BuildArtifacts.GetBuildArtifactsAuthenticationTokenRequest()
		{
			BuildArtifactsApiUri = GetNullableUri(settings.BuildArtifacts.ApiUrl),
			UserName = settings.ActiveDirectory.UserName,
			Password = settings.ActiveDirectory.Password,
		}).AuthenticationToken;

		UploadBuildArtifact(new ISI.Cake.Addin.BuildArtifacts.UploadBuildArtifactRequest()
		{
			BuildArtifactsApiUri = GetNullableUri(settings.BuildArtifacts.ApiUrl),
			BuildArtifactsApiKey = authenticationToken,
			SourceFileName = buildArtifactZipFile.Path.FullPath,
			BuildArtifactName = artifactName,
			DateTimeStampVersion = buildDateTimeStampVersion,
		});

		SetBuildArtifactEnvironmentDateTimeStampVersion(new ISI.Cake.Addin.BuildArtifacts.SetBuildArtifactEnvironmentDateTimeStampVersionRequest()
		{
			BuildArtifactsApiUri = GetNullableUri(settings.BuildArtifacts.ApiUrl),
			BuildArtifactsApiKey = authenticationToken,
			BuildArtifactName = artifactName,
			Environment = "Build",
			DateTimeStampVersion = buildDateTimeStampVersion,
		});
	});
	
Task("Production-Deploy")
	.Does(() => 
	{
		var authenticationToken = GetBuildArtifactsAuthenticationToken(new ISI.Cake.Addin.BuildArtifacts.GetBuildArtifactsAuthenticationTokenRequest()
		{
			BuildArtifactsApiUri = GetNullableUri(settings.BuildArtifacts.ApiUrl),
			UserName = settings.ActiveDirectory.UserName,
			Password = settings.ActiveDirectory.Password,
		}).AuthenticationToken;

		var dateTimeStampVersion = GetBuildArtifactEnvironmentDateTimeStampVersion(new ISI.Cake.Addin.BuildArtifacts.GetBuildArtifactEnvironmentDateTimeStampVersionRequest()
		{
			BuildArtifactsApiUri = GetNullableUri(settings.BuildArtifacts.ApiUrl),
			BuildArtifactsApiKey = authenticationToken,
			BuildArtifactName = artifactName,
			Environment = "Build",
		}).DateTimeStampVersion;

		DeployBuildArtifact(new ISI.Cake.Addin.DeploymentManager.DeployBuildArtifactRequest()
		{
			ServicesManagerUri = GetNullableUri(settings.GetValue("PRODUCTION-SERVER-DeployManager-Url")),
			ServicesManagerApiKey = settings.GetValue("PRODUCTION-SERVER-DeployManager-Password"),

			BuildArtifactsApiUri = GetNullableUri(settings.BuildArtifacts.ApiUrl),
			BuildArtifactsApiKey = authenticationToken,

			BuildArtifactName = artifactName,
			ToDateTimeStamp = dateTimeStampVersion,
			ToEnvironment = "Production",
			ConfigurationKey = "Production",
			Components = new ISI.Cake.Addin.DeploymentManager.IDeployComponent[]
			{
				new ISI.Cake.Addin.DeploymentManager.DeployComponentConsoleApplication()
				{
					PackageFolder = "ISI\\ISI.ServiceExample.MigrationTool.SqlServer",
					DeployToSubfolder = "ISI.ServiceExample.MigrationTool.SqlServer",
					ConsoleApplicationExe = "ISI.ServiceExample.MigrationTool.SqlServer.exe",
					ExecuteConsoleApplicationAfterInstall = true,
					ExecuteConsoleApplicationAfterInstallArguments = "-noWaitAtFinish",
				},
				new ISI.Cake.Addin.DeploymentManager.DeployComponentWindowsService()
				{
					PackageFolder = "ISI\\ISI.ServiceExample.ServiceApplication",
					DeployToSubfolder = "ISI.ServiceExample.ServiceApplication",
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