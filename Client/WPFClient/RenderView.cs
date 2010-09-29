using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace Dwarrowdelf.Client
{
	interface IRenderView
	{
		Environment Environment { get; set; }

		int Z { get; set; }
		IntSize Size { get; set; }
		IntVector Offset { get; set; }
	
		void Invalidate();
		void ResolveAll();

		bool ShowVirtualSymbols { get; set; }
	}

	class RenderView : IRenderView
	{
		RenderMap m_renderMap;
		bool m_showVirtualSymbols = true;
		bool m_invalid;
		IntVector m_offset;
		int m_z;
		Environment m_environment;

		/* How many levels to show */
		const int MAXLEVEL = 4;

		public RenderView()
		{
			m_renderMap = new RenderMap();
		}

		public RenderMap RenderMap { get { return m_renderMap; } }

		public RenderTile GetRenderTile(IntPoint ml)
		{
			if (m_invalid)
			{
				m_renderMap.Clear();
				m_invalid = false;
			}

			//MyDebug.WriteLine("GET {0}", ml - this.Offset);

			IntPoint p = ml - this.Offset;
			var tile = m_renderMap.ArrayGrid.Grid[p.Y, p.X];

			if (!tile.IsValid)
			{
				var ml3d = new IntPoint3D(ml.X, ml.Y, this.Z);

				tile = Resolve(this.Environment, ml3d, m_showVirtualSymbols);

				m_renderMap.ArrayGrid.Grid[p.Y, p.X] = tile;
			}

			return tile;
		}

		public void ResolveAll()
		{
			//MyDebug.WriteLine("RenderView.ResolveAll");

			var columns = m_renderMap.Size.Width;
			var rows = m_renderMap.Size.Height;
			var grid = m_renderMap.ArrayGrid.Grid;

			if (m_invalid)
			{
				m_renderMap.Clear();
				m_invalid = false;
			}

			for (int y = 0; y < rows; ++y)
			{
				for (int x = 0; x < columns; ++x)
				{
					var p = new IntPoint(x, y);

					if (m_renderMap.ArrayGrid.Grid[p.Y, p.X].IsValid)
						continue;

					var ml = p + this.Offset;
					var ml3d = new IntPoint3D(ml.X, ml.Y, this.Z);

					Resolve(out m_renderMap.ArrayGrid.Grid[p.Y, p.X], this.Environment, ml3d, m_showVirtualSymbols);
				}
			}
		}


		void ScrollTiles(IntVector scrollVector)
		{
			//MyDebug.WriteLine("RenderView.ScrollTiles");

			var columns = m_renderMap.Size.Width;
			var rows = m_renderMap.Size.Height;
			var grid = m_renderMap.ArrayGrid.Grid;

			var ax = Math.Abs(scrollVector.X);
			var ay = Math.Abs(scrollVector.Y);

			if (ax >= columns || ay >= rows)
			{
				m_invalid = true;
				return;
			}

			int srcIdx = 0;
			int dstIdx = 0;

			if (scrollVector.X >= 0)
				srcIdx += ax;
			else
				dstIdx += ax;

			if (scrollVector.Y >= 0)
				srcIdx += columns * ay;
			else
				dstIdx += columns * ay;

			var xClrIdx = scrollVector.X >= 0 ? columns - ax : 0;
			var yClrIdx = scrollVector.Y >= 0 ? rows - ay : 0;

			Array.Copy(grid, srcIdx, grid, dstIdx, columns * rows - ax - columns * ay);

			for (int y = 0; y < rows; ++y)
				Array.Clear(grid, y * columns + xClrIdx, ax);

			Array.Clear(grid, yClrIdx * columns, columns * ay);
		}

		public IntVector Offset
		{
			get { return m_offset; }
			set
			{
				if (value == m_offset)
					return;

				var diff = value - m_offset;

				m_offset = value;

				if (!m_invalid)
					ScrollTiles(diff);
			}
		}

		public int Z
		{
			get { return m_z; }
			set
			{
				m_z = value;
				m_invalid = true;
			}
		}

		public IntSize Size
		{
			get { return m_renderMap.Size; }
			set
			{
				if (value == m_renderMap.Size)
					return;

				m_renderMap.Size = value;
				m_invalid = true;
			}
		}

		public bool ShowVirtualSymbols
		{
			get { return m_showVirtualSymbols; }

			set
			{
				m_showVirtualSymbols = value;
				m_invalid = true;
			}
		}

		public Environment Environment
		{
			get { return m_environment; }

			set
			{
				if (m_environment == value)
					return;

				if (m_environment != null)
				{
					m_environment.MapTileChanged -= MapChangedCallback;
				}

				m_environment = value;
				m_invalid = true;

				if (m_environment != null)
				{
					m_environment.MapTileChanged += MapChangedCallback;
				}
			}
		}

		public void Invalidate()
		{
			m_invalid = true;
		}

		void MapChangedCallback(IntPoint3D ml)
		{
			// Note: invalidates the rendertile regardless of ml.Z
			IntPoint p = ml.ToIntPoint() - this.Offset;
			if (m_renderMap.ArrayGrid.Bounds.Contains(p))
				m_renderMap.ArrayGrid.Grid[p.Y, p.X].IsValid = false;
		}



		static RenderTile Resolve(Environment env, IntPoint3D ml, bool showVirtualSymbols)
		{
			RenderTile tile;
			Resolve(out tile, env, ml, showVirtualSymbols);
			return tile;
		}

		static void Resolve(out RenderTile tile, Environment env, IntPoint3D ml, bool showVirtualSymbols)
		{
			tile = new RenderTile();
			tile.IsValid = true;

			if (env == null)
				return;

			bool visible;

			if (GameData.Data.IsSeeAll)
				visible = true;
			else
				visible = TileVisible(ml, env);

			for (int z = ml.Z; z > ml.Z - MAXLEVEL; --z)
			{
				var p = new IntPoint3D(ml.X, ml.Y, z);

				if (tile.Color == GameColor.None)
				{
					tile.Color = GetTileColor(p, env);

					if (tile.Color != GameColor.None)
						tile.DarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));
				}

				if (tile.Top.SymbolID == SymbolID.Undefined)
				{
					GetTopTile(p, env, ref tile.Top, showVirtualSymbols);

					if (tile.Top.SymbolID != SymbolID.Undefined)
						tile.Top.DarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));
				}

				if (tile.Object.SymbolID == SymbolID.Undefined)
				{
					GetObjectTile(p, env, ref tile.Object, showVirtualSymbols);

					if (tile.Object.SymbolID != SymbolID.Undefined)
						tile.Object.DarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));
				}

				if (tile.Interior.SymbolID == SymbolID.Undefined)
				{
					GetInteriorTile(p, env, ref tile.Interior, showVirtualSymbols);

					if (tile.Interior.SymbolID != SymbolID.Undefined)
						tile.Interior.DarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));
				}

				GetFloorTile(p, env, ref tile.Floor, showVirtualSymbols);

				if (tile.Floor.SymbolID != SymbolID.Undefined)
				{
					tile.Floor.DarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));
					break;
				}
			}

			if (tile.Object.DarknessLevel == 0)
				tile.Object.DarknessLevel = tile.Top.DarknessLevel;

			if (tile.Interior.DarknessLevel == 0)
				tile.Interior.DarknessLevel = tile.Object.DarknessLevel;

			if (tile.Floor.DarknessLevel == 0)
				tile.Floor.DarknessLevel = tile.Interior.DarknessLevel;
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

		static byte GetDarknessForLevel(int level)
		{
			if (level == 0)
				return 0;
			else
				return (byte)((level + 2) * 255 / (MAXLEVEL + 2));
		}

		static void GetFloorTile(IntPoint3D ml, Environment env, ref RenderTileLayer tile, bool showVirtualSymbols)
		{
			var flrID = env.GetFloorID(ml);

			if (flrID == FloorID.Undefined)
				return;

			if (flrID == FloorID.Empty)
			{
				if (showVirtualSymbols)
				{
					var flrId2 = env.GetFloor(ml + Direction.Down).ID;

					if (flrId2.IsSlope())
					{
						tile.Color = env.GetFloorMaterial(ml + Direction.Down).Color;

						switch (flrId2)
						{
							case FloorID.SlopeNorth:
								tile.SymbolID = SymbolID.SlopeDownSouth;
								break;

							case FloorID.SlopeSouth:
								tile.SymbolID = SymbolID.SlopeDownNorth;
								break;

							case FloorID.SlopeEast:
								tile.SymbolID = SymbolID.SlopeDownWest;
								break;

							case FloorID.SlopeWest:
								tile.SymbolID = SymbolID.SlopeDownEast;
								break;
						}

						if (env.GetGrass(ml + Direction.Down))
						{
							// override the material color
							tile.Color = GameColor.Green;
							tile.BgColor = GameColor.DarkGreen;
						}
					}
				}

				return;
			}

			var matInfo = env.GetFloorMaterial(ml);
			tile.Color = matInfo.Color;
			tile.BgColor = GameColor.None;

			switch (flrID)
			{
				case FloorID.Floor:
					if (env.GetGrass(ml))
					{
						tile.SymbolID = SymbolID.Grass;
						// override the material color
						tile.Color = GameColor.DarkGreen;
						tile.BgColor = GameColor.Green;
					}
					else
					{
						tile.SymbolID = SymbolID.Floor;
					}
					break;

				case FloorID.Hole:
					tile.SymbolID = SymbolID.Floor;
					break;


				case FloorID.SlopeNorth:
				case FloorID.SlopeSouth:
				case FloorID.SlopeEast:
				case FloorID.SlopeWest:
					switch (flrID.ToDir())
					{
						case Direction.North:
							tile.SymbolID = SymbolID.SlopeUpNorth;
							break;
						case Direction.South:
							tile.SymbolID = SymbolID.SlopeUpSouth;
							break;
						case Direction.East:
							tile.SymbolID = SymbolID.SlopeUpEast;
							break;
						case Direction.West:
							tile.SymbolID = SymbolID.SlopeUpWest;
							break;
						default:
							throw new Exception();
					}

					if (env.GetGrass(ml))
					{
						// override the material color
						tile.Color = GameColor.DarkGreen;
						tile.BgColor = GameColor.Green;
					}

					break;

				default:
					throw new Exception();
			}
		}

		static void GetInteriorTile(IntPoint3D ml, Environment env, ref RenderTileLayer tile, bool showVirtualSymbols)
		{
			var intID = env.GetInteriorID(ml);
			var intID2 = env.GetInteriorID(ml + Direction.Down);

			if (intID == InteriorID.Undefined)
				return;

			var matInfo = env.GetInteriorMaterial(ml);
			tile.Color = matInfo.Color;
			tile.BgColor = GameColor.None;

			switch (intID)
			{
				case InteriorID.Stairs:
					tile.SymbolID = SymbolID.StairsUp;
					break;

				case InteriorID.Empty:
					tile.SymbolID = SymbolID.Undefined;
					break;

				case InteriorID.Wall:
					tile.SymbolID = SymbolID.Wall;
					break;

				case InteriorID.Ore:
					tile.SymbolID = SymbolID.Ore;
					// use floor material as background color
					tile.BgColor = env.GetFloorMaterial(ml).Color;
					break;

				case InteriorID.Portal:
					tile.SymbolID = SymbolID.Portal;
					break;

				case InteriorID.Sapling:
					tile.SymbolID = SymbolID.Sapling;
					tile.Color = GameColor.ForestGreen;
					break;

				case InteriorID.Tree:
					tile.SymbolID = SymbolID.Tree;
					tile.Color = GameColor.ForestGreen;
					break;

				default:
					throw new Exception();
			}

			if (showVirtualSymbols)
			{
				if (intID == InteriorID.Stairs && intID2 == InteriorID.Stairs)
				{
					tile.SymbolID = SymbolID.StairsUpDown;
				}
			}
		}

		static void GetObjectTile(IntPoint3D ml, Environment env, ref RenderTileLayer tile, bool showVirtualSymbols)
		{
			var ob = env.GetFirstObject(ml);

			if (ob == null)
				return;

			tile.SymbolID = ob.SymbolID;
			tile.Color = ob.GameColor;
			tile.BgColor = GameColor.None;
		}

		static void GetTopTile(IntPoint3D ml, Environment env, ref RenderTileLayer tile, bool showVirtualSymbols)
		{
			int wl = env.GetWaterLevel(ml);

			if (wl == 0)
				return;

			SymbolID id;

			wl = wl * 100 / TileData.MaxWaterLevel;

			id = SymbolID.Water;

			if (wl > 80)
			{
				tile.Color = GameColor.Aqua;
			}
			else if (wl > 60)
			{
				tile.Color = GameColor.DodgerBlue;
			}
			else if (wl > 40)
			{
				tile.Color = GameColor.Blue;
			}
			else if (wl > 20)
			{
				tile.Color = GameColor.Blue;
			}
			else
			{
				tile.Color = GameColor.MediumBlue;
			}

			tile.BgColor = GameColor.DarkBlue;

			tile.SymbolID = id;
		}

		static GameColor GetTileColor(IntPoint3D ml, Environment env)
		{
			var waterLevel = env.GetWaterLevel(ml);

			if (waterLevel > TileData.MaxWaterLevel / 4 * 3)
				return GameColor.DarkBlue;
			else if (waterLevel > TileData.MaxWaterLevel / 4 * 2)
				return GameColor.MediumBlue;
			else if (waterLevel > TileData.MaxWaterLevel / 4 * 1)
				return GameColor.Blue;
			else if (waterLevel > 1)
				return GameColor.DodgerBlue;

			if (env.GetGrass(ml))
				return GameColor.Green;

			var interID = env.GetInterior(ml).ID;
			if (interID != InteriorID.Empty && interID != InteriorID.Undefined)
			{
				var mat = env.GetInteriorMaterial(ml);
				return mat.Color;
			}

			var floorID = env.GetFloor(ml).ID;
			if (floorID != FloorID.Empty && floorID != FloorID.Undefined)
			{
				var mat = env.GetFloorMaterial(ml);
				return mat.Color;
			}

			return GameColor.None;
		}
	}
}
