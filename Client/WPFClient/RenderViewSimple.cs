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
		/* How many levels to show */
		const int MAXLEVEL = 4;

		RendererSimple m_renderer;

		public RenderViewSimple()
		{
			m_renderer = new RendererSimple(this, m_renderData);
		}

		public override IRenderer Renderer { get { return m_renderer; } }

		protected override void MapChangedOverride(IntPoint3D ml)
		{
			// Note: invalidates the rendertile regardless of ml.Z
			// invalidate only if the change is within resolve limits (MAXLEVEL?)

			var x = ml.X - m_centerPos.X + m_renderData.Size.Width / 2;
			var y = ml.Y - m_centerPos.Y + m_renderData.Size.Height / 2;

			var p = new IntPoint(x, y);

			if (m_renderData.ArrayGrid.Bounds.Contains(p))
				m_renderData.ArrayGrid.Grid[p.Y, p.X].IsValid = false;
		}



		public override void Resolve()
		{
			//Debug.WriteLine("RenderViewSimple.Resolve");

			//var sw = Stopwatch.StartNew();

			var columns = m_renderData.Size.Width;
			var rows = m_renderData.Size.Height;
			var grid = m_renderData.ArrayGrid.Grid;

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

					if (m_renderData.ArrayGrid.Grid[y, x].IsValid)
						continue;

					var ml = new IntPoint3D(offsetX + x, offsetY + y, offsetZ);

					ResolveSimple(out m_renderData.ArrayGrid.Grid[y, x], this.Environment, ml, m_showVirtualSymbols, isSeeAll);
				}
			});

			//sw.Stop();
			//Trace.WriteLine(String.Format("Resolve {0} ms", sw.ElapsedMilliseconds));
		}

		static void ResolveSimple(out RenderTileSimple tile, Environment env, IntPoint3D ml, bool showVirtualSymbols, bool isSeeAll)
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

			for (int z = ml.Z; z > ml.Z - MAXLEVEL; --z)
			{
				var p = new IntPoint3D(ml.X, ml.Y, z);

				tile.Color = GetTileColor(p, env);

				if (!tile.Color.IsEmpty)
				{
					tile.DarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));
					break;
				}
			}
		}

		static GameColorRGB GetTileColor(IntPoint3D ml, Environment env)
		{
			var waterLevel = env.GetWaterLevel(ml);

			if (waterLevel > TileData.MaxWaterLevel / 4 * 3)
				return GameColor.DarkBlue.ToGameColorRGB();
			else if (waterLevel > TileData.MaxWaterLevel / 4 * 2)
				return GameColor.MediumBlue.ToGameColorRGB();
			else if (waterLevel > TileData.MaxWaterLevel / 4 * 1)
				return GameColor.Blue.ToGameColorRGB();
			else if (waterLevel > 1)
				return GameColor.DodgerBlue.ToGameColorRGB();


			var ob = env.GetFirstObject(ml);
			if (ob != null)
			{
				return ob.GameColor.ToGameColorRGB();
			}

			var interID = env.GetInterior(ml).ID;
			if (interID != InteriorID.Empty && interID != InteriorID.Undefined)
			{
				if (interID == InteriorID.Tree || interID == InteriorID.Sapling)
					return GameColor.ForestGreen.ToGameColorRGB();

				var mat = env.GetInteriorMaterial(ml);
				return mat.Color.ToGameColorRGB();
			}

			if (env.GetGrass(ml))
				return GameColor.Green.ToGameColorRGB();

			var floorID = env.GetFloor(ml).ID;
			if (floorID != FloorID.Empty && floorID != FloorID.Undefined)
			{
				var mat = env.GetFloorMaterial(ml);
				var rgb = mat.Color.ToGameColorRGB();
				// use a bit dimmer color for floor
				var r = rgb.R * 2 / 3;
				var g = rgb.G * 2 / 3;
				var b = rgb.B * 2 / 3;
				return new GameColorRGB((byte)r, (byte)g, (byte)b);
			}

			return GameColorRGB.Empty;
		}
	}
}
