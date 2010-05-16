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
		public const int MinWaterLevel = 1;
		public const int MaxWaterLevel = 7;
		public const int MaxCompress = 1;

		public bool IsEmpty { get { return this.InteriorID == InteriorID.Empty && this.FloorID == FloorID.Empty; } }
	}

	public enum VisibilityMode
	{
		AllVisible,	// everything visible
		SimpleFOV,	// everything inside VisionRange is visible
		LOS,		// use LOS algorithm
	}
}
