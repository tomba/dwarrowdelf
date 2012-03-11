using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Dwarrowdelf.Client
{
	static class ClientSaveManager
	{
		public static Action SaveEvent;

		public static void Save(Guid id)
		{
			var saveData = GameData.Data.World.Objects.ToArray();

			Trace.TraceInformation("Saving client data");
			var watch = Stopwatch.StartNew();

			string data;

			using (var stream = new System.IO.MemoryStream())
			{
				using (var serializer = new Dwarrowdelf.SaveGameSerializer(stream, new[] { new ClientObjectRefResolver() }))
				{
					serializer.Serialize(saveData);

					stream.Position = 0;

					using (StreamReader reader = new StreamReader(stream))
						data = reader.ReadToEnd();
				}
			}

			watch.Stop();
			Trace.TraceInformation("Saving client data took {0}", watch.Elapsed);

			var msg = new Messages.SaveClientDataReplyMessage() { ID = id, Data = data };
			GameData.Data.User.Send(msg);

			if (SaveEvent != null)
				SaveEvent();
		}

		public static void Load(string dataStr)
		{
			Trace.TraceInformation("Loading client data");
			var watch = Stopwatch.StartNew();

			var reader = new StringReader(dataStr);

			var deserializer = new Dwarrowdelf.SaveGameDeserializer(reader, new[] { new ClientObjectRefResolver() });
			var data = deserializer.Deserialize<BaseObject[]>();

			watch.Stop();
			Trace.TraceInformation("Loading game took {0}", watch.Elapsed);
		}

		sealed class ClientObjectRefResolver : ISaveGameRefResolver
		{
			public int ToRefID(object value)
			{
				var ob = (BaseObject)value;
				return (int)ob.ObjectID.RawValue;
			}

			public object FromRef(int refID)
			{
				var oid = new ObjectID((uint)refID);
				var ob = GameData.Data.World.GetObject(oid);
				return ob;
			}

			public Type InputType { get { return typeof(IBaseObject); } }
		}
	}
}
