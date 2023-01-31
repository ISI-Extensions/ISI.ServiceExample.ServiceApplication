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

namespace ISI.ServiceExample.Repository
{
	[ISI.Extensions.Repository.Record(Schema = "Example",TableName = "MoreComplexObjects")]
	public class MoreComplexObjectRecord : ISI.Extensions.Repository.IRecordManagerPrimaryKeyRecord<Guid>, ISI.Extensions.Repository.IRecordManagerRecordWithArchiveDateTime, ISI.Extensions.Repository.IRecordIndexDescriptions<MoreComplexObjectRecord>
	{
		[ISI.Extensions.Repository.PrimaryKey]
		[ISI.Extensions.Repository.RecordProperty(ColumnName = "MoreComplexObjectUuid")]
		public Guid MoreComplexObjectUuid { get; set; }

		[ISI.Extensions.Repository.RecordProperty(ColumnName = "Name", PropertySize = 255)]
		public string Name { get; set; }

		[ISI.Extensions.Repository.RecordProperty(ColumnName = "MoreComplexObject")]
		public ISI.ServiceExample.Repository.SerializableEntities.IMoreComplexObject MoreComplexObject { get; set; }

		[ISI.Extensions.Repository.RecordProperty(ColumnName = "IsActive")]
		public bool IsActive { get; set; }

		[ISI.Extensions.Repository.RecordProperty(ColumnName = "ModifiedOnUtc")]
		public DateTime ModifiedOnUtc { get; set; }

		Guid ISI.Extensions.Repository.IRecordManagerPrimaryKeyRecord<Guid>.PrimaryKey => MoreComplexObjectUuid;

		DateTime ISI.Extensions.Repository.IRecordManagerRecordWithArchiveDateTime.ArchiveDateTime => ModifiedOnUtc;

		ISI.Extensions.Repository.RecordIndexCollection<MoreComplexObjectRecord> ISI.Extensions.Repository.IRecordIndexDescriptions<MoreComplexObjectRecord>.GetRecordIndexes()
		{
			return new ISI.Extensions.Repository.RecordIndexCollection<MoreComplexObjectRecord>()
			{
				{
					new ISI.Extensions.Repository.RecordIndexColumnCollection<MoreComplexObjectRecord>()
					{
						{record => record.IsActive},
					}
				},
				{
					new ISI.Extensions.Repository.RecordIndexColumnCollection<MoreComplexObjectRecord>()
					{
						{record => record.Name},
						{record => record.IsActive},
					}
				},
			};
		}
	}
}