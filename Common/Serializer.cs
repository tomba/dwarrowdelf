using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using MyGame.ClientMsgs;
using System.IO;
using System.Net.Sockets;
using System.Xml;

namespace MyGame
{
	public static class Serializer
	{
		static DataContractSerializer m_serializer;

		static Serializer()
		{
			var types = typeof(Message).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Message)));
			m_serializer = new DataContractSerializer(typeof(Message), types);
		}

		public static void Serialize(Stream stream, Message msg)
		{
			using (var w = XmlDictionaryWriter.CreateBinaryWriter(stream, null, null, false))
			{
				m_serializer.WriteObject(w, msg);
			}
		}

		public static Message Deserialize(Stream stream)
		{
			using (var r = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
			{
				var msg = (Message)m_serializer.ReadObject(r);
				return msg;
			}
		}
	}
}
