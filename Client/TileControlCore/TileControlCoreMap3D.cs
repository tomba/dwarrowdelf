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
			get { return ContentToMap(this.ScreenCenterPos); }
			set { this.ScreenCenterPos = MapToContent(value); }
		}

		public Rect MapCubeToScreenPointRect(IntGrid3 grid)
		{
			var p1 = MapLocationToContentTile(grid.Corner1);
			var p2 = MapLocationToContentTile(grid.Corner2);

			var r = new Rect(p1, p2);
			r.Inflate(0.5, 0.5);

			p1 = ContentTileToScreenPoint(r.TopLeft);
			p2 = ContentTileToScreenPoint(r.BottomRight);

			return new Rect(p1, p2);
		}

		public IntPoint3 ScreenPointToMapLocation(Point p)
		{
			var ct = ScreenPointToContentTile(p);
			return ContentTileToMapLocation(ct);
		}

		public IntPoint3 ScreenTileToMapLocation(Point p)
		{
			var ct = ScreenTileToContentTile(p);
			return ContentTileToMapLocation(ct);
		}

		public IntPoint3 ContentTileToMapLocation(Point p)
		{
			return ContentTileToMapLocation(p, this.ScreenZ);
		}

		public IntPoint3 ContentTileToMapLocation(Point p, double _z)
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

		public DoublePoint3 ContentTileToMapPoint(Point p)
		{
			return ContentTileToMapPoint(p, this.ScreenZ);
		}

		public DoublePoint3 ContentTileToMapPoint(Point p, double z)
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

		public Point MapLocationToContentTile(IntPoint3 p)
		{
			int z;
			return MapLocationToContentTile(p, out z);
		}

		public Point MapLocationToContentTile(IntPoint3 p, out int z)
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

		public IntPoint2 MapLocationToIntScreenTile(IntPoint3 p)
		{
			var ct = MapLocationToContentTile(p);
			var st = ContentTileToScreenTile(ct);
			return new IntPoint2(MyMath.Round(st.X), MyMath.Round(st.Y));
		}

		public DoublePoint3 MapToContent(IntPoint3 p)
		{
			return MapToContent(p.ToDoublePoint3());
		}

		public DoublePoint3 MapToContent(DoublePoint3 p)
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

		public DoublePoint3 ContentToMap(DoublePoint3 p)
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

		public DoubleVector3 ContentToMap(DoubleVector3 v)
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
	}
}
