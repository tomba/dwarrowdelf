using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Dwarrowdelf.Client.UI
{
	sealed partial class LivingControlWindow : Window
	{
		ManualControlAI m_ai;

		public LivingControlWindow()
		{
			InitializeComponent();

			this.DataContextChanged += LivingControlWindow_DataContextChanged;
			this.Closing += LivingControlWindow_Closing;
		}

		void LivingControlWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			var living = (LivingObject)this.DataContext;

			if (living != null)
			{
				m_ai = null;
				living.IsManuallyControlled = false;
			}
		}

		void LivingControlWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var living = (LivingObject)e.OldValue;

			if (living != null)
				living.IsManuallyControlled = false;

			living = (LivingObject)e.NewValue;

			if (living != null)
			{
				living.IsManuallyControlled = true;
				m_ai = (ManualControlAI)living.AI;
			}
		}

		void AddAction(GameAction action)
		{
			m_ai.AddAction(action);
		}

		private void MoveButton_Click(object sender, RoutedEventArgs e)
		{
			var b = (Button)sender;
			var d = (Direction)Enum.Parse(typeof(Direction), (string)b.Tag);

			var action = new MoveAction(d);

			AddAction(action);
		}

		private void DropButton_Click(object sender, RoutedEventArgs e)
		{
			if (inventoryListBox.SelectedItems.Count == 0)
				return;

			foreach (ItemObject item in inventoryListBox.SelectedItems)
			{
				var action = new DropItemAction(item);
				AddAction(action);
			}
		}

		private void GetButton_Click(object sender, RoutedEventArgs e)
		{
			var living = (LivingObject)this.DataContext;

			var obs = living.Environment.GetContents(living.Location).OfType<ItemObject>();

			var dlg = new ItemSelectorDialog();
			dlg.DataContext = obs;
			var b = dlg.ShowDialog();

			if (!b.HasValue || b.Value == false)
				return;

			var action = new GetItemAction((ItemObject)dlg.SelectedItem);
			AddAction(action);
		}

		private void WearButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (ItemObject item in inventoryListBox.SelectedItems)
			{
				var action = new EquipItemAction(item);
				AddAction(action);
			}
		}

		private void RemoveButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (ItemObject item in inventoryListBox.SelectedItems)
			{
				GameAction action;

				if (item.IsArmor || item.IsWeapon)
					action = new UnequipItemAction(item);
				else
					continue;

				AddAction(action);
			}
		}
	}
}
