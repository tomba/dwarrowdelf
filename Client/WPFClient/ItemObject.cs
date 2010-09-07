using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using MyGame.Messages;
using System.Windows;

namespace MyGame.Client
{
	class ItemObject : ClientGameObject, IItemObject
	{
		public static readonly DependencyProperty NutritionalValueProperty =
			RegisterGameProperty(PropertyID.NutritionalValue, "NutritionalValue", typeof(int), typeof(ItemObject), new UIPropertyMetadata(0));
		public static readonly DependencyProperty RefreshmentValueProperty =
			RegisterGameProperty(PropertyID.RefreshmentValue, "RefreshmentValue", typeof(int), typeof(ItemObject), new UIPropertyMetadata(0));

		public ItemObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			
		}

		public override string ToString()
		{
			return String.Format("Item({0})", this.ObjectID);
		}
	}
}
