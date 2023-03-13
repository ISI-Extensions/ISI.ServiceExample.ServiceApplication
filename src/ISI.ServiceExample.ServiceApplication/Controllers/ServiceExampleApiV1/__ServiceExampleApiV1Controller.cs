using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISI.Extensions.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ISI.ServiceExample.ServiceApplication.Controllers
{
	public partial class ServiceExampleApiV1Controller : Controller
	{
		public ServiceExampleApiV1Controller(
			Microsoft.Extensions.Logging.ILogger logger)
			: base(logger)
		{
		}
	}
}