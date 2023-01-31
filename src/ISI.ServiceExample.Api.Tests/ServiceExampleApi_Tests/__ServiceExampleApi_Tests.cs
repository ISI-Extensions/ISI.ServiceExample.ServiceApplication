using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISI.Extensions.ConfigurationHelper.Extensions;
using ISI.Extensions.DependencyInjection.Extensions;
using ISI.Extensions.Extensions;
using ISI.Extensions.MessageBus.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ISI.ServiceExample.Api.Tests
{
	[TestFixture]
	public partial class ServiceExampleApi_Tests
	{
		public IServiceProvider ServiceProvider { get; set; }

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			var configurationBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();

			var configurationsPath = string.Format("Configuration{0}", System.IO.Path.DirectorySeparatorChar);

			var activeEnvironment = configurationBuilder.GetActiveEnvironmentConfig($"{configurationsPath}isi.extensions.environmentsConfig.json");

			configurationBuilder.SetBasePath(System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location));
			configurationBuilder.AddJsonFile("appsettings.json", optional: false);
			configurationBuilder.AddJsonFiles(activeEnvironment.ActiveEnvironments, environment => $"appsettings.{environment}.json");

			var connectionStringPath = string.Format("Configuration{0}", System.IO.Path.DirectorySeparatorChar);

			configurationBuilder.AddClassicConnectionStringsSectionFile($"{connectionStringPath}connectionStrings.config");
			configurationBuilder.AddClassicConnectionStringsSectionFiles(activeEnvironment.ActiveEnvironments, environment => $"{connectionStringPath}connectionStrings.{environment}.config");

			var configuration = configurationBuilder.Build();

			var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
				.AddOptions()
				.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(configuration)
				.AddAllConfigurations(configuration)
				.AddConfiguration<Microsoft.Extensions.Hosting.ConsoleLifetimeOptions>(configuration)
				.AddConfiguration<Microsoft.Extensions.Hosting.HostOptions>(configuration)

				//.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory>()
				.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory, Microsoft.Extensions.Logging.LoggerFactory>()
				.AddLogging(builder => builder
					.AddConsole()
				//.AddFilter(level => level >= Microsoft.Extensions.Logging.LogLevel.Information)
				)
				.AddTransient<Microsoft.Extensions.Logging.ILogger>(serviceProvider => serviceProvider.GetService<ILoggerFactory>().CreateLogger<ServiceExampleApi_Tests>())

				.AddSingleton<Microsoft.Extensions.Caching.Memory.IMemoryCache>(provider => new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()))
				.AddSingleton<ISI.Extensions.Caching.ICacheManager, ISI.Extensions.Caching.CacheManager<Microsoft.Extensions.Caching.Memory.IMemoryCache>>()

				.AddMessageBus(configuration)
				.AddConfigurationRegistrations(configuration)
				.ProcessServiceRegistrars()
				;

			configuration.AddAllConfigurations(services);

			ServiceProvider = services.BuildServiceProvider(configuration);
		}
	}
}