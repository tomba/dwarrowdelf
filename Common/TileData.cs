using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[Serializable]
	public struct TileData
	{
		public InteriorID InteriorID { get; set; }
		public MaterialID InteriorMaterialID { get; set; }

		public FloorID FloorID { get; set; }
		public MaterialID FloorMaterialID { get; set; }

		public byte WaterLevel { get; set; }
		public const int MinWaterLevel = 1;
		public const int MaxWaterLevel = 7;
		public const int MaxCompress = 1;

		public bool Grass { get; set; }

		public bool HasGrass { get { return this.Grass; } }
		public bool IsEmpty { get { return this.InteriorID == InteriorID.Empty && this.FloorID == FloorID.Empty; } }
	}

	public enum VisibilityMode
	{
		AllVisible,	// everything visible
		SimpleFOV,	// everything inside VisionRange is visible
		LOS,		// use LOS algorithm
	}
}
