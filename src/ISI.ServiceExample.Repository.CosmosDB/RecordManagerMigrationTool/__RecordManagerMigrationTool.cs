using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISI.Extensions.Extensions;
using DTOs = ISI.ServiceExample.Repository.DataTransferObjects.RecordManagerMigrationTool;

namespace ISI.ServiceExample.Repository.CosmosDB
{
	public partial class RecordManagerMigrationTool : IRecordManagerMigrationTool
	{
		protected Configuration Configuration { get; }

		protected System.IServiceProvider ServiceProvider { get; }
		protected Microsoft.Extensions.Configuration.IConfiguration ConfigurationRoot { get; }
		protected Microsoft.Extensions.Logging.ILogger Logger { get; }
		protected ISI.Extensions.DateTimeStamper.IDateTimeStamper DateTimeStamper { get; }
		protected ISI.Extensions.JsonSerialization.IJsonSerializer Serializer { get; }

		public RecordManagerMigrationTool(
			Configuration configuration,
			System.IServiceProvider serviceProvider,
			Microsoft.Extensions.Configuration.IConfiguration configurationRoot,
			Microsoft.Extensions.Logging.ILogger logger,
			ISI.Extensions.DateTimeStamper.IDateTimeStamper dateTimeStamper,
			ISI.Extensions.JsonSerialization.IJsonSerializer serializer)
		{
			Configuration = configuration;
			ServiceProvider = serviceProvider;
			ConfigurationRoot = configurationRoot;
			Logger = logger;
			DateTimeStamper = dateTimeStamper;
			Serializer = serializer;
		}
	}
}