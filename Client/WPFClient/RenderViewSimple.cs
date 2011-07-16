using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Dwarrowdelf.Client.TileControl;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	class RenderViewSimple : RenderViewBase<RenderTileSimple>
	{
		public RenderViewSimple()
		{
		}

		protected override void MapChangedOverride(IntPoint3D ml)
		{
			// Note: invalidates the rendertile regardless of ml.Z
			// invalidate only if the change is within resolve limits (MAXLEVEL?)

			var x = ml.X - m_centerPos.X + m_renderData.Width / 2;
			var y = ml.Y - m_centerPos.Y + m_renderData.Height / 2;

			if (m_renderData.Contains(new IntPoint(x, y)))
				m_renderData.Grid[y, x].IsValid = false;
		}

		public override void Resolve()
		{
			//Debug.WriteLine("RenderView.Resolve");

			//var sw = Stopwatch.StartNew();

			var columns = m_renderData.Width;
			var rows = m_renderData.Height;
			var grid = m_renderData.Grid;

			if (m_invalid || (m_environment != null && (m_environment.VisibilityMode != VisibilityMode.AllVisible || m_environment.VisibilityMode != VisibilityMode.GlobalFOV)))
			{
				//Debug.WriteLine("RenderView.Resolve All");
				m_renderData.Clear();
				m_invalid = false;
			}

			bool isSeeAll = GameData.Data.User.IsSeeAll;

			int offsetX = m_centerPos.X - columns / 2;
			int offsetY = m_centerPos.Y - rows / 2;
			int offsetZ = m_centerPos.Z;

			// Note: we cannot access WPF stuff from different threads
			Parallel.For(0, rows, y =>
			{
				for (int x = 0; x < columns; ++x)
				{
					var p = new IntPoint(x, y);

					if (m_renderData.Grid[y, x].IsValid)
						continue;

					var ml = new IntPoint3D(offsetX + x, offsetY + (rows - y - 1), offsetZ);

					Resolve(out m_renderData.Grid[y, x], this.Environment, ml, m_showVirtualSymbols, isSeeAll);
				}
			});

			//sw.Stop();
			//Trace.WriteLine(String.Format("Resolve {0} ms", sw.ElapsedMilliseconds));
		}

		static void Resolve(out RenderTileSimple tile, Environment env, IntPoint3D ml, bool showVirtualSymbols, bool isSeeAll)
		{
			tile = new RenderTileSimple();
			tile.IsValid = true;

			if (env == null || !env.Bounds.Contains(ml))
				return;

			bool visible;

			if (isSeeAll)
				visible = true;
			else
				visible = TileVisible(ml, env);

			int z = ml.Z;

			var p = new IntPoint3D(ml.X, ml.Y, z);

			tile.SymbolID = GetTerrainTile(p, env, showVirtualSymbols);
		}

		static SymbolID GetTerrainTile(IntPoint3D ml, Environment env, bool showVirtualSymbols)
		{
			var flrID = env.GetTerrainID(ml);

			if (flrID == TerrainID.Undefined)
				return SymbolID.Undefined;

			if (flrID == TerrainID.Empty)
			{
				if (showVirtualSymbols)
				{
					var flrId2 = env.GetTerrain(ml + Direction.Down).ID;

					if (flrId2.IsSlope())
					{
						switch (flrId2.ToDir().Reverse())
						{
							case Direction.North:
								return SymbolID.SlopeDownNorth;
							case Direction.NorthEast:
								return SymbolID.SlopeDownNorthEast;
							case Direction.East:
								return SymbolID.SlopeDownEast;
							case Direction.SouthEast:
								return SymbolID.SlopeDownSouthEast;
							case Direction.South:
								return SymbolID.SlopeDownSouth;
							case Direction.SouthWest:
								return SymbolID.SlopeDownSouthWest;
							case Direction.West:
								return SymbolID.SlopeDownWest;
							case Direction.NorthWest:
								return SymbolID.SlopeDownNorthWest;
							default:
								throw new Exception();
						}
					}
				}

				return SymbolID.Undefined;
			}

			var matInfo = env.GetTerrainMaterial(ml);

			switch (flrID)
			{
				case TerrainID.NaturalFloor:
					if (env.GetGrass(ml))
						return SymbolID.Grass;
					else
						return SymbolID.Floor;

				case TerrainID.NaturalWall:
					return SymbolID.Wall;

				case TerrainID.Hole:
					return SymbolID.Floor;

				case TerrainID.SlopeNorth:
				case TerrainID.SlopeNorthEast:
				case TerrainID.SlopeEast:
				case TerrainID.SlopeSouthEast:
				case TerrainID.SlopeSouth:
				case TerrainID.SlopeSouthWest:
				case TerrainID.SlopeWest:
				case TerrainID.SlopeNorthWest:
					switch (flrID.ToDir())
					{
						case Direction.North:
							return SymbolID.SlopeUpNorth;
						case Direction.NorthEast:
							return SymbolID.SlopeUpNorthEast;
						case Direction.East:
							return SymbolID.SlopeUpEast;
						case Direction.SouthEast:
							return SymbolID.SlopeUpSouthEast;
						case Direction.South:
							return SymbolID.SlopeUpSouth;
						case Direction.SouthWest:
							return SymbolID.SlopeUpSouthWest;
						case Direction.West:
							return SymbolID.SlopeUpWest;
						case Direction.NorthWest:
							return SymbolID.SlopeUpNorthWest;
						default:
							throw new Exception();
					}

				default:
					throw new Exception();
			}
		}
	}
}
