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
	public partial class StockpileInfoControl : UserControl
	{
		public StockpileInfoControl()
		{
			InitializeComponent();
		}
	}

	class DesignStockpileSample
	{
		public ObservableCollection<StockpileCriteria> Criterias { get; set; }

		public DesignStockpileSample()
		{
			var criterias = new ObservableCollection<StockpileCriteria>();

			var c1 = new StockpileCriteria();
			c1.ItemIDs.Add(ItemID.Log);
			c1.ItemIDs.Add(ItemID.Door);
			c1.MaterialCategories.Add(MaterialCategory.Wood);
			criterias.Add(c1);

			var c2 = new StockpileCriteria();
			c2.ItemIDs.Add(ItemID.Gem);
			criterias.Add(c2);

			this.Criterias = criterias;
		}
	}
}
