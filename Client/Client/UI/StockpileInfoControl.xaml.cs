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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Client.UI
{
	sealed partial class StockpileInfoControl : UserControl
	{
		public StockpileInfoControl()
		{
			InitializeComponent();
		}
	}

	sealed class DesignStockpileSample
	{
		public StockpileCriteria Criteria { get; set; }

		public DesignStockpileSample()
		{
			var c = new StockpileCriteriaEditable();

			c.ItemIDs.Add(ItemID.Log);
			c.ItemIDs.Add(ItemID.Door);
			c.MaterialCategories.Add(MaterialCategory.Wood);

			this.Criteria = new StockpileCriteria(c);
		}
	}
}
