using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	public enum MapSelectionMode
	{
		None,
		Point,
		Rectangle,
		Box,
	}

	public struct MapSelection
	{
		public MapSelection(IntVector3 start, IntVector3 end)
			: this()
		{
			this.SelectionStart = start;
			this.SelectionEnd = end;
			this.IsSelectionValid = true;
		}

		public MapSelection(IntGrid3 box)
			: this()
		{
			if (box.Columns == 0 || box.Rows == 0 || box.Depth == 0)
			{
				this.IsSelectionValid = false;
			}
			else
			{
				this.SelectionStart = box.Corner1;
				this.SelectionEnd = box.Corner2;
				this.IsSelectionValid = true;
			}
		}

		public bool IsSelectionValid { get; set; }
		public IntVector3 SelectionStart { get; set; }
		public IntVector3 SelectionEnd { get; set; }

		public IntVector3 SelectionPoint
		{
			get
			{
				return this.SelectionStart;
			}
		}

		public IntGrid3 SelectionBox
		{
			get
			{
				if (!this.IsSelectionValid)
					return new IntGrid3();

				return new IntGrid3(this.SelectionStart, this.SelectionEnd);
			}
		}

		public IntGrid2Z SelectionIntRectZ
		{
			get
			{
				if (!this.IsSelectionValid)
					return new IntGrid2Z();

				if (this.SelectionStart.Z != this.SelectionEnd.Z)
					throw new Exception();

				return new IntGrid2Z(this.SelectionStart.ToIntVector2(), this.SelectionEnd.ToIntVector2(), this.SelectionStart.Z);
			}
		}
	}
}
