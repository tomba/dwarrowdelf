using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	class ContentPool : Component
	{
		GraphicsDevice GraphicsDevice { get; set; }

		readonly string baseDir;

		Dictionary<string, GraphicsResource> m_pool = new Dictionary<string, GraphicsResource>();

		public ContentPool(GraphicsDevice device)
		{
			var exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
			baseDir = Path.Combine(exePath, "Content");

			this.GraphicsDevice = device;
		}

		string GetContentPath(string name)
		{
			return Path.Combine(baseDir, name + ".tkb");
		}

		string GetContentKey(Type type, string name)
		{
			return type.Name + "_" + name;
		}

		public T Load<T>(string name) where T : GraphicsResource
		{
			var key = GetContentKey(typeof(T), name);

			GraphicsResource res;

			if (m_pool.TryGetValue(key, out res))
				return (T)res;

			var path = GetContentPath(name);

			if (typeof(T) == typeof(Texture2D))
			{
				res = Texture2D.Load(this.GraphicsDevice, path);
			}
			else if (typeof(T) == typeof(Effect))
			{
				var effectData = EffectData.Load(path);

				res = (GraphicsResource)Activator.CreateInstance(typeof(T), this.GraphicsDevice, effectData, null);
			}
			else if (typeof(T).IsSubclassOf(typeof(Effect)))
			{
				var effectData = EffectData.Load(path);

				res = (GraphicsResource)Activator.CreateInstance(typeof(T), this.GraphicsDevice, effectData);
			}
			else
			{
				throw new NotImplementedException();
			}

			res = ToDispose(res);

			m_pool.Add(key, res);

			return (T)res;
		}
	}
}
