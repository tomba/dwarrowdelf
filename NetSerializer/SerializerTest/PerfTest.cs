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
			VerifyObjects(test1.Result);
			Console.WriteLine("maxsize {0}", test1.Size);

			var test2 = new BinaryFormatterTest(obs);
			test2.Run(LOOPS);
			VerifyObjects(test2.Result);
			Console.WriteLine("maxsize {0}", test2.Size);

#if USE_PROTOBUF
			var test3 = new ProtoBufTest(obs);
			test3.Run(LOOPS);
			VerifyObjects(test3.Result);
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
				var ctm = new ComplexTypesMessage(NUMITEMS);
				list.Add(ctm);
			}

			return list.ToArray();
		}

		static void VerifyObjects(object[] objects)
		{
			for (int x = 0; x < NUMMESSAGES; ++x)
			{
				var ctm = (ComplexTypesMessage)objects[x];
				ctm.Verify();
			}
		}
	}

	class NetSerializerTest : Test
	{
		public int Size { get; private set; }
		Stream m_stream;
		object[] m_obs;
		object[] m_resultObs;
		public object[] Result { get { return m_resultObs; } }

		public NetSerializerTest(object[] obs)
		{
			m_stream = new MemoryStream(1024 * 1024);
			m_obs = obs;
			m_resultObs = new object[m_obs.Length];

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

				int i = 0;
				while (m_stream.Position < m_stream.Length)
				{
					var ob = (Message)NetSerializer.Serializer.Deserialize(m_stream);
					if (ob == null)
						throw new Exception();
					m_resultObs[i++] = ob;
				}
			}
		}
	}

	class BinaryFormatterTest : Test
	{
		public int Size { get; private set; }
		Stream m_stream;
		object[] m_obs;
		object[] m_resultObs;
		public object[] Result { get { return m_resultObs; } }

		public BinaryFormatterTest(object[] obs)
		{
			m_stream = new MemoryStream(1024 * 1024);
			m_obs = obs;
			m_resultObs = new object[m_obs.Length];
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

				int i = 0;
				while (m_stream.Position < m_stream.Length)
				{
					var ob = (Message)bf.Deserialize(m_stream);
					if (ob == null)
						throw new Exception();
					m_resultObs[i++] = ob;
				}
			}
		}
	}

	class ProtoBufTest : Test
	{
		public int Size { get; private set; }
		Stream m_stream;
		object[] m_obs;
		object[] m_resultObs;
		public object[] Result { get { return m_resultObs; } }

		public ProtoBufTest(object[] obs)
		{
			m_stream = new MemoryStream(1024 * 1024);
			m_obs = obs;
			m_resultObs = new object[m_obs.Length];
		}

		protected override void RunOverride(int loops)
		{
			this.Size = 0;

			for (int l = 0; l < loops; ++l)
			{
				m_stream.Seek(0, SeekOrigin.Begin);

				foreach (var o in m_obs)
					ProtoBuf.Serializer.SerializeWithLengthPrefix(m_stream, (ComplexTypesMessage)o, ProtoBuf.PrefixStyle.Base128);

				if (l == 0)
					this.Size = (int)m_stream.Position;

				m_stream.Seek(0, SeekOrigin.Begin);

				int i = 0;
				while (m_stream.Position < m_stream.Length)
				{
					var ob = ProtoBuf.Serializer.DeserializeWithLengthPrefix<Message>(m_stream, ProtoBuf.PrefixStyle.Base128);
					if (ob == null)
						throw new Exception();
					m_resultObs[i++] = ob;
				}
			}
		}
	}
}

