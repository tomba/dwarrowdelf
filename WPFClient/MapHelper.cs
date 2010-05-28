using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace MyGame.Client
{
	class MapHelper
	{
		public SymbolID FloorSymbolID;
		public bool FloorDark;

		public SymbolID InteriorSymbolID;
		public bool InteriorDark;

		public SymbolID ObjectSymbolID;
		public Color ObjectColor;
		public bool ObjectDark;

		public SymbolID TopSymbolID;
		public bool TopDark;

		public void Resolve(Environment env, IntPoint3D ml, bool showVirtualSymbols)
		{
			var visible = false;

			if (env == null)
			{
				this.FloorSymbolID = SymbolID.Undefined;
				this.InteriorSymbolID = SymbolID.Undefined;
				this.ObjectSymbolID = SymbolID.Undefined;
				this.TopSymbolID = SymbolID.Undefined;
			}
			else
			{
				if (GameData.Data.IsSeeAll)
					visible = true;
				else
					visible = TileVisible(ml, env);

				bool floorLit;
				this.FloorSymbolID = GetFloorBitmap(ml, out floorLit, env, showVirtualSymbols);
				this.FloorDark = visible ? !floorLit : true;

				this.InteriorSymbolID = GetInteriorBitmap(ml, env, showVirtualSymbols);
				this.InteriorDark = !visible;

				if (GameData.Data.DisableLOS)
					visible = true; // lit always so we see what server sends

				this.ObjectSymbolID = visible ? GetObjectBitmap(ml, env, out this.ObjectColor) : SymbolID.Undefined;
				this.ObjectDark = !visible;

				this.TopSymbolID = GetTopBitmap(ml, env);
				this.TopDark = !visible;
			}
		}

		static bool TileVisible(IntPoint3D ml, Environment env)
		{
			if (env.VisibilityMode == VisibilityMode.AllVisible)
				return true;

			if (env.GetInterior(ml).ID == InteriorID.Undefined)
				return false;

			var controllables = env.World.Controllables;

			if (env.VisibilityMode == VisibilityMode.LOS)
			{
				foreach (var l in controllables)
				{
					if (l.Environment != env || l.Location.Z != ml.Z)
						continue;

					IntPoint vp = new IntPoint(ml.X - l.Location.X, ml.Y - l.Location.Y);

					if (Math.Abs(vp.X) <= l.VisionRange && Math.Abs(vp.Y) <= l.VisionRange &&
						l.VisionMap[vp] == true)
						return true;
				}
			}
			else if (env.VisibilityMode == VisibilityMode.SimpleFOV)
			{
				foreach (var l in controllables)
				{
					if (l.Environment != env || l.Location.Z != ml.Z)
						continue;

					IntPoint vp = new IntPoint(ml.X - l.Location.X, ml.Y - l.Location.Y);

					if (Math.Abs(vp.X) <= l.VisionRange && Math.Abs(vp.Y) <= l.VisionRange)
						return true;
				}
			}
			else
			{
				throw new Exception();
			}

			return false;
		}

		static SymbolID GetFloorBitmap(IntPoint3D ml, out bool lit, Environment env, bool showVirtualSymbols)
		{
			var flrInfo = env.GetFloor(ml);

			if (flrInfo == null || flrInfo == Floors.Undefined)
			{
				lit = false;
				return SymbolID.Undefined;
			}

			FloorID fid = flrInfo.ID;
			SymbolID id;

			switch (fid)
			{
				case FloorID.NaturalFloor:
				case FloorID.Floor:
				case FloorID.Hole:
					id = SymbolID.Floor;
					break;

				case FloorID.Empty:
					id = SymbolID.Undefined;
					break;

				default:
					throw new Exception();
			}

			lit = true;

			if (showVirtualSymbols)
			{
				if (fid == FloorID.Empty)
				{
					id = SymbolID.Floor;
					lit = false;
				}
			}

			return id;
		}

		static SymbolID GetInteriorBitmap(IntPoint3D ml, Environment env, bool showVirtualSymbols)
		{
			SymbolID id;

			var intInfo = env.GetInterior(ml);
			var intInfo2 = env.GetInterior(ml + Direction.Down);

			var intID = intInfo.ID;
			var intID2 = intInfo2.ID;

			if (intInfo == null || intInfo == Interiors.Undefined)
				return SymbolID.Undefined;

			switch (intID)
			{
				case InteriorID.Stairs:
					id = SymbolID.StairsUp;
					break;

				case InteriorID.Empty:
					id = SymbolID.Undefined;
					break;

				case InteriorID.NaturalWall:
				case InteriorID.Wall:
					id = SymbolID.Wall;
					break;

				case InteriorID.Grass:
					id = SymbolID.Grass;
					break;

				case InteriorID.Portal:
					id = SymbolID.Portal;
					break;

				case InteriorID.Sapling:
					id = SymbolID.Sapling;
					break;

				case InteriorID.Tree:
					id = SymbolID.Tree;
					break;

				case InteriorID.SlopeNorth:
				case InteriorID.SlopeSouth:
				case InteriorID.SlopeEast:
				case InteriorID.SlopeWest:
					{
						switch (Interiors.GetDirFromSlope(intID))
						{
							case Direction.North:
								id = SymbolID.SlopeUpNorth;
								break;
							case Direction.South:
								id = SymbolID.SlopeUpSouth;
								break;
							case Direction.East:
								id = SymbolID.SlopeUpEast;
								break;
							case Direction.West:
								id = SymbolID.SlopeUpWest;
								break;
							default:
								throw new Exception();
						}
					}
					break;

				default:
					throw new Exception();
			}

			if (showVirtualSymbols)
			{
				if (intID == InteriorID.Stairs && intID2 == InteriorID.Stairs)
				{
					id = SymbolID.StairsUpDown;
				}
				else if (intID == InteriorID.Empty && intID2.IsSlope())
				{
					switch (intID2)
					{
						case InteriorID.SlopeNorth:
							id = SymbolID.SlopeDownSouth;
							break;

						case InteriorID.SlopeSouth:
							id = SymbolID.SlopeDownNorth;
							break;

						case InteriorID.SlopeEast:
							id = SymbolID.SlopeDownWest;
							break;

						case InteriorID.SlopeWest:
							id = SymbolID.SlopeDownEast;
							break;
					}
				}
			}

			return id;
		}

		static SymbolID GetObjectBitmap(IntPoint3D ml, Environment env, out Color color)
		{
			IList<ClientGameObject> obs = env.GetContents(ml);
			if (obs != null && obs.Count > 0)
			{
				var id = obs[0].SymbolID;
				color = obs[0].Color;
				return id;
			}
			else
			{
				color = Colors.Black;
				return SymbolID.Undefined;
			}
		}

		static SymbolID GetTopBitmap(IntPoint3D ml, Environment env)
		{
			int wl = env.GetWaterLevel(ml);

			if (wl == 0)
				return SymbolID.Undefined;

			SymbolID id;

			wl = wl * 100 / TileData.MaxWaterLevel;

			if (wl > 80)
				id = SymbolID.Water100;
			else if (wl > 60)
				id = SymbolID.Water80;
			else if (wl > 40)
				id = SymbolID.Water60;
			else if (wl > 20)
				id = SymbolID.Water40;
			else
				id = SymbolID.Water20;

			return id;
		}
	}
}
