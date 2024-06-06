#region Copyright & License
/*
Copyright (c) 2024, Integrated Solutions, Inc.
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
using ISI.Extensions.Extensions;
using DTOs = ISI.Extensions.AuthenticationIdentity.DataTransferObjects;
 
namespace ISI.ServiceExample.ServiceApplication
{
	public class AuthenticationIdentityApi : ISI.Extensions.IAuthenticationIdentityApi
	{
		public static Guid ApiKeyUserUuid = Guid.Parse("5f18eb6e-3854-4dad-893a-9db6bad02a73");
		
		public const string AdministratorRole = "ServiceExample-Administrator";

		protected ISI.Services.ServiceExample.Configuration Configuration { get; }
		
		public AuthenticationIdentityApi(
			ISI.Services.ServiceExample.Configuration configuration)
		{
			Configuration = configuration;
		}

		public async Task<DTOs.ListRolesResponse> ListRolesAsync(DTOs.ListRolesRequest request, System.Threading.CancellationToken cancellationToken = default)
		{
			var response = new DTOs.ListRolesResponse();

			response.Roles = new[]
			{
				(Role: AdministratorRole, Description: "Administrator"),
			};

			return response;
		}

		public async Task<DTOs.GetUsersResponse> GetUsersAsync(DTOs.GetUsersRequest request, System.Threading.CancellationToken cancellationToken = default)
		{
			var response = new DTOs.GetUsersResponse();

			var users = new List<ISI.Extensions.IAuthenticationIdentityUser>();

			if (request.UserUuids.Contains(ApiKeyUserUuid))
			{
				users.Add(new ISI.Extensions.AuthenticationIdentity.AuthenticationIdentityUser()
				{
					UserUuid = ApiKeyUserUuid,
					LastName = "ApiKeyUser",
					Roles = new[] { AdministratorRole },
					IsActive = true,
				});
			}

			response.Users = users.ToArray();

			return response;
		}

		public async Task<DTOs.ValidateApiKeyResponse> ValidateApiKeyAsync(DTOs.ValidateApiKeyRequest request, System.Threading.CancellationToken cancellationToken = default)
		{
			var response = new DTOs.ValidateApiKeyResponse();

			if (string.Equals(request.ApiKey, Configuration.ServiceExampleApiToken, StringComparison.InvariantCultureIgnoreCase))
			{
				response.UserUuid = ApiKeyUserUuid;
			}

			return response;
		}
	}
}
