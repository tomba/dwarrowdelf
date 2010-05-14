using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	[Serializable]
	public struct TileData
	{
		[DataMember]
		public InteriorID InteriorID { get; set; }
		[DataMember]
		public MaterialID InteriorMaterialID { get; set; }

		[DataMember]
		public FloorID FloorID { get; set; }
		[DataMember]
		public MaterialID FloorMaterialID { get; set; }

		[DataMember]
		public byte WaterLevel { get; set; }
		public const int MinWaterLevel = 8 - 1;
		public const int MaxWaterLevel = 64 - 1;
		public const int MaxCompress = 4;

		public bool IsEmpty { get { return this.InteriorID == InteriorID.Empty && this.FloorID == FloorID.Empty; } }
	}

	public enum VisibilityMode
	{
		AllVisible,	// everything visible
		SimpleFOV,	// everything inside VisionRange is visible
		LOS,		// use LOS algorithm
	}
}
