using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISI.ServiceExample.ServiceApplication
{
	[ISI.Extensions.ConfigurationHelper.Configuration(ConfigurationSectionName)]
	public partial class Configuration : ISI.Extensions.ConfigurationHelper.IConfiguration
	{
		public const string ConfigurationSectionName = "ISI.ServiceExample.ServiceApplication";

		public ElasticsearchLoggingConfiguration ElasticsearchLogging { get; set; } = new();

		public bool UseMessageBus { get; set; } = true;

		public string ServiceApiToken { get; set; }

		public string LogDirectory { get; set; }
	}
}