#region Copyright & License
/*
Copyright (c) 2021, Integrated Solutions, Inc.
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

		* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
		* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
		* Neither the name of the Integrated Solutions, Inc. nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion
 
using System;
using System.Linq;
using ISI.Extensions.ConfigurationHelper.Extensions;
using ISI.Extensions.DependencyInjection.Extensions;
using ISI.Extensions.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ISI.ServiceExample.MigrationTool.SqlServer
{
	class Program
	{
		private static void Main(string[] args)
		{
			Console.WriteLine(DateTime.Now.Formatted(DateTimeExtensions.DateTimeFormat.DateTime));

			var configurationBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();

			var configurationsPath = string.Format("Configuration{0}", System.IO.Path.DirectorySeparatorChar);

			var activeEnvironment = configurationBuilder.GetActiveEnvironmentConfig($"{configurationsPath}isi.extensions.environmentsConfig.json");

			System.Console.WriteLine($"Starting {typeof(Program).Namespace}");
			System.Console.WriteLine($"Version: {ISI.Extensions.SystemInformation.GetAssemblyVersion(typeof(Program).Assembly)}");

			foreach (System.Collections.DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
			{
				System.Console.WriteLine($"  EV \"{environmentVariable.Key}\" => \"{environmentVariable.Value}\"");
			}

			System.Console.WriteLine($"ActiveEnvironment: {activeEnvironment.ActiveEnvironment}");
			System.Console.WriteLine($"ActiveEnvironments: {string.Join(", ", activeEnvironment.ActiveEnvironments.Select(e => string.Format("\"{0}\"", e)))}");

			configurationBuilder.SetBasePath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
			configurationBuilder.AddJsonFile("appsettings.json", optional: false);
			configurationBuilder.AddJsonFiles(activeEnvironment.ActiveEnvironments, environment => $"appsettings.{environment}.json");

			var connectionStringPath = string.Format("Configuration{0}ConnectionStrings{0}", System.IO.Path.DirectorySeparatorChar);

			configurationBuilder.AddClassicConnectionStringsSectionFile($"{connectionStringPath}connectionStrings.config");
			configurationBuilder.AddClassicConnectionStringsSectionFiles(activeEnvironment.ActiveEnvironments, environment => $"{connectionStringPath}connectionStrings.{environment}.config");

			var configurationRoot = configurationBuilder.Build().ApplyConfigurationValueReaders();

			var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
				.AddOptions()
				.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(configurationRoot)
				.AddConfigurationRegistrations(configurationRoot)
				.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory>()
				//.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory, Microsoft.Extensions.Logging.LoggerFactory>()
				.AddLogging(builder => builder
						.AddConsole()
					//.AddFilter(level => level >= Microsoft.Extensions.Logging.LogLevel.Information)
				)
				.AddTransient<Microsoft.Extensions.Logging.ILogger>(serviceProvider => serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>())
				.ProcessServiceRegistrars();

			configurationRoot.AddAllConfigurations(services);

			var serviceProvider = services.BuildServiceProvider(configurationRoot);

			var logger = serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger>();
			var dateTimeStamper = serviceProvider.GetService<ISI.Extensions.DateTimeStamper.IDateTimeStamper>();
			
			var serializer = serviceProvider.GetService<ISI.Extensions.JsonSerialization.IJsonSerializer>();

			var repositoryConfiguration = serviceProvider.GetService<ISI.ServiceExample.Repository.SqlServer.Configuration>();
			var repositorySetupApi = new ISI.Extensions.Repository.SqlServer.RepositorySetupApi(configurationRoot, logger, dateTimeStamper, serializer, repositoryConfiguration.ConnectionString);

			var migrationToolApi = new ISI.Extensions.Repository.MigrationApi(serviceProvider, repositorySetupApi);

			migrationToolApi.Migrate();

			Console.WriteLine(DateTime.Now.Formatted(DateTimeExtensions.DateTimeFormat.DateTime));

			if (!(new System.Collections.Generic.HashSet<string>(args ?? Array.Empty<string>(), StringComparer.InvariantCultureIgnoreCase)).Contains("-noWaitAtFinish"))
			{
				Console.WriteLine("Press <enter> to finish");
				Console.ReadLine();
			}
		}
	}
}