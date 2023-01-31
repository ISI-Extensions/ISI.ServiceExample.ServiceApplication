#region Copyright & License
/*
Copyright (c) 2023 Integrated Solutions, Inc.
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

		* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
		* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
		* Neither the name of the Integrated Solutions, Inc. nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISI.Extensions.ConfigurationHelper.Extensions;
using ISI.Extensions.DependencyInjection.Extensions;
using ISI.Extensions.DependencyInjection.Iunq.Extensions;
using ISI.Extensions.Extensions;
using ISI.Extensions.MessageBus.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Exceptions;
using Topshelf;
using ISI.Extensions.Services.Extensions;

namespace ISI.ServiceExample.ServiceApplication
{
	public class Program
	{
		public static int Main()
		{
			var arguments = Environment.GetCommandLineArgs();

			var configurationBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();

			var configurationsPath = string.Format("Configuration{0}", System.IO.Path.DirectorySeparatorChar);

			var activeEnvironment = configurationBuilder.GetActiveEnvironmentConfig($"{configurationsPath}isi.extensions.environmentsConfig.json");

			configurationBuilder.SetBasePath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
			configurationBuilder.AddJsonFile("appsettings.json", optional: false);
			configurationBuilder.AddJsonFiles(activeEnvironment.ActiveEnvironments, environment => $"appsettings.{environment}.json");

			var connectionStringPath = string.Format("Configuration{0}ConnectionStrings{0}", System.IO.Path.DirectorySeparatorChar);

			configurationBuilder.AddClassicConnectionStringsSectionFile($"{connectionStringPath}connectionStrings.config");
			configurationBuilder.AddClassicConnectionStringsSectionFiles(activeEnvironment.ActiveEnvironments, environment => $"{connectionStringPath}connectionStrings.{environment}.config");

			var configuration = configurationBuilder.Build();

			CreateLogger(configuration, activeEnvironment.ActiveEnvironment);

			Serilog.Log.Information($"Starting {typeof(Program).Namespace}");
			Serilog.Log.Information($"Version: {ISI.Extensions.SystemInformation.GetAssemblyVersion(typeof(Program).Assembly)}");

			foreach (System.Collections.DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
			{
				Serilog.Log.Information($"  EV \"{environmentVariable.Key}\" => \"{environmentVariable.Value}\"");
			}

			Serilog.Log.Information($"ActiveEnvironment: {activeEnvironment.ActiveEnvironment}");
			Serilog.Log.Information($"ActiveEnvironments: {string.Join(", ", activeEnvironment.ActiveEnvironments.Select(e => string.Format("\"{0}\"", e)))}");

			var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
				.AddOptions()
				.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(configuration)
				.AddAllConfigurations(configuration)
				.AddConfiguration<Microsoft.Extensions.Hosting.ConsoleLifetimeOptions>(configuration)
				.AddConfiguration<Microsoft.Extensions.Hosting.HostOptions>(configuration)
				//.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory>()
				//.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory, Microsoft.Extensions.Logging.LoggerFactory>()
				//.AddLogging(builder => builder
				//	.AddConsole()
				////.AddFilter(level => level >= Microsoft.Extensions.Logging.LogLevel.Information)
				//)
				.AddSingleton<ILoggerFactory>(services => new Serilog.Extensions.Logging.SerilogLoggerFactory(dispose: true))
				.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true))
				.AddTransient<Microsoft.Extensions.Logging.ILogger>(serviceProvider => serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>())
				.AddSingleton<Microsoft.Extensions.Caching.Memory.IMemoryCache>(provider => new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()))
				;

			AddCaching(services, configuration);

			services
				.AddMessageBus(configuration)
				.AddConfigurationRegistrations(configuration)
				.ProcessServiceRegistrars()
			;

			configuration.AddAllConfigurations(services);

			var serviceProvider = services.BuildServiceProvider(configuration);

			return (int) HostFactory.Run(hostConfigurator =>
			{
				hostConfigurator.UseSerilog();

				hostConfigurator.UseAssemblyInfoForServiceInfo();

				hostConfigurator.StartAutomatically();

				hostConfigurator.EnableServiceRecovery(recoveryConfig =>
				{
					recoveryConfig.RestartService(1); // restart the service after 1 minute
					recoveryConfig.RestartService(1); // restart the service after 1 minute
					recoveryConfig.SetResetPeriod(1); // set the reset interval to one day
				});

				hostConfigurator.Service<ISI.Extensions.Services.IServiceManagerAsync>(configurator =>
				{
					configurator.ConstructUsing(serviceFactory => serviceProvider.GetService<ISI.Extensions.Services.IServiceManagerAsync>());
					configurator.WhenStarted((service, control) =>
					{
						control.RequestAdditionalTime(TimeSpan.FromMinutes(10));
						service.StartAsync(configuration, activeEnvironment.ActiveEnvironment, arguments, System.Threading.CancellationToken.None, serviceProvider).Wait();
						return true;
					});
					configurator.WhenStopped((service, control) =>
					{
						service.StopAsync(configuration, activeEnvironment.ActiveEnvironment, arguments, System.Threading.CancellationToken.None, serviceProvider).Wait();
						return true;
					});
				});

				hostConfigurator.BeforeInstall(settings => { serviceProvider.GetService<ISI.Extensions.Services.IServiceManagerAsync>().BeforeInstallAsync(configuration, activeEnvironment.ActiveEnvironment, arguments, System.Threading.CancellationToken.None, serviceProvider); });

				hostConfigurator.AfterInstall(settings => { serviceProvider.GetService<ISI.Extensions.Services.IServiceManagerAsync>().AfterInstallAsync(configuration, activeEnvironment.ActiveEnvironment, arguments, System.Threading.CancellationToken.None, serviceProvider); });

				hostConfigurator.BeforeUninstall(() => { serviceProvider.GetService<ISI.Extensions.Services.IServiceManagerAsync>().BeforeUninstallAsync(configuration, activeEnvironment.ActiveEnvironment, arguments, System.Threading.CancellationToken.None, serviceProvider); });

				hostConfigurator.AfterUninstall(() => { serviceProvider.GetService<ISI.Extensions.Services.IServiceManagerAsync>().AfterUninstallAsync(configuration, activeEnvironment.ActiveEnvironment, arguments, System.Threading.CancellationToken.None, serviceProvider); });
			});
		}

		public static void AddCaching(IServiceCollection services, IConfigurationRoot configuration)
		{
			services.AddSingleton<ISI.Extensions.Caching.ICacheManager, ISI.Extensions.Caching.CacheManager<Microsoft.Extensions.Caching.Memory.IMemoryCache>>();
			services.AddSingleton<ISI.Extensions.Caching.IEnterpriseCacheManagerApi, ISI.Extensions.Caching.MessageBus.EnterpriseCacheManagerApi>();
		}

		public static void CreateLogger(IConfigurationRoot configuration, string environment)
		{
			var loggerConfiguration = new Serilog.LoggerConfiguration()
					.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
					.MinimumLevel.Verbose()
					.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
					.MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
					.Enrich.FromLogContext()
					.Enrich.WithEnvironmentUserName()
					.Enrich.WithMachineName()
					.Enrich.WithProcessId()
					.Enrich.WithThreadId()
					.Enrich.WithExceptionDetails()
					.Enrich.WithProperty("Environment", environment)
					.ReadFrom.Configuration(configuration)
					.WriteTo.Console()
				;

			var serviceApplicationConfiguration = new ISI.ServiceExample.ServiceApplication.Configuration();
			configuration.Bind(ISI.ServiceExample.ServiceApplication.Configuration.ConfigurationSectionName, serviceApplicationConfiguration);

			if (!string.IsNullOrWhiteSpace(serviceApplicationConfiguration?.ElasticsearchLogging?.NodeUrl))
			{
				if (!string.IsNullOrWhiteSpace(serviceApplicationConfiguration?.ElasticsearchLogging?.UserName))
				{
					loggerConfiguration.WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(serviceApplicationConfiguration.ElasticsearchLogging.NodeUrl))
					{
						AutoRegisterTemplate = true,
						AutoRegisterTemplateVersion = Serilog.Sinks.Elasticsearch.AutoRegisterTemplateVersion.ESv6,
						IndexFormat = serviceApplicationConfiguration.ElasticsearchLogging.IndexFormat,
						ModifyConnectionSettings = connectionConfiguration => connectionConfiguration.BasicAuthentication(serviceApplicationConfiguration.ElasticsearchLogging.UserName, serviceApplicationConfiguration.ElasticsearchLogging.Password),
					});
				}
				else
				{
					loggerConfiguration.WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(serviceApplicationConfiguration.ElasticsearchLogging.NodeUrl))
					{
						AutoRegisterTemplate = true,
						AutoRegisterTemplateVersion = Serilog.Sinks.Elasticsearch.AutoRegisterTemplateVersion.ESv6,
						IndexFormat = serviceApplicationConfiguration.ElasticsearchLogging.IndexFormat,
					});
				}
			}

			Serilog.Log.Logger = loggerConfiguration.CreateLogger();
		}
	}
}