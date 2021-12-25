﻿#region Copyright & License
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
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ISI.ServiceExample.Service
{
	public class ServiceManager : Microsoft.Extensions.Hosting.IHostedService, ISI.Extensions.IServiceManager
	{
		protected System.IServiceProvider ServiceProvider { get; }
		protected Microsoft.Extensions.Logging.ILogger Logger { get; }
		protected ISI.Extensions.MessageBus.IMessageBus MessageBus { get; }

		public ISI.Extensions.ServiceManager.Status Status { get; private set; }

		public ServiceManager(
			System.IServiceProvider serviceProvider,
			Microsoft.Extensions.Logging.ILogger logger,
			ISI.Extensions.MessageBus.IMessageBus messageBus)
		{
			ServiceProvider = serviceProvider;
			Logger = logger;
			MessageBus = messageBus;
		}

		public async Task StartAsync(System.Threading.CancellationToken cancellationToken)
		{
			MessageBus.Build(ServiceProvider, new ISI.Extensions.MessageBus.MessageBusBuildRequestCollection()
			{
				ISI.ServiceExample.Service.MessageQueue.Subscriptions.GetAddSubscriptions,
				ISI.Extensions.Caching.MessageBus.Subscriptions.GetAddSubscriptions,
			});

			await MessageBus.StartAsync(cancellationToken);
		}

		public async Task StopAsync(System.Threading.CancellationToken cancellationToken)
		{
			await MessageBus.StopAsync(cancellationToken);

			ISI.Extensions.Threads.ExitAll();
		}

		public void BeforeInstall()
		{
		}

		public void AfterInstall()
		{
		}

		public void BeforeUninstall()
		{
		}

		public void AfterUninstall()
		{
		}
	}
}
