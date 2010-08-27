using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetSerializerTest
{
	[Serializable]
	struct BasicTypesStruct
	{
		public byte Byte { get; set; }

		public short Short { get; set; }

		public int Int { get; set; }

		//public long Long { get; set; }

		public bool Bool { get; set; }

		public string String { get; set; }
	}

	[Serializable]
	[ProtoBuf.ProtoContract]
	[ProtoBuf.ProtoInclude(1, typeof(BasicTypesClass))]
	[ProtoBuf.ProtoInclude(2, typeof(ComplexTypesMessage))]
	class Message
	{
	}

	[Serializable]
	[ProtoBuf.ProtoContract]
	class BasicTypesClass : Message
	{
		[ProtoBuf.ProtoMember(1)]
		public byte Byte { get; set; }
		
		[ProtoBuf.ProtoMember(2)]
		public short Short { get; set; }

		[ProtoBuf.ProtoMember(3)]
		public int Int { get; set; }

		//public long Long { get; set; }

		[ProtoBuf.ProtoMember(4)]
		public bool Bool { get; set; }

		[ProtoBuf.ProtoMember(5)]
		public string String { get; set; }
	}

	[Serializable]
	[ProtoBuf.ProtoContract]
	class ComplexTypesMessage : Message
	{
		[ProtoBuf.ProtoMember(1)]
		public Message[] ClassArray { get; set; }

		[ProtoBuf.ProtoMember(2)]
		public List<Message> ClassList { get; set; }
#if !USE_PROTOBUF
		public BasicTypesStruct[] StructArray { get; set; }
#endif
	}
}
