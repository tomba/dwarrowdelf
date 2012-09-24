﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Dwarrowdelf.Client.TileControl;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	sealed class RenderViewSimple : RenderViewBase<RenderTileSimple>
	{
		public RenderViewSimple()
		{
		}

		protected override void MapChangedOverride(IntPoint3 ml)
		{
			// Note: invalidates the rendertile regardless of ml.Z
			// invalidate only if the change is within resolve limits (MAXLEVEL?)

			var x = ml.X - m_centerPos.X + m_renderData.Width / 2;
			var y = ml.Y - m_centerPos.Y + m_renderData.Height / 2;

			if (m_renderData.Contains(new IntPoint2(x, y)))
			{
				int idx = m_renderData.GetIdx(x, y);
				m_renderData.Grid[idx].IsValid = false;
			}
		}

		public override bool Invalidate(IntPoint3 ml)
		{
			if (Contains(ml))
			{
				var p = MapLocationToRenderDataLocation(ml);
				int idx = m_renderData.GetIdx(p);
				m_renderData.Grid[idx].IsValid = false;
				return true;
			}
			else
			{
				return false;
			}
		}

		public override void Resolve()
		{
			//Debug.WriteLine("RenderView.Resolve");

			//var sw = Stopwatch.StartNew();

			var columns = m_renderData.Width;
			var rows = m_renderData.Height;
			var grid = m_renderData.Grid;

			if (m_renderData.Invalid || (m_environment != null && (m_environment.VisibilityMode != VisibilityMode.AllVisible || m_environment.VisibilityMode != VisibilityMode.GlobalFOV)))
			{
				//Debug.WriteLine("RenderView.Resolve All");
				m_renderData.Clear();
				m_renderData.Invalid = false;
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
					var p = new IntPoint2(x, y);

					int idx = m_renderData.GetIdx(x, y);

					if (m_renderData.Grid[idx].IsValid)
						continue;

					var ml = new IntPoint3(offsetX + x, offsetY + (rows - y - 1), offsetZ);

					Resolve(out m_renderData.Grid[idx], this.Environment, ml, isSeeAll);
				}
			});

			//sw.Stop();
			//Trace.WriteLine(String.Format("Resolve {0} ms", sw.ElapsedMilliseconds));
		}

		static void Resolve(out RenderTileSimple tile, EnvironmentObject env, IntPoint3 ml, bool isSeeAll)
		{
			tile = new RenderTileSimple();
			tile.IsValid = true;

			if (env == null || !env.Contains(ml))
				return;

			bool visible;

			if (isSeeAll)
				visible = true;
			else
				visible = TileVisible(ml, env);

			int z = ml.Z;

			var p = new IntPoint3(ml.X, ml.Y, z);

			tile.SymbolID = GetTerrainTile(p, env);
		}

		static SymbolID GetTerrainTile(IntPoint3 ml, EnvironmentObject env)
		{
			var flrID = env.GetTerrainID(ml);

			if (flrID == TerrainID.Undefined)
				return SymbolID.Undefined;

			if (flrID == TerrainID.Empty)
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

				return SymbolID.Undefined;
			}

			var matInfo = env.GetTerrainMaterial(ml);

			switch (flrID)
			{
				case TerrainID.NaturalFloor:
					return SymbolID.Floor;

				case TerrainID.NaturalWall:
					return SymbolID.Wall;

				case TerrainID.StairsDown:
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
