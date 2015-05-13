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

		IntVector3? m_location;

		public IntVector3? MouseLocation
		{
			get { return m_location; }

			private set
			{
				if (value == m_location)
					return;

				m_location = value;

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
				this.MouseLocation = null;
				this.Face = Direction.None;
				return;
			}

			bool hit = PickVoxel(m_game.Environment, m_surfaceView, mousePos, m_game.ViewGrid.ViewGrid, out p, out d);

			if (hit)
			{
				this.MouseLocation = p;
				this.Face = d;
			}
			else
			{
				this.MouseLocation = null;
				this.Face = Direction.None;
			}
		}

		public static bool PickVoxel(MyGame game, IntVector2 screenPos, out IntVector3 pos, out Direction face)
		{
			return MousePositionService.PickVoxel(game.Environment, game.Surfaces[0].Views[0], screenPos,
				game.ViewGrid.ViewGrid, out pos, out face);
		}

		public static bool PickVoxel(EnvironmentObject env, GameSurfaceView view, IntVector2 screenPos, IntGrid3 cropGrid,
			out IntVector3 pos, out Direction face)
		{
			var camera = view.Camera;

			var ray = Ray.GetPickRay(screenPos.X, screenPos.Y, view.ViewPort, view.Camera.View * view.Camera.Projection);

			IntVector3 outpos = new IntVector3();
			Direction outdir = Direction.None;

			var corner = cropGrid.Corner2;
			var size = new IntSize3(corner.X, corner.Y, corner.Z);

			VoxelRayCast.RunRayCast(size, ray.Position, ray.Direction, view.Camera.FarZ,
				(x, y, z, dir) =>
				{
					var p = new IntVector3(x, y, z);

					if (cropGrid.Contains(p) == false)
						return false;

					var td = env.GetTileData(p);

					if (td.IsEmpty)
						return false;

					outpos = p;
					outdir = dir;

					return true;
				});

			pos = outpos;
			face = outdir;
			return face != Direction.None;
		}
	}
}
