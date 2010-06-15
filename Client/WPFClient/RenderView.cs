using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace MyGame.Client
{
	class RenderView
	{
		RenderMap m_renderMap;
		bool m_showVirtualSymbols = true;
		bool m_invalid;
		IntVector m_offset;
		int m_z;
		Environment m_environment;

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

		void MapChangedCallback(IntPoint3D ml)
		{
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

			if (env != null)
			{
				var visible = false;

				if (GameData.Data.IsSeeAll)
					visible = true;
				else
					visible = TileVisible(ml, env);

				bool floorLit;
				tile.FloorSymbolID = GetFloorBitmap(ml, out floorLit, env, showVirtualSymbols);
				tile.FloorDark = visible ? !floorLit : true;

				tile.InteriorSymbolID = GetInteriorBitmap(ml, env, showVirtualSymbols);
				tile.InteriorDark = !visible;

				if (GameData.Data.DisableLOS)
					visible = true; // lit always so we see what server sends

				tile.ObjectSymbolID = visible ? GetObjectBitmap(ml, env, out tile.ObjectColor) : SymbolID.Undefined;
				tile.ObjectDark = !visible;

				tile.TopSymbolID = GetTopBitmap(ml, env);
				tile.TopDark = !visible;
			}

			tile.IsValid = true;
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

		static SymbolID GetObjectBitmap(IntPoint3D ml, Environment env, out GameColor color)
		{
			IList<ClientGameObject> obs = env.GetContents(ml);
			if (obs != null && obs.Count > 0)
			{
				var id = obs[0].SymbolID;
				color = obs[0].GameColor;
				return id;
			}
			else
			{
				color = GameColor.None;
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
