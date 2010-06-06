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
			const int loops = 100;
			var obs = CreateObjects();

			var test1 = new NetSerializerTest(obs);
			test1.Run(loops);
			Console.WriteLine("maxsize {0}", test1.Maxsize);
			VerifyObjects(test1.Test());

			var test2 = new BinaryFormatterTest(obs);
			test2.Run(loops);
			Console.WriteLine("maxsize {0}", test2.Maxsize);
			VerifyObjects(test2.Test());

			Console.WriteLine("done, press enter");
			Console.ReadLine();
		}

		const int NUMITEMS = 1000;
		static object[] CreateObjects()
		{
			List<Message> list = new List<Message>();

			{
				const int m = 4;
				var arr = new Message[NUMITEMS];
				for (int i = 0; i < arr.Length; ++i)
					arr[i] = new BasicMessage()
					{
						Byte = (byte)(i * m),
						Short = (short)(i * m + 1),
						Int = i * m + 2,
						Bool = i % 2 == 0,
						String = (i * m + 3).ToString()
					};
				list.Add(new ArrayMessage() { Array = arr });
			}

			{
				const int m = 4;
				var arr = new TestStruct[NUMITEMS];
				for (int i = 0; i < arr.Length; ++i)
					arr[i] = new TestStruct()
					{
						Byte = (byte)(i * m),
						Short = (short)(i * m + 1),
						Int = i * m + 2,
						Bool = i % 2 == 0,
						String = (i * m + 3).ToString()
					};
				list.Add(new StructMessage() { Array = arr });
			}

			return list.ToArray();
		}

		static void VerifyObjects(object[] obs)
		{
			const int m = 4;

			if (obs.Length != 2)
				throw new Exception();

			var msg1 = (ArrayMessage)obs[0];
			if (msg1.Array.Length != NUMITEMS)
				throw new Exception();
			for (int i = 0; i < NUMITEMS; ++i)
			{
				var msg = (BasicMessage)msg1.Array[i];

				if (msg.Byte != (byte)(i * m))
					throw new Exception();

				if (msg.Short != (short)(i * m + 1))
					throw new Exception();

				if (msg.Int != i * m + 2)
					throw new Exception();

				if (msg.Bool != (i % 2 == 0))
					throw new Exception();

				if (msg.String != (i * m + 3).ToString())
					throw new Exception();
			}

			var msg2 = (StructMessage)obs[1];
			if (msg2.Array.Length != NUMITEMS)
				throw new Exception();
			for (int i = 0; i < NUMITEMS; ++i)
			{
				var msg = msg2.Array[i];

				if (msg.Byte != (byte)(i * m))
					throw new Exception();

				if (msg.Short != (short)(i * m + 1))
					throw new Exception();

				if (msg.Int != i * m + 2)
					throw new Exception();

				if (msg.Bool != (i % 2 == 0))
					throw new Exception();

				if (msg.String != (i * m + 3).ToString())
					throw new Exception();
			}
		}
	}

	[Serializable]
	class Message
	{
	}

	[Serializable]
	class BasicMessage : Message
	{
		public byte Byte { get; set; }
		public short Short { get; set; }
		public int Int { get; set; }
		public bool Bool { get; set; }
		public string String { get; set; }
	}

	[Serializable]
	class ArrayMessage : Message
	{
		public Message[] Array { get; set; }
	}

	[Serializable]
	struct TestStruct
	{
		public byte Byte { get; set; }
		public short Short { get; set; }
		public int Int { get; set; }
		public bool Bool { get; set; }
		public string String { get; set; }
	}

	[Serializable]
	class StructMessage : Message
	{
		public TestStruct[] Array { get; set; }
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

		public object[] Test()
		{
			m_stream.Seek(0, SeekOrigin.Begin);

			foreach (var o in m_obs)
				NetSerializer.Serializer.Serialize(m_stream, o);

			m_stream.Seek(0, SeekOrigin.Begin);

			var arr = new object[m_obs.Length];
			int x = 0;
			while (m_stream.Position < m_stream.Length)
			{
				var ob = (Message)NetSerializer.Serializer.Deserialize(m_stream);
				if (ob == null)
					throw new Exception();
				arr[x++] = ob;
			}

			return arr;
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

		public object[] Test()
		{
			var bf = new BinaryFormatter();

			m_stream.Seek(0, SeekOrigin.Begin);

			foreach (var o in m_obs)
				bf.Serialize(m_stream, o);

			m_stream.Seek(0, SeekOrigin.Begin);

			var arr = new object[m_obs.Length];
			int x = 0;
			while (m_stream.Position < m_stream.Length)
			{
				var ob = (Message)bf.Deserialize(m_stream);
				if (ob == null)
					throw new Exception();
				arr[x++] = ob;
			}

			return arr;
		}
	}
}
