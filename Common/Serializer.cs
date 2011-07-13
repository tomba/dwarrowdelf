using System;
using System.IO;
using System.Linq;
using Dwarrowdelf.Messages;

namespace Dwarrowdelf
{
	public static class Serializer
	{
		static Serializer()
		{
			var messageTypes = Helpers.GetSubclasses(typeof(Message));
			var objectDataTypes = Helpers.GetSubclasses(typeof(BaseGameObjectData));
			var changeTypes = Helpers.GetSubclasses(typeof(Change));
			var actionTypes = Helpers.GetSubclasses(typeof(GameAction));
			var extra = new Type[] { typeof(SymbolID), typeof(GameColor) };
			var types = messageTypes.Concat(objectDataTypes).Concat(changeTypes).Concat(actionTypes).Concat(extra);
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
