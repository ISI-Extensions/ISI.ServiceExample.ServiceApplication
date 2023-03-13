﻿#region Copyright & License
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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace ISI.ServiceExample.ServiceApplication
{
	public class Startup
	{
		public const string ServiceVersion = "v1";

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddControllersWithViews()
				.AddRazorRuntimeCompilation(options => options.FileProviders.Add(new ISI.Extensions.VirtualFileVolumesFileProvider()))
				.AddNewtonsoftJson(options =>
				{
					options.SerializerSettings.Converters = ISI.Extensions.JsonSerialization.Newtonsoft.NewtonsoftJsonSerializer.JsonConverters();
					options.SerializerSettings.DateParseHandling = global::Newtonsoft.Json.DateParseHandling.None;
				})
				;

			services
				.AddAuthentication(AuthenticationHandler.AuthenticationHandlerName)
				.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, AuthenticationHandler>(AuthenticationHandler.AuthenticationHandlerName, null)
				;

			services.AddAuthorization(options =>
			{
				options.AddPolicy(AuthorizationPolicy.PolicyName, policy => policy.Requirements.Add(new AuthorizationPolicy()));
			});

			services.AddSwaggerGen(swaggerGenOptions =>
			{
				swaggerGenOptions.CustomOperationIds(apiDescription => apiDescription.TryGetMethodInfo(out var methodInfo) ? methodInfo.Name.TrimEnd("Async") : null);

				swaggerGenOptions.SwaggerDoc(ServiceVersion, new Microsoft.OpenApi.Models.OpenApiInfo { Title = typeof(Startup).Namespace, Version = ServiceVersion });

				swaggerGenOptions.AddSecurityDefinition(AuthenticationHandler.Keys.Bearer, new Microsoft.OpenApi.Models.OpenApiSecurityScheme
				{
					Name = AuthenticationHandler.Keys.Authorization,
					Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
					Scheme = AuthenticationHandler.Keys.Bearer,
					In = Microsoft.OpenApi.Models.ParameterLocation.Header,
					Description = "Bearer Authorization header using the Bearer scheme."
				});

				swaggerGenOptions.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
				{
					{
						new Microsoft.OpenApi.Models.OpenApiSecurityScheme
						{
							Reference = new Microsoft.OpenApi.Models.OpenApiReference
							{
								Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
								Id = AuthenticationHandler.Keys.Bearer,
							}
						},
						new string[] {}
					}
				});
			});

			services.AddSwaggerGenNewtonsoftSupport();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder applicationBuilder, IWebHostEnvironment webHostingEnvironment)
		{
			if (webHostingEnvironment.IsDevelopment())
			{
				applicationBuilder.UseDeveloperExceptionPage();
			}

			applicationBuilder.UseSerilogRequestLogging(options =>
			{
				// Customize the message template
				options.MessageTemplate = "Handled {RequestPath}";

				// Emit debug-level events instead of the defaults
				options.GetLevel = (httpContext, elapsed, ex) => Serilog.Events.LogEventLevel.Debug;

				// Attach additional properties to the request completion event
				options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
				{
					diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
					diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
				};
			});

			applicationBuilder.UseDefaultFiles();

			applicationBuilder.UseStaticFiles();

			applicationBuilder.UseRouting();

			//applicationBuilder.UseHttpsRedirection();

			applicationBuilder.UseAuthentication();
			applicationBuilder.UseAuthorization();

			applicationBuilder.UseSwagger();
			applicationBuilder.UseSwaggerUI(c => c.SwaggerEndpoint(string.Format("/swagger/{0}/swagger.json", ServiceVersion), string.Format("{0} {1}", typeof(Startup).Namespace, ServiceVersion)));

			applicationBuilder.UseEndpoints(endpointRouteBuilder =>
			{
				endpointRouteBuilder.MapControllers();
			});
		}
	}
}
