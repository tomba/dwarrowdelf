using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using MyGame.ClientMsgs;
using System.Diagnostics;

namespace MyGame
{
	class ItemObject : ClientGameObject
	{
		public ItemObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			
		}

		public void Deserialize(ItemData data)
		{
			Debug.Assert(data.ObjectID == this.ObjectID);

			this.Name = data.Name;
			this.SymbolID = data.SymbolID;
			this.Color = data.Color.ToColor();
			this.Material = this.World.AreaData.Materials.GetMaterialInfo(data.MaterialID);

			ClientGameObject env = null;
			if (data.Environment != ObjectID.NullObjectID)
				env = this.World.FindObject(data.Environment);

			this.MoveTo(env, data.Location);
		}

		public override string ToString()
		{
			return String.Format("Item({0})", this.ObjectID);
		}
	}
}
