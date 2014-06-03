using System;
using System.Windows;

namespace Dwarrowdelf.Client.TileControl
{
	public enum TileControlOrientation
	{
		XY,
		XZ,
		ZY,
	}

	public abstract class TileControlCoreMap3D : TileControlCore3D
	{
		public TileControlOrientation Orientation { get; set; }

		public DoublePoint3 MapCenterPos
		{
			get { return ScreenToMap(this.ScreenCenterPos); }
			set { this.ScreenCenterPos = MapToScreen(value); }
		}

		public Rect MapCubeToRenderPointRect(IntGrid3 grid)
		{
			var p1 = MapLocationToScreenTile(grid.Corner1);
			var p2 = MapLocationToScreenTile(grid.Corner2);

			var r = new Rect(p1, p2);
			r.Inflate(0.5, 0.5);

			p1 = ScreenToRenderPoint(r.TopLeft);
			p2 = ScreenToRenderPoint(r.BottomRight);

			return new Rect(p1, p2);
		}

		public IntPoint3 RenderPointToMapLocation(Point p)
		{
			var ct = RenderPointToScreen(p);
			return ScreenTileToMapLocation(ct);
		}

		public IntPoint3 RenderTileToMapLocation(Point p)
		{
			var ct = RenderTileToScreen(p);
			return ScreenTileToMapLocation(ct);
		}

		public IntPoint3 ScreenTileToMapLocation(Point p)
		{
			return ScreenTileToMapLocation(p, this.ScreenZ);
		}

		public IntPoint3 ScreenTileToMapLocation(Point p, double _z)
		{
			int x = MyMath.Round(p.X);
			int y = MyMath.Round(p.Y);
			int z = MyMath.Round(_z);

			switch (this.Orientation)
			{
				case TileControlOrientation.XY:
					return new IntPoint3(x, y, z);
				case TileControlOrientation.XZ:
					return new IntPoint3(x, z, -y);
				case TileControlOrientation.ZY:
					return new IntPoint3(z, y, x);
				default:
					throw new NotImplementedException();
			}
		}

		public DoublePoint3 ScreenTileToMapPoint(Point p)
		{
			return ScreenTileToMapPoint(p, this.ScreenZ);
		}

		public DoublePoint3 ScreenTileToMapPoint(Point p, double z)
		{
			switch (this.Orientation)
			{
				case TileControlOrientation.XY:
					return new DoublePoint3(p.X, p.Y, z);
				case TileControlOrientation.XZ:
					return new DoublePoint3(p.X, z, -p.Y);
				case TileControlOrientation.ZY:
					return new DoublePoint3(z, p.Y, p.X);
				default:
					throw new NotImplementedException();
			}
		}

		public Point MapLocationToScreenTile(IntPoint3 p)
		{
			int z;
			return MapLocationToScreenTile(p, out z);
		}

		public Point MapLocationToScreenTile(IntPoint3 p, out int z)
		{
			switch (this.Orientation)
			{
				case TileControlOrientation.XY:
					z = p.Z;
					return new Point(p.X, p.Y);
				case TileControlOrientation.XZ:
					z = p.Y;
					return new Point(p.X, -p.Z);
				case TileControlOrientation.ZY:
					z = p.X;
					return new Point(p.Z, p.Y);
				default:
					throw new NotImplementedException();
			}
		}

		public IntPoint2 MapLocationToIntRenderTile(IntPoint3 p)
		{
			var ct = MapLocationToScreenTile(p);
			var st = ScreenToRenderTile(ct);
			return new IntPoint2(MyMath.Round(st.X), MyMath.Round(st.Y));
		}

		public DoublePoint3 MapToScreen(IntPoint3 p)
		{
			return MapToScreen(p.ToDoublePoint3());
		}

		public DoublePoint3 MapToScreen(DoublePoint3 p)
		{
			switch (this.Orientation)
			{
				case TileControlOrientation.XY:
					return p;
				case TileControlOrientation.XZ:
					return new DoublePoint3(p.X, -p.Z, p.Y);
				case TileControlOrientation.ZY:
					return new DoublePoint3(p.Z, p.Y, p.X);
				default:
					throw new NotImplementedException();
			}
		}

		public DoublePoint3 ScreenToMap(DoublePoint3 p)
		{
			switch (this.Orientation)
			{
				case TileControlOrientation.XY:
					return p;
				case TileControlOrientation.XZ:
					return new DoublePoint3(p.X, p.Z, -p.Y);
				case TileControlOrientation.ZY:
					return new DoublePoint3(p.Z, p.Y, p.X);
				default:
					throw new NotImplementedException();
			}
		}

		public DoubleVector3 ScreenToMap(DoubleVector3 v)
		{
			switch (this.Orientation)
			{
				case TileControlOrientation.XY:
					return v;
				case TileControlOrientation.XZ:
					return new DoubleVector3(v.X, v.Z, -v.Y);
				case TileControlOrientation.ZY:
					return new DoubleVector3(v.Z, v.Y, v.X);
				default:
					throw new NotImplementedException();
			}
		}

		public IntVector3 ScreenToMap(IntVector3 v)
		{
			switch (this.Orientation)
			{
				case TileControlOrientation.XY:
					return v;
				case TileControlOrientation.XZ:
					return new IntVector3(v.X, v.Z, -v.Y);
				case TileControlOrientation.ZY:
					return new IntVector3(v.Z, v.Y, v.X);
				default:
					throw new NotImplementedException();
			}
		}
	}
}
