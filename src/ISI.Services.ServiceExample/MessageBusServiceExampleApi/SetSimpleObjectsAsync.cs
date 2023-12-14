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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISI.Extensions.Caching.Extensions;
using ISI.Extensions.Extensions;
using ISI.Services.ServiceExample.SerializableModels.MessageBus.ServiceExampleApiV1;
using DOMAINENTITY = ISI.Services.ServiceExample;
using DTOs = ISI.Services.ServiceExample.DataTransferObjects.ServiceExampleApi;
using MESSAGEBUS = ISI.Services.ServiceExample.SerializableModels.MessageBus.ServiceExampleApiV1;

namespace ISI.Services.ServiceExample
{
	public partial class MessageBusServiceExampleApi
	{
		public async Task<DTOs.SetSimpleObjectsResponse> SetSimpleObjectsAsync(DTOs.SetSimpleObjectsRequest request, System.Threading.CancellationToken cancellationToken = default)
		{
			var response = new DTOs.SetSimpleObjectsResponse();
			
			var serviceRequest = new MESSAGEBUS.SetSimpleObjectsRequest()
			{
				SimpleObjects = request.SimpleObjects.ToNullCheckedArray(Convert),
			};

			var serviceResponse = await MessageBus.PublishAsync<SetSimpleObjectsRequest, SetSimpleObjectsResponse>(serviceRequest, cancellationToken: cancellationToken);

			response.SimpleObjects = serviceResponse.SimpleObjects.NullCheckedSelect(Convert);

			return response;
		}
	}
}