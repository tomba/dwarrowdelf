using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	public struct MaterialID : IEquatable<MaterialID>
	{
		[DataMember]
		int m_id;

		public MaterialID(int id)
		{
			m_id = id;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is MaterialID))
				return false;

			MaterialID id = (MaterialID)obj;
			return m_id == id.m_id;
		}

		public static bool operator ==(MaterialID left, MaterialID right)
		{
			return left.m_id == right.m_id;
		}

		public static bool operator !=(MaterialID left, MaterialID right)
		{
			return !(left.m_id == right.m_id);
		}

		public override int GetHashCode()
		{
			return m_id;
		}


		#region IEquatable<MaterialID> Members

		public bool Equals(MaterialID other)
		{
			return m_id == other.m_id;
		}

		#endregion
	}

	public class MaterialInfo
	{
		MaterialID m_id;
		string m_name;

		public MaterialInfo(MaterialID id, string name)
		{
			m_id = id;
			m_name = name;
		}

		public MaterialID ID { get { return m_id; } }
		public string Name { get { return m_name; } }
	}

	public class Materials
	{
		Dictionary<MaterialID, MaterialInfo> m_materialMap = new Dictionary<MaterialID, MaterialInfo>();

		public Materials()
		{
			Add(new MaterialInfo(new MaterialID(0), "Undefined"));

			int id = 1;
			Add(new MaterialInfo(new MaterialID(id++), "Stone"));
			Add(new MaterialInfo(new MaterialID(id++), "Iron"));
			Add(new MaterialInfo(new MaterialID(id++), "Steel"));
			Add(new MaterialInfo(new MaterialID(id++), "Diamond"));
			Add(new MaterialInfo(new MaterialID(id++), "Wood"));
		}

		void Add(MaterialInfo info)
		{
			m_materialMap[info.ID] = info;
			if (info.ID == info.ID)
				return;
		}

		public MaterialInfo GetMaterialInfo(string name)
		{
			return m_materialMap.Values.Single(m => m.Name == name);
		}

		public MaterialInfo GetMaterialInfo(MaterialID id)
		{
			return m_materialMap[id];
		}
	}
}
