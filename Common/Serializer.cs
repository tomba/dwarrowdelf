//#define USE_MY
//#define USE_MY_COMP
//#define USE_BINFMT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using MyGame.ClientMsgs;
using System.IO;
using System.Net.Sockets;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;

namespace MyGame
{
	public static class Serializer
	{
#if USE_BINFMT
		static BinaryFormatter m_bformatter = new BinaryFormatter();
#elif USE_MY
		static GameSerializer.Serializer m_serializer;

#else
		static DataContractSerializer m_serializer;
#endif

		static Serializer()
		{
#if USE_BINFMT
#elif USE_MY
			var messageTypes = typeof(Message).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Message)));
			var eventTypes = typeof(Event).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Event)));
			var actionTypes = typeof(GameAction).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(GameAction)));
			var types = messageTypes.Concat(eventTypes).Concat(actionTypes);
			m_serializer = new GameSerializer.Serializer(types.ToArray());
#else
			var messageTypes = typeof(Message).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Message)));
			var eventTypes = typeof(Event).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Event)));
			var actionTypes = typeof(GameAction).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(GameAction)));
			var types = messageTypes.Concat(eventTypes).Concat(actionTypes);
			m_serializer = new DataContractSerializer(typeof(Message), types);
#endif
		}

		public static void Serialize(Stream stream, Message msg)
		{
#if USE_BINFMT
			m_bformatter.Serialize(stream, msg);
#elif USE_MY
			m_serializer.Serialize(stream, msg);
#elif USE_MY_COMP
			using (var s = new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Compress, true))
				m_serializer.Serialize(s, msg);
#else
			using (var w = XmlDictionaryWriter.CreateBinaryWriter(stream, null, null, false))
			{
				m_serializer.WriteObject(w, msg);
			}
#endif
		}

		public static Message Deserialize(Stream stream)
		{
#if USE_BINFMT
			return (Message)m_bformatter.Deserialize(stream);
#elif USE_MY
			object ob = m_serializer.Deserialize(stream);
			return (Message)ob;
#elif USE_MY_COMP
			using (var s = new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Decompress, true))

			{
				object ob = m_serializer.Deserialize(s);
				return (Message)ob;
			}
#else
			using (var r = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
			{
				var msg = (Message)m_serializer.ReadObject(r);
				return msg;
			}
#endif
		}
	}
}
