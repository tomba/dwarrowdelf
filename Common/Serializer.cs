using System;
using System.IO;
using System.Linq;
using Dwarrowdelf.Messages;

namespace Dwarrowdelf
{
	static class Serializer
	{
		static Serializer()
		{
			var messageTypes = Helpers.GetSubclasses(typeof(Message));
			var objectDataTypes = Helpers.GetSubclasses(typeof(BaseGameObjectData));
			var changeTypes = Helpers.GetSubclasses(typeof(ChangeData));
			var actionTypes = Helpers.GetSubclasses(typeof(GameAction));
			var events = Helpers.GetSubclasses(typeof(GameEvent));
			var extra = new Type[] { typeof(GameColor), typeof(LivingGender) };
			var reports = Helpers.GetSubclasses(typeof(GameReport));
			var types = messageTypes.Concat(objectDataTypes).Concat(changeTypes).Concat(actionTypes).Concat(events)
				.Concat(extra).Concat(reports);

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
