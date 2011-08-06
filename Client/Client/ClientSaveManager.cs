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
			Dictionary<ObjectID, object> objectData = new Dictionary<ObjectID, object>();

			foreach (var l in GameData.Data.World.Objects.OfType<BaseGameObject>())
			{
				var d = l.Save();
				if (d != null)
					objectData[l.ObjectID] = d;
			}

			var saveData = new SaveData()
			{
				ObjectData = objectData,
			};

			Trace.TraceInformation("Saving client data");
			var watch = Stopwatch.StartNew();

			string data;

			using (var stream = new System.IO.MemoryStream())
			{
				using (var serializer = new Dwarrowdelf.SaveGameSerializer(stream, new[] { new GameObjectConverter() }))
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
			GameData.Data.Connection.Send(msg);

			if (SaveEvent != null)
				SaveEvent();
		}

		public static void Load(string dataStr)
		{
			Trace.TraceInformation("Loading client data");
			var watch = Stopwatch.StartNew();

			var reader = new StringReader(dataStr);

			var deserializer = new Dwarrowdelf.SaveGameDeserializer(reader, new[] { new GameObjectConverter() });
			var data = deserializer.Deserialize<SaveData>();

			foreach (var kvp in data.ObjectData)
			{
				var ob = GameData.Data.World.FindObject<BaseGameObject>(kvp.Key);
				ob.Restore(kvp.Value);
			}

			watch.Stop();
			Trace.TraceInformation("Loading game took {0}", watch.Elapsed);
		}

		[Serializable]
		class SaveData
		{
			public Dictionary<ObjectID, object> ObjectData;
		}

		class GameObjectConverter : ISaveGameConverter
		{
			#region ISaveGameConverter Members

			public object ConvertToSerializable(object value)
			{
				var ob = (IBaseGameObject)value;
				return ob.ObjectID;
			}

			public object ConvertFromSerializable(object value)
			{
				var oid = (ObjectID)value;
				var ob = GameData.Data.World.FindObject(oid);
				if (ob == null)
					throw new Exception();
				return ob;
			}

			public Type InputType { get { return typeof(IBaseGameObject); } }

			public Type OutputType { get { return typeof(ObjectID); } }

			#endregion
		}
	}
}
