using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dwarrowdelf.Client
{
	class MousePositionService : IGameUpdatable
	{
		MyGame m_game;
		GameSurfaceView m_surfaceView;
		SharpDXHost m_control;

		public Direction Face { get; private set; }

		public event Action MouseLocationChanged;

		public MousePositionService(MyGame game, SharpDXHost control, GameSurfaceView surfaceView)
		{
			m_game = game;
			m_control = control;
			m_surfaceView = surfaceView;
		}

		IntVector2? m_screenLocation;

		public IntVector2? ScreenLocation
		{
			get { return m_screenLocation; }

			private set
			{
				if (value == m_screenLocation)
					return;

				m_screenLocation = value;
			}
		}

		IntVector3? m_mapLocation;

		public IntVector3? MapLocation
		{
			get { return m_mapLocation; }

			private set
			{
				if (value == m_mapLocation)
					return;

				m_mapLocation = value;

				if (this.MouseLocationChanged != null)
					this.MouseLocationChanged();
			}
		}

		public void Update()
		{
			IntVector3 p;
			Direction d;

			var pos = Mouse.GetPosition(m_control);
			var mousePos = new IntVector2(MyMath.Round(pos.X), MyMath.Round(pos.Y));

			if (m_surfaceView.ViewPort.Width == 0 || m_surfaceView.ViewPort.Height == 0 ||
				m_surfaceView.ViewPort.Bounds.Contains(mousePos.X, mousePos.Y) == false)
			{
				this.ScreenLocation = null;
				this.MapLocation = null;
				this.Face = Direction.None;
				return;
			}

			this.ScreenLocation = mousePos;

			var pickMode = ((MapControl3D)m_control).Config.PickMode;

			bool hit = PickVoxel(m_game.Environment, m_surfaceView, mousePos, m_game.ViewGridProvider.ViewGrid, pickMode, out p, out d);

			if (hit)
			{
				this.MapLocation = p;
				this.Face = d;
			}
			else
			{
				this.MapLocation = null;
				this.Face = Direction.None;
			}
		}

		public static bool PickVoxel(MyGame game, IntVector2 screenPos, MapControlPickMode pickMode, out IntVector3 pos, out Direction face)
		{
			return MousePositionService.PickVoxel(game.Environment, game.Surfaces[0].Views[0], screenPos,
				game.ViewGridProvider.ViewGrid, pickMode, out pos, out face);
		}

		public static bool PickVoxel(EnvironmentObject env, GameSurfaceView view, IntVector2 screenPos, IntGrid3 cropGrid,
			MapControlPickMode pickMode, out IntVector3 pos, out Direction face)
		{
			var camera = view.Camera;

			var ray = Ray.GetPickRay(screenPos.X, screenPos.Y, view.ViewPort, view.Camera.View * view.Camera.Projection);

			IntVector3 outpos = new IntVector3();
			Direction outdir = Direction.None;

			var corner = cropGrid.Corner2;
			var size = new IntSize3(corner.X + 1, corner.Y + 1, corner.Z + 1);

			IntVector3 prevoutpos = new IntVector3();
			Direction prevoutdir = Direction.None;

			VoxelRayCast.RunRayCast(size, ray.Position, ray.Direction, view.Camera.FarZ,
				(x, y, z, dir) =>
				{
					var p = new IntVector3(x, y, z);

					if (cropGrid.Contains(p) == false)
						return false;

					var td = env.GetTileData(p);

					switch (pickMode)
					{
						case MapControlPickMode.Underground:
							if (!td.IsUndefined && !td.IsWall)
								return false;

							outpos = p;
							outdir = dir;
							return true;

						case MapControlPickMode.AboveGroud:
							if (!td.IsUndefined && !td.IsWall)
							{
								prevoutpos = p;
								prevoutdir = dir;
								return false;
							}

							outpos = prevoutpos;
							outdir = prevoutdir;
							return true;

						case MapControlPickMode.Constant:
							if (p.Z > cropGrid.Z2)
								return false;

							outpos = p;
							outdir = dir;
							return true;

						default:
							throw new NotImplementedException();
					}
				});

			pos = outpos;
			face = outdir;
			return face != Direction.None;
		}
	}
}
