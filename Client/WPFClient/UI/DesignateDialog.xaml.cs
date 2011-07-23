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

namespace Dwarrowdelf.Client
{
	partial class DesignateDialog : Window
	{
		public DesignationType DesignationType { get; private set; }

		public DesignateDialog()
		{
			InitializeComponent();
		}

		public void SetContext(Environment env, IntCuboid area)
		{
			DesignationType type = Client.DesignationType.None;

			foreach (var p in area.Range())
			{
				var t = env.GetTerrain(p);

				if (t.IsMinable)
				{
					type = Client.DesignationType.Mine;
					break;
				}

				var iid = env.GetInteriorID(p);

				if (iid == InteriorID.Tree)
				{
					type = Client.DesignationType.FellTree;
					break;
				}
			}

			if (type != Client.DesignationType.None)
			{
				var button = GetButton(type.ToString());
				button.Focus();
			}
		}

		Button GetButton(string tag)
		{
			return buttonContainer.Children.Cast<Button>().Single(b => (string)(b.Tag) == tag);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			DesignationType? type = null;

			switch (e.Key)
			{
				case Key.M:
					type = DesignationType.Mine;
					break;

				case Key.S:
					type = DesignationType.CreateStairs;
					break;

				case Key.F:
					type = DesignationType.FellTree;
					break;

				case Key.R:
					type = Client.DesignationType.None;
					break;
			}

			if (type.HasValue)
			{
				this.DesignationType = type.Value;
				this.DialogResult = true;
				Close();
				return;
			}

			base.OnKeyDown(e);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var tag = (string)button.Tag;

			var type = (DesignationType)Enum.Parse(typeof(DesignationType), tag);

			this.DesignationType = type;

			this.DialogResult = true;
			Close();
		}
	}
}
