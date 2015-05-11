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

		public IntVector3? MouseLocation { get; private set; }
		public Direction Face { get; private set; }

		public MousePositionService(MyGame game, SharpDXHost control, GameSurfaceView surfaceView)
		{
			m_game = game;
			m_control = control;
			m_surfaceView = surfaceView;
		}

		public void Update()
		{
			IntVector3 p;
			Direction d;

			var pos = Mouse.GetPosition(m_control);
			var mousePos = new IntVector2(MyMath.Round(pos.X), MyMath.Round(pos.Y));

			if (m_surfaceView.ViewPort.Bounds.Contains(mousePos.X, mousePos.Y) == false)
			{
				this.MouseLocation = null;
				this.Face = Direction.None;
				return;
			}

			bool hit = MousePickVoxel(m_surfaceView, mousePos, m_game.ViewGrid.ViewGrid, out p, out d);

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

		bool MousePickVoxel(GameSurfaceView view, IntVector2 mousePos, IntGrid3 cropGrid, out IntVector3 pos, out Direction face)
		{
			var camera = view.Camera;

			var ray = Ray.GetPickRay(mousePos.X, mousePos.Y, view.ViewPort, view.Camera.View * view.Camera.Projection);

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

					var td = m_game.Environment.GetTileData(p);

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
