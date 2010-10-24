using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public enum BuildingID : byte
	{
		Undefined = 0,
		Carpenter,
		Mason,
		Smith,
	}

	public class BuildingInfo
	{
		public BuildingID ID { get; set; }
		public string Name { get; set; }
	}

	public static class Buildings
	{
		static BuildingInfo[] s_buildings;

		static Buildings()
		{
			BuildingInfo[] buildings;

			using (var stream = System.IO.File.OpenRead("Buildings.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = System.Reflection.Assembly.GetCallingAssembly(),
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					buildings = (BuildingInfo[])System.Xaml.XamlServices.Load(reader);
			}

			var max = buildings.Max(bi => (int)bi.ID);
			s_buildings = new BuildingInfo[max + 1];

			foreach (var building in buildings)
			{
				if (s_buildings[(int)building.ID] != null)
					throw new Exception();

				if (building.Name == null)
					building.Name = building.ID.ToString();

				s_buildings[(int)building.ID] = building;
			}

			s_buildings[0] = new BuildingInfo()
			{
				ID = BuildingID.Undefined,
				Name = "<undefined>",
			};
		}

		public static BuildingInfo GetBuildingInfo(BuildingID id)
		{
			Debug.Assert(id != BuildingID.Undefined);
			Debug.Assert(s_buildings[(int)id] != null);

			return s_buildings[(int)id];
		}
	}
}
