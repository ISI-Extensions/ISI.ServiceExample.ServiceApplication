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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISI.Extensions.Extensions;

namespace ISI.Services.ServiceExample
{
	public class CachedObject : ISI.Extensions.Caching.IHasSettableCacheKeyWithInstanceUuidAndAbsoluteTimeExpiration
	{
		public static string GetCacheKey(Guid cachedObjectUuid) =>  string.Format(ISI.Extensions.Caching.CacheKeyGenerators.GetCacheKeyFormat<CachedObject>(), cachedObjectUuid.Formatted(GuidExtensions.GuidFormat.WithHyphens));
		public static string GetListCacheKey() => "ISI.Services.ServiceExample.CachedObject.ListCachedObjects";

		public Guid CachedObjectUuid { get; set; }

		public string Name { get; set; }

		public bool IsActive { get; set; }

		public DateTime CreatedOnUtc { get; set; }
		public string CreatedBy { get; set; }
		public DateTime ModifiedOnUtc { get; set; }
		public string ModifiedBy { get; set; }

		private string _cacheKey;
		string ISI.Extensions.Caching.IHasCacheKey.CacheKey => _cacheKey;
		string ISI.Extensions.Caching.IHasSettableCacheKey.CacheKey { set => _cacheKey = value; }

		private Guid _cacheKeyInstanceUuid;
		Guid ISI.Extensions.Caching.IHasCacheKeyInstanceUuid.CacheKeyInstanceUuid => _cacheKeyInstanceUuid;
		Guid ISI.Extensions.Caching.IHasSettableCacheKeyInstanceUuid.CacheKeyInstanceUuid { set => _cacheKeyInstanceUuid = value; }

		private DateTime _cacheAbsoluteDateTimeExpiration;
		DateTime ISI.Extensions.Caching.IHasCacheAbsoluteDateTimeExpiration.CacheAbsoluteDateTimeExpiration => _cacheAbsoluteDateTimeExpiration;
		DateTime ISI.Extensions.Caching.IHasSettableCacheAbsoluteDateTimeExpiration.CacheAbsoluteDateTimeExpiration { set => _cacheAbsoluteDateTimeExpiration = value; }
	}
}
