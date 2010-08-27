using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NetSerializerTest
{
	class PerfTest
	{
		const int NUMMESSAGES = 100;
		const int NUMITEMS = 100;
		const int LOOPS = 100;

		public void Run()
		{
			var obs = CreateObjects();

			var test1 = new NetSerializerTest(obs);
			test1.Run(LOOPS);
			Console.WriteLine("maxsize {0}", test1.Size);

			var test2 = new BinaryFormatterTest(obs);
			test2.Run(LOOPS);
			Console.WriteLine("maxsize {0}", test2.Size);

#if USE_PROTOBUF
			var test3 = new ProtoBufTest(obs);
			test3.Run(LOOPS);
			Console.WriteLine("maxsize {0}", test3.Size);
#endif
			Console.WriteLine("done, press enter");
			Console.ReadLine();
		}

		static object[] CreateObjects()
		{
			List<Message> list = new List<Message>();

			for (int x = 0; x < NUMMESSAGES; ++x)
			{
				const int m = 4;

				var ctm = new ComplexTypesMessage();
#if !USE_PROTOBUF
				{
					var arr = new BasicTypesStruct[NUMITEMS];

					for (int i = 0; i < arr.Length; ++i)
					{
						int z = 0;

						arr[i] = new BasicTypesStruct()
						{
							Byte = (byte)(i * m + z++),
							Short = (short)(i * m + z++),
							Int = i * m + z++,
							//Long = i * m + z++,
							Bool = i % 2 == 0,
							String = (i * m + z++).ToString()
						};
					}

					ctm.StructArray = arr;
				}
#endif

				{
					var arr = new Message[NUMITEMS];

					for (int i = 0; i < arr.Length; ++i)
					{
						int z = 0;

						arr[i] = new BasicTypesClass()
						{
							Byte = (byte)(i * m + z++),
							Short = (short)(i * m + z++),
							Int = i * m + z++,
							//Long = i * m + z++,
							Bool = i % 2 == 0,
							String = (i * m + z++).ToString()
						};
					}

					ctm.ClassArray = arr;
				}

				{
					var arr = new Message[NUMITEMS];

					for (int i = 0; i < arr.Length; ++i)
					{
						int z = 0;

						arr[i] = new BasicTypesClass()
						{
							Byte = (byte)(i * m + z++),
							Short = (short)(i * m + z++),
							Int = i * m + z++,
							//Long = i * m + z++,
							Bool = i % 2 == 0,
							String = (i * m + z++).ToString()
						};
					}

					ctm.ClassList = new List<Message>(arr);
				}

				list.Add(ctm);
			}

			return list.ToArray();
		}
	}

	class NetSerializerTest : Test
	{
		public int Size { get; private set; }
		Stream m_stream;
		object[] m_obs;

		public NetSerializerTest(object[] obs)
		{
			m_stream = new MemoryStream(1024 * 1024);
			m_obs = obs;

			var messageTypes = typeof(Message).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Message)));
			NetSerializer.Serializer.Initialize(messageTypes.ToArray());
		}

		protected override void RunOverride(int loops)
		{
			this.Size = 0;

			for (int l = 0; l < loops; ++l)
			{
				m_stream.Seek(0, SeekOrigin.Begin);

				foreach (var o in m_obs)
					NetSerializer.Serializer.Serialize(m_stream, o);

				if (l == 0)
					this.Size = (int)m_stream.Position;

				m_stream.Seek(0, SeekOrigin.Begin);

				while (m_stream.Position < m_stream.Length)
				{
					var ob = (Message)NetSerializer.Serializer.Deserialize(m_stream);
					if (ob == null)
						throw new Exception();
				}
			}
		}
	}

	class BinaryFormatterTest : Test
	{
		public int Size { get; private set; }
		Stream m_stream;
		object[] m_obs;

		public BinaryFormatterTest(object[] obs)
		{
			m_stream = new MemoryStream(1024 * 1024);
			m_obs = obs;
		}

		protected override void RunOverride(int loops)
		{
			this.Size = 0;

			var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

			for (int l = 0; l < loops; ++l)
			{
				m_stream.Seek(0, SeekOrigin.Begin);

				foreach (var o in m_obs)
					bf.Serialize(m_stream, o);

				if (l == 0)
					this.Size = (int)m_stream.Position;

				m_stream.Seek(0, SeekOrigin.Begin);

				while (m_stream.Position < m_stream.Length)
				{
					var ob = (Message)bf.Deserialize(m_stream);
					if (ob == null)
						throw new Exception();
				}
			}
		}
	}

	class ProtoBufTest : Test
	{
		public int Size { get; private set; }
		Stream m_stream;
		object[] m_obs;

		public ProtoBufTest(object[] obs)
		{
			m_stream = new MemoryStream(1024 * 1024);
			m_obs = obs;
		}

		protected override void RunOverride(int loops)
		{
			this.Size = 0;

			for (int l = 0; l < loops; ++l)
			{
				m_stream.Seek(0, SeekOrigin.Begin);

				foreach (var o in m_obs)
					ProtoBuf.Serializer.Serialize(m_stream, (ComplexTypesMessage)o);

				if (l == 0)
					this.Size = (int)m_stream.Position;

				m_stream.Seek(0, SeekOrigin.Begin);

				while (m_stream.Position < m_stream.Length)
				{
					var ob = ProtoBuf.Serializer.Deserialize<Message>(m_stream);
					if (ob == null)
						throw new Exception();
				}
			}
		}
	}
}

