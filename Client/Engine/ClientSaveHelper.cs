using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Dwarrowdelf.Client
{
	[Serializable]
	sealed class ClientSaveData
	{
		public BaseObject[] Objects;
	}

	public static class ClientSaveHelper
	{
		public static string SerializeClientObjects(World world)
		{
			var saveData = new ClientSaveData()
			{
				Objects = world.Objects.ToArray(),
			};

			Trace.TraceInformation("Saving client data");
			var watch = Stopwatch.StartNew();

			string data;

			using (var stream = new System.IO.MemoryStream())
			{
				using (var serializer = new Dwarrowdelf.SaveGameSerializer(stream, new[] { new ClientObjectRefResolver(world) }))
				{
					serializer.Serialize(saveData);

					stream.Position = 0;

					using (StreamReader reader = new StreamReader(stream))
						data = reader.ReadToEnd();
				}
			}

			watch.Stop();
			Trace.TraceInformation("Saving client data took {0}", watch.Elapsed);

			return data;
		}

		public static void Load(World world, string dataStr)
		{
			Trace.TraceInformation("Loading client data");
			var watch = Stopwatch.StartNew();

			ClientSaveData data;

			using (var reader = new StringReader(dataStr))
			{
				var deserializer = new Dwarrowdelf.SaveGameDeserializer(reader, new[] { new ClientObjectRefResolver(world) });
				data = deserializer.Deserialize<ClientSaveData>();
			}

			// Note that BaseObjects are deserialized by the SaveGameDeserializer, so we don't need to do anything for them here.

			watch.Stop();
			Trace.TraceInformation("Loading game took {0}", watch.Elapsed);
		}

		sealed class ClientObjectRefResolver : ISaveGameRefResolver
		{
			World m_world;

			public ClientObjectRefResolver(World world)
			{
				m_world = world;
			}

			public int ToRefID(object value)
			{
				var ob = (BaseObject)value;
				return (int)ob.ObjectID.RawValue;
			}

			public object FromRef(int refID)
			{
				var oid = new ObjectID((uint)refID);
				var ob = m_world.GetObject(oid);
				return ob;
			}

			public Type InputType { get { return typeof(IBaseObject); } }
		}
	}
}
