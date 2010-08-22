using System;
using System.IO;
using System.Linq;
using MyGame.ClientMsgs;

namespace MyGame
{
	public static class Serializer
	{
		static Serializer()
		{
			var messageTypes = typeof(Message).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Message)));
			var eventTypes = typeof(Event).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Event)));
			var actionTypes = typeof(GameAction).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(GameAction)));
			var extra = new Type[] { typeof(SymbolID), typeof(GameColor) };
			var types = messageTypes.Concat(eventTypes).Concat(actionTypes).Concat(extra);
			NetSerializer.Serializer.Initialize(types.ToArray());
		}

		public static void Serialize(Stream stream, Message msg)
		{
			NetSerializer.Serializer.Serialize(stream, msg);
		}

		public static Message Deserialize(Stream stream)
		{
			object ob = NetSerializer.Serializer.Deserialize(stream);
			return (Message)ob;
		}
	}
}
