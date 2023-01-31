#region Copyright & License
/*
Copyright (c) 2023 Integrated Solutions, Inc.
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
using System.Runtime.Serialization;
using ISI.Extensions.Extensions;
using LOCALENTITY = ISI.ServiceExample;

namespace ISI.ServiceExample.Repository.SerializableEntities
{
	[ISI.Extensions.Serialization.PreferredSerializerJsonDataContract]
	[ISI.Extensions.Serialization.SerializerContractUuid("300439d2-b3a0-4b87-a2d4-82368ffc9ed6")]
	[DataContract]
	public class MoreComplexObjectV1 : IMoreComplexObject
	{
		public static IMoreComplexObject ToSerializable(LOCALENTITY.MoreComplexObject source)
		{
			var serialized = new MoreComplexObjectV1()
			{
				MoreComplexObjectUuid = source.MoreComplexObjectUuid,
				Name = source.Name,
				Widgets = source.Widgets.ToNullCheckedArray(widget =>
				{
					switch (widget)
					{
						case MoreComplexObjectWidgetA moreComplexObjectWidgetA:
							return MoreComplexObjectWidgetAV1.ToSerializable(moreComplexObjectWidgetA);
						case MoreComplexObjectWidgetB moreComplexObjectWidgetB:
							return MoreComplexObjectWidgetBV1.ToSerializable(moreComplexObjectWidgetB);
						default:
							throw new ArgumentOutOfRangeException(nameof(widget));
					}
				}),
				IsActive = source.IsActive,
				CreatedOnUtc = source.CreatedOnUtc,
				CreatedBy = source.CreatedBy,
				ModifiedOnUtc = source.ModifiedOnUtc,
				ModifiedBy = source.ModifiedBy,
			};

			return serialized;
		}

		public LOCALENTITY.MoreComplexObject Export()
		{
			var result = new LOCALENTITY.MoreComplexObject()
			{
				MoreComplexObjectUuid = MoreComplexObjectUuid,
				Name = Name,
				Widgets = Widgets.ToNullCheckedArray(widget => widget.Export()),
				IsActive = IsActive,
				CreatedOnUtc = CreatedOnUtc,
				CreatedBy = CreatedBy,
				ModifiedOnUtc = ModifiedOnUtc,
				ModifiedBy = ModifiedBy,
			};

			return result;
		}

		[DataMember(Name = "moreComplexObjectUuid", EmitDefaultValue = false)]
		public Guid MoreComplexObjectUuid { get; set; }

		[DataMember(Name = "name", EmitDefaultValue = false)]
		public string Name { get; set; }

		[DataMember(Name = "widgets", EmitDefaultValue = false)]
		public IMoreComplexObjectWidget[] Widgets { get; set; }

		[DataMember(Name = "isActive", EmitDefaultValue = false)]
		public bool IsActive { get; set; }

		[DataMember(Name = "createdOnUtc", EmitDefaultValue = false)]
		public string __CreatedOnUtc { get => CreatedOnUtc.Formatted(DateTimeExtensions.DateTimeFormat.DateTimeUniversalPrecise); set => CreatedOnUtc = value.ToDateTime(); }
		[IgnoreDataMember]
		public DateTime CreatedOnUtc { get; set; }

		[DataMember(Name = "createdBy", EmitDefaultValue = false)]
		public string CreatedBy { get; set; }

		[DataMember(Name = "modifiedOnUtc", EmitDefaultValue = false)]
		public string __ModifiedOnUtc { get => ModifiedOnUtc.Formatted(DateTimeExtensions.DateTimeFormat.DateTimeUniversalPrecise); set => ModifiedOnUtc = value.ToDateTime(); }
		[IgnoreDataMember]
		public DateTime ModifiedOnUtc { get; set; }

		[DataMember(Name = "modifiedBy", EmitDefaultValue = false)]
		public string ModifiedBy { get; set; }
	}
}
