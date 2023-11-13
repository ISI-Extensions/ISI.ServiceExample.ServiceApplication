#region Copyright & License
/*
Copyright (c) 2023, Integrated Solutions, Inc.
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

		* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
		* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
		* Neither the name of the Integrated Solutions, Inc. nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion

using ISI.Extensions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MESSAGEBUS = ISI.Services.ServiceExample.SerializableModels.MessageBus.ServiceExampleApiV1;

namespace ISI.ServiceExample.ServiceApplication.MessageBus
{
	public partial class Subscriptions
	{
		public class ServiceExampleApiV1
		{
			public static ISI.Extensions.MessageBus.IMessageBusBuildRequest GetAddSubscriptions()
			{
				var response = new ISI.Extensions.MessageBus.DefaultMessageBusBuildRequest();

				response.AddSubscriptions.Add(messageQueueConfigurator =>
				{
					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.SetSimpleObjectsRequest, MESSAGEBUS.SetSimpleObjectsResponse>(async (service, request, cancellationToken) => await service.SetSimpleObjectsAsync(request, cancellationToken));
					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.GetSimpleObjectsRequest, MESSAGEBUS.GetSimpleObjectsResponse>(async (service, request, cancellationToken) => await service.GetSimpleObjectsAsync(request, cancellationToken));
					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.ListSimpleObjectsRequest, MESSAGEBUS.ListSimpleObjectsResponse>(async (service, request, cancellationToken) => await service.ListSimpleObjectsAsync(request, cancellationToken));
					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.FindSimpleObjectsByNameRequest, MESSAGEBUS.FindSimpleObjectsByNameResponse>(async (service, request, cancellationToken) => await service.FindSimpleObjectsByNameAsync(request, cancellationToken));

					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.SetComplexObjectsRequest, MESSAGEBUS.SetComplexObjectsResponse>(async (service, request, cancellationToken) => await service.SetComplexObjectsAsync(request, cancellationToken));
					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.GetComplexObjectsRequest, MESSAGEBUS.GetComplexObjectsResponse>(async (service, request, cancellationToken) => await service.GetComplexObjectsAsync(request, cancellationToken));
					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.ListComplexObjectsRequest, MESSAGEBUS.ListComplexObjectsResponse>(async (service, request, cancellationToken) => await service.ListComplexObjectsAsync(request, cancellationToken));
					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.FindComplexObjectsByNameRequest, MESSAGEBUS.FindComplexObjectsByNameResponse>(async (service, request, cancellationToken) => await service.FindComplexObjectsByNameAsync(request, cancellationToken));

					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.SetMoreComplexObjectsRequest, MESSAGEBUS.SetMoreComplexObjectsResponse>(async (service, request, cancellationToken) => await service.SetMoreComplexObjectsAsync(request, cancellationToken));
					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.GetMoreComplexObjectsRequest, MESSAGEBUS.GetMoreComplexObjectsResponse>(async (service, request, cancellationToken) => await service.GetMoreComplexObjectsAsync(request, cancellationToken));
					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.ListMoreComplexObjectsRequest, MESSAGEBUS.ListMoreComplexObjectsResponse>(async (service, request, cancellationToken) => await service.ListMoreComplexObjectsAsync(request, cancellationToken));
					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.FindMoreComplexObjectsByNameRequest, MESSAGEBUS.FindMoreComplexObjectsByNameResponse>(async (service, request, cancellationToken) => await service.FindMoreComplexObjectsByNameAsync(request, cancellationToken));

					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.SetCachedObjectsRequest, MESSAGEBUS.SetCachedObjectsResponse>(async (service, request, cancellationToken) => await service.SetCachedObjectsAsync(request, cancellationToken));
					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.GetCachedObjectsRequest, MESSAGEBUS.GetCachedObjectsResponse>(async (service, request, cancellationToken) => await service.GetCachedObjectsAsync(request, cancellationToken));
					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.ListCachedObjectsRequest, MESSAGEBUS.ListCachedObjectsResponse>(async (service, request, cancellationToken) => await service.ListCachedObjectsAsync(request, cancellationToken));
					messageQueueConfigurator.Subscribe<Controllers.ServiceExampleApiV1Controller, MESSAGEBUS.FindCachedObjectsByNameRequest, MESSAGEBUS.FindCachedObjectsByNameResponse>(async (service, request, cancellationToken) => await service.FindCachedObjectsByNameAsync(request, cancellationToken));
				});

				return response;
			}
		}
	}
}