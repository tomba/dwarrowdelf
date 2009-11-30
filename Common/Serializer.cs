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
	public class Serializer
	{
		DataContractSerializer m_serializer;
		
		public Serializer()
		{
			var types = typeof(Message).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Message)));
			m_serializer = new DataContractSerializer(typeof(Message), types);
		}

		public void Serialize(Stream stream, Message msg)
		{
			using (var w = XmlDictionaryWriter.CreateBinaryWriter(stream, null, null, false))
			{
				m_serializer.WriteObject(w, msg);
			}
		}

		public Message Deserialize(Stream stream)
		{
			using (var r = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
			{
				var msg = (Message)m_serializer.ReadObject(r);
				return msg;
			}
		}

		public int Send(NetworkStream netStream, Message msg)
		{
			var stream = new MemoryStream();
			stream.Seek(4, SeekOrigin.Begin);
			Serialize(stream, msg);
			int len = (int)stream.Position;

			stream.Seek(0, SeekOrigin.Begin);
			BinaryWriter sw = new BinaryWriter(stream);
			sw.Write(len);

			//MyDebug.WriteLine("Sending {0} bytes", len);
			var buffer = stream.ToArray();
			netStream.Write(buffer, 0, len);

			return len;
		}
	}
}
