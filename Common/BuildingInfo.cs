using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public enum BuildingID : byte
	{
		Undefined,
		Smith,
		Stockpile,
	}

	public class BuildingInfo
	{
		public BuildingID ID { get; set; }
		public string Name { get; set; }

		public BuildingInfo(BuildingID id)
		{
			this.ID = id;
			this.Name = id.ToString();
		}
	}

	public static class Buildings
	{
		static Dictionary<BuildingID, BuildingInfo> m_buildingMap = new Dictionary<BuildingID, BuildingInfo>();

		static Buildings()
		{
			Add(new BuildingInfo(BuildingID.Smith));
			Add(new BuildingInfo(BuildingID.Stockpile));
		}

		static void Add(BuildingInfo info)
		{
			m_buildingMap[info.ID] = info;
		}

		public static BuildingInfo GetBuildingInfo(BuildingID id)
		{
			return m_buildingMap[id];
		}
	}
}
