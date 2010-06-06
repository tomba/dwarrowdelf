using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Xml;
using System.Runtime.Serialization;

namespace NetSerializer
{
	class Program
	{
		static void Main(string[] args)
		{
			Benchmark();
		}

		static void Benchmark()
		{
			const int loops = 1000;
			var obs = CreateObjects();

			var test1 = new NetSerializerTest(obs);
			test1.Run(loops);
			Console.WriteLine("maxsize {0}", test1.Maxsize);

			var test2 = new BinaryFormatterTest(obs);
			test2.Run(loops);
			Console.WriteLine("maxsize {0}", test2.Maxsize);

			Console.WriteLine("done, press enter");
			Console.ReadLine();
		}

		static object[] CreateObjects()
		{
			const int m = 4;
			var arr = new Message[1000];
			for (int i = 0; i < arr.Length; ++i)
				arr[i] = new BasicMessage()
				{
					A = (byte)(i * m),
					B = (short)(i * m + 1),
					C = i * m + 2,
					String = (i * m + 3).ToString()
				};

			return new object[] { new ArrayMessage() { Array = arr } };
		}
	}

	[Serializable]
	class Message
	{
	}

	[Serializable]
	class BasicMessage : Message
	{
		public byte A { get; set; }
		public short B { get; set; }
		public int C { get; set; }
		public string String { get; set; }
	}

	[Serializable]
	class ArrayMessage : Message
	{
		public Message[] Array { get; set; }
	}

	class NetSerializerTest : Test
	{
		public int Maxsize { get; set; }
		Stream m_stream;
		object[] m_obs;

		public NetSerializerTest(object[] obs)
		{
			m_stream = new MemoryStream(1024*1024);
			m_obs = obs;

			IEnumerable<Type> rootTypes = new Type[0];

			var messageTypes = typeof(Message).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Message)));
			rootTypes = rootTypes.Concat(messageTypes);
			NetSerializer.Serializer.Initialize(rootTypes.ToArray());
		}

		protected override void RunOverride(int loops)
		{
			Maxsize = 0;

			for (int l = 0; l < loops; ++l)
			{
				m_stream.Seek(0, SeekOrigin.Begin);

				foreach (var o in m_obs)
					NetSerializer.Serializer.Serialize(m_stream, o);

				if (l == 0)
					Maxsize = (int)m_stream.Position;

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
		public int Maxsize { get; set; }
		Stream m_stream;
		object[] m_obs;

		public BinaryFormatterTest(object[] obs)
		{
			m_stream = new MemoryStream(1024*1024);
			m_obs = obs;
		}

		protected override void RunOverride(int loops)
		{
			Maxsize = 0;

			var bf = new BinaryFormatter();

			for (int l = 0; l < loops; ++l)
			{
				m_stream.Seek(0, SeekOrigin.Begin);

				foreach (var o in m_obs)
					bf.Serialize(m_stream, o);

				if (l == 0)
					Maxsize = (int)m_stream.Position;

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
}
