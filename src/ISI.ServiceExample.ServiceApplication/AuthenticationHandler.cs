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
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ISI.ServiceExample.ServiceApplication
{
	public class AuthenticationHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
	{
		public const string AuthenticationHandlerName = nameof(AuthenticationHandler);

		public class Keys
		{
			public const string Authorization = nameof(Authorization);
			public const string Bearer = nameof(Bearer);
			public const string UserUuid = nameof(UserUuid);
		}

		protected Configuration Configuration { get; }

		public AuthenticationHandler(
			Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
			Microsoft.Extensions.Logging.ILoggerFactory logger,
			System.Text.Encodings.Web.UrlEncoder encoder,
			Microsoft.AspNetCore.Authentication.ISystemClock clock,
			Configuration configuration)
			: base(options, logger, encoder, clock)
		{
			Configuration = configuration;
		}

		protected override async Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
		{
			// skip authentication if endpoint has [AllowAnonymous] attribute
			var endpoint = Context.GetEndpoint();
			if (endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() != null)
			{
				return Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult();
			}

			if (!Request.Headers.ContainsKey(Keys.Authorization))
			{
				return Microsoft.AspNetCore.Authentication.AuthenticateResult.Fail("Missing Authorization Header");
			}

			try
			{
				var authHeader = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(Request.Headers[Keys.Authorization]);

				var identityApiToken = authHeader?.Parameter?.NullCheckedTrimStart(Keys.Bearer)?.NullCheckedTrim() ?? string.Empty;

				if (string.IsNullOrWhiteSpace(Configuration.ServiceApiToken) || string.Equals(identityApiToken, Configuration.ServiceApiToken, StringComparison.InvariantCultureIgnoreCase))
				{
					var userUuid = Guid.NewGuid();

					Request.HttpContext.Items[Keys.UserUuid] = userUuid.Formatted(GuidExtensions.GuidFormat.WithHyphens);

					var principal = new System.Security.Claims.ClaimsPrincipal(new[]
					{
						new System.Security.Claims.ClaimsIdentity(new[]
						{
							new System.Security.Claims.Claim(Keys.UserUuid, userUuid.Formatted(GuidExtensions.GuidFormat.WithHyphens)),
						}),
					});

					var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, Scheme.Name);

					return Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket);
				}

				return Microsoft.AspNetCore.Authentication.AuthenticateResult.Fail("Invalid Token");
			}
			catch
			{
				return Microsoft.AspNetCore.Authentication.AuthenticateResult.Fail("Invalid Authorization Header");
			}
		}
	}
}