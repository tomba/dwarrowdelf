using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetSerializerTest
{
	[Serializable]
	struct BasicTypesStruct
	{
		public byte Byte;

		public short Short;

		public int Int;

		public long Long;

		public bool Bool;

		public string String;

		public BasicTypesStruct(int seed)
		{
			const int m = 6;
			int i = 0;

			Byte = (byte)(seed * m + i++);
			Short = (short)(seed * m + i++);
			Int = seed * m + i++;
			Long = seed * m + i++;
			Bool = seed % 2 == 0;
			String = (seed * m + i++).ToString();
		}
	}

	[Serializable]
	[ProtoBuf.ProtoContract]
	[ProtoBuf.ProtoInclude(1, typeof(BasicTypesClass))]
	[ProtoBuf.ProtoInclude(2, typeof(ComplexTypesMessage))]
	abstract class Message
	{
	}

	[Serializable]
	[ProtoBuf.ProtoContract]
	[ProtoBuf.ProtoInclude(10, typeof(SealedBasicTypesClass))]
	class BasicTypesClass : Message
	{
		[ProtoBuf.ProtoMember(1)]
		public byte Byte;

		[ProtoBuf.ProtoMember(2)]
		public short Short;

		[ProtoBuf.ProtoMember(3)]
		public int Int;

		[ProtoBuf.ProtoMember(4)]
		public long Long;

		[ProtoBuf.ProtoMember(5)]
		public bool Bool;

		[ProtoBuf.ProtoMember(6)]
		public string String;

		public BasicTypesClass()
		{

		}

		public BasicTypesClass(int seed)
		{
			const int m = 6;
			int i = 0;

			Byte = (byte)(seed * m + i++);
			Short = (short)(seed * m + i++);
			Int = seed * m + i++;
			Long = seed * m + i++;
			Bool = seed % 2 == 0;
			String = (seed * m + i++).ToString();
		}

		public void Verify(int seed)
		{
			var o = new BasicTypesClass(seed);

			if (o.Byte == this.Byte &&
				o.Short == this.Short &&
				o.Int == this.Int &&
				o.Bool == this.Bool &&
				o.String == this.String)
				return;

			throw new Exception();
		}
	}

	[Serializable]
	[ProtoBuf.ProtoContract]
	sealed class SealedBasicTypesClass : BasicTypesClass
	{
		public SealedBasicTypesClass()
		{

		}

		public SealedBasicTypesClass(int seed)
			: base(seed)
		{

		}
	}

	[Serializable]
	[ProtoBuf.ProtoContract]
	class ComplexTypesMessage : Message
	{
		[ProtoBuf.ProtoMember(1)]
		public Message Message;

		[ProtoBuf.ProtoMember(2)]
		public Message NullMessage;

		[ProtoBuf.ProtoMember(3)]
		public BasicTypesClass BasicTypesClass;

		[ProtoBuf.ProtoMember(4)]
		public BasicTypesClass NullBasicTypesClass;

		[ProtoBuf.ProtoMember(5)]
		public SealedBasicTypesClass SealedBasicTypesClass;

		[ProtoBuf.ProtoMember(6)]
		public SealedBasicTypesClass NullSealedBasicTypesClass;

		[ProtoBuf.ProtoMember(7)]
		public Message[] ClassArray;

		[ProtoBuf.ProtoMember(8)]
		public List<Message> ClassList;

#if !USE_PROTOBUF
		public BasicTypesStruct[] StructArray;
#endif

		public ComplexTypesMessage()
		{

		}

		public ComplexTypesMessage(int numItems)
		{
			this.Message = new BasicTypesClass(10);
			this.NullMessage = null;

			this.BasicTypesClass = new BasicTypesClass(11);
			this.NullBasicTypesClass = null;

			this.SealedBasicTypesClass = new SealedBasicTypesClass(12);
			this.NullSealedBasicTypesClass = null;

#if !USE_PROTOBUF
				{
					var arr = new BasicTypesStruct[numItems];

					for (int i = 0; i < arr.Length; ++i)
						arr[i] = new BasicTypesStruct(i);

					this.StructArray = arr;
				}
#endif

			{
				var arr = new Message[numItems];

				for (int i = 0; i < arr.Length; ++i)
					arr[i] = new BasicTypesClass(i);

				this.ClassArray = arr;
			}

			{
				var arr = new Message[numItems];

				for (int i = 0; i < arr.Length; ++i)
					arr[i] = new BasicTypesClass(i);

				this.ClassList = new List<Message>(arr);
			}
		}

		public void Verify()
		{
			if (this.NullMessage != null || this.NullBasicTypesClass != null || this.NullSealedBasicTypesClass != null)
				throw new Exception();


			((BasicTypesClass)this.Message).Verify(10);
			((BasicTypesClass)this.BasicTypesClass).Verify(11);
			((SealedBasicTypesClass)this.SealedBasicTypesClass).Verify(12);


#if !USE_PROTOBUF
			for (int i = 0; i < this.StructArray.Length; ++i)
			{
				var ob1 = new BasicTypesStruct(i);
				var ob2 = this.StructArray[i];
				if (ob2.Equals(ob1) == false)
					throw new Exception();
			}
#endif
			for (int i = 0; i < this.ClassArray.Length; ++i)
			{
				var ob = (BasicTypesClass)this.ClassArray[i];
				ob.Verify(i);
			}

			for (int i = 0; i < this.ClassList.Count; ++i)
			{
				var ob2 = (BasicTypesClass)this.ClassList[i];
				ob2.Verify(i);
			}
		}
	}
}
