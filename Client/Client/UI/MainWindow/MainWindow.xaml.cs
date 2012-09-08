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
using System.Windows.Threading;
using System.ComponentModel;
using Dwarrowdelf.Messages;
using Dwarrowdelf.Jobs;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace Dwarrowdelf.Client.UI
{
	sealed partial class MainWindow : Window, INotifyPropertyChanged
	{
		MovableObject m_followObject;

		enum CloseStatus
		{
			None,
			ShuttingDown,
			Ready,
		}

		CloseStatus m_closeStatus;

		// Stores previous user values for setTerrainData
		SetTerrainData m_setTerrainData;

		MainWindowCommandHandler m_cmdHandler;

		public MainWindow()
		{
			InitializeComponent();

			this.MapControl.GotSelection += MapControl_GotSelection;
			this.mainWindowTools.ToolModeChanged += MainWindowTools_ToolModeChanged;

			this.mainWindowTools.ToolMode = ClientToolMode.Info;

			// for some reason this prevents the changing of focus from mapcontrol with cursor keys
			KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.Once);
		}

		void MainWindowTools_ToolModeChanged(ClientToolMode toolMode)
		{
			switch (toolMode)
			{
				case ClientToolMode.Info:
					this.MapControl.SelectionMode = MapSelectionMode.None;
					break;

				case ClientToolMode.CreateItem:
				case ClientToolMode.DesignationMine:
				case ClientToolMode.DesignationStairs:
				case ClientToolMode.DesignationChannel:
				case ClientToolMode.DesignationFellTree:
				case ClientToolMode.DesignationRemove:
				case ClientToolMode.SetTerrain:
				case ClientToolMode.CreateLiving:
					this.MapControl.SelectionMode = MapSelectionMode.Box;
					break;

				case ClientToolMode.CreateStockpile:
				case ClientToolMode.ConstructBuilding:
					this.MapControl.SelectionMode = MapSelectionMode.Rectangle;
					break;


				case ClientToolMode.ConstructWall:
				case ClientToolMode.ConstructFloor:
				case ClientToolMode.ConstructPavement:
				case ClientToolMode.ConstructRemove:
					this.MapControl.SelectionMode = MapSelectionMode.Rectangle;
					break;

				case ClientToolMode.InstallFurniture:
					this.MapControl.SelectionMode = MapSelectionMode.Point;
					break;

				default:
					throw new Exception();
			}
		}

		void MapControl_GotSelection(MapSelection selection)
		{
			var env = this.Map;

			switch (this.mainWindowTools.ToolMode)
			{
				case ClientToolMode.DesignationRemove:
					env.Designations.RemoveArea(selection.SelectionBox);
					break;

				case ClientToolMode.DesignationMine:
					env.Designations.AddArea(selection.SelectionBox, DesignationType.Mine);
					break;

				case ClientToolMode.DesignationStairs:
					env.Designations.AddArea(selection.SelectionBox, DesignationType.CreateStairs);
					break;

				case ClientToolMode.DesignationChannel:
					env.Designations.AddArea(selection.SelectionBox, DesignationType.Channel);
					break;

				case ClientToolMode.DesignationFellTree:
					env.Designations.AddArea(selection.SelectionBox, DesignationType.FellTree);
					break;

				case ClientToolMode.CreateStockpile:
					{
						var stockpile = new Stockpile(env, selection.SelectionIntRectZ);
						env.AddAreaElement(stockpile);

						var dlg = new ObjectEditDialog();
						dlg.DataContext = stockpile;
						dlg.Owner = this;
						dlg.Show();
					}
					break;

				case ClientToolMode.InstallFurniture:
					{
						var p = selection.SelectionPoint;

						var dlg = new InstallFurnitureDialog();
						dlg.SetContext(env, p);

						var res = dlg.ShowDialog();

						if (res == true)
						{
							var item = dlg.SelectedItem;

							if (item != null)
								env.InstallFurnitureManager.AddInstallJob(item, p);
						}
					}
					break;

				case ClientToolMode.SetTerrain:
					{
						var dialog = new SetTerrainDialog();
						dialog.Owner = this;
						if (m_setTerrainData != null)
							dialog.Data = m_setTerrainData;
						var res = dialog.ShowDialog();

						if (res == true)
						{
							var data = dialog.Data;
							m_setTerrainData = data;

							var args = new Dictionary<string, object>()
							{
								{ "envID", map.Environment.ObjectID },
								{ "cube", selection.SelectionBox },
								{ "terrainID", data.TerrainID },
								{ "terrainMaterialID", data.TerrainMaterialID },
								{ "interiorID", data.InteriorID },
								{ "interiorMaterialID", data.InteriorMaterialID },
								{ "waterLevel", data.Water.HasValue ? (data.Water == true ? (byte?)TileData.MaxWaterLevel : (byte?)0) : null },
							};

							var script =
@"env = world.GetObject(envID)
for p in cube.Range():
	td = env.GetTileData(p)

	if terrainID != None:
		Dwarrowdelf.TileData.TerrainID.SetValue(td, terrainID)
	if terrainMaterialID != None:
		Dwarrowdelf.TileData.TerrainMaterialID.SetValue(td, terrainMaterialID)

	if interiorID != None:
		Dwarrowdelf.TileData.InteriorID.SetValue(td, interiorID)
	if interiorMaterialID != None:
		Dwarrowdelf.TileData.InteriorMaterialID.SetValue(td, interiorMaterialID)

	if waterLevel != None:
		Dwarrowdelf.TileData.WaterLevel.SetValue(td, waterLevel)

	env.SetTileData(p, td)

env.ScanWaterTiles()
";
							var msg = new Dwarrowdelf.Messages.IPScriptMessage(script, args);

							GameData.Data.User.Send(msg);
						}
					}
					break;

				case ClientToolMode.CreateItem:
					{
						var dialog = new CreateItemDialog();
						dialog.Owner = this;
						dialog.SetContext(env, selection.SelectionBox);
						var res = dialog.ShowDialog();

						if (res == true)
						{
							var args = new Dictionary<string, object>()
							{
								{ "envID", dialog.Environment.ObjectID },
								{ "area", dialog.Area },
								{ "itemID", dialog.ItemID },
								{ "materialID", dialog.MaterialID },
							};

							var script =
@"env = world.GetObject(envID)
for p in area.Range():
	builder = Dwarrowdelf.Server.ItemObjectBuilder(itemID, materialID)
	item = builder.Create(world)
	item.MoveTo(env, p)";

							var msg = new Dwarrowdelf.Messages.IPScriptMessage(script, args);

							GameData.Data.User.Send(msg);
						}
					}
					break;

				case ClientToolMode.CreateLiving:
					{
						var dialog = new CreateLivingDialog();
						dialog.Owner = this;
						dialog.SetContext(env, selection.SelectionIntRectZ);
						var res = dialog.ShowDialog();

						if (res == true)
						{
							GameData.Data.User.Send(new CreateLivingMessage()
							{
								EnvironmentID = dialog.Environment.ObjectID,
								Area = dialog.Area,

								Name = dialog.LivingName,
								LivingID = dialog.LivingID,

								IsControllable = dialog.IsControllable,
								IsGroup = dialog.IsGroup,
							});
						}
					}
					break;

				case ClientToolMode.ConstructBuilding:
					{
						var dialog = new ConstructBuildingDialog();
						dialog.Owner = this;
						dialog.SetContext(env, selection.SelectionIntRectZ);
						var res = dialog.ShowDialog();

						if (res == true)
						{
							var id = dialog.BuildingID;
							var site = new ConstructionSite(env, id, selection.SelectionIntRectZ);
							env.AddAreaElement(site);
						}
					}
					break;

				case ClientToolMode.ConstructRemove:
					env.ConstructManager.RemoveArea(selection.SelectionIntRectZ);
					break;

				case ClientToolMode.ConstructWall:
				case ClientToolMode.ConstructFloor:
				case ClientToolMode.ConstructPavement:
					{
						ConstructMode mode;

						switch (this.mainWindowTools.ToolMode)
						{
							case ClientToolMode.ConstructWall:
								mode = ConstructMode.Wall;
								break;

							case ClientToolMode.ConstructFloor:
								mode = ConstructMode.Floor;
								break;

							case ClientToolMode.ConstructPavement:
								mode = ConstructMode.Pavement;
								break;

							default:
								throw new Exception();
						}

						var dialog = new ConstructDialog();
						dialog.Owner = this;
						dialog.ConstructMode = mode;
						var res = dialog.ShowDialog();

						if (res == true)
						{
							var area = selection.SelectionIntRectZ;
							var filter = dialog.GetItemFilter();

							env.ConstructManager.AddConstructJob(mode, area, filter);
						}
					}
					break;

				default:
					throw new Exception();
			}

			this.MapControl.Selection = new MapSelection();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			m_cmdHandler = new MainWindowCommandHandler(this);

			AddHandler(UI.MapControl.MouseClickedEvent, new MouseButtonEventHandler(OnMouseClicked));
		}

		void OnMouseClicked(object sender, MouseButtonEventArgs ev)
		{
			if (this.mainWindowTools.ToolMode == ClientToolMode.Info)
			{
				var env = this.MapControl.Environment;

				if (env == null)
					return;

				var ml = this.MapControl.ScreenPointToMapLocation(ev.GetPosition(this.MapControl));

				var obs = new List<object>();

				var elem = env.GetElementAt(ml);
				if (elem != null)
					obs.Add(env.GetElementAt(ml));

				obs.AddRange(env.GetContents(ml));

				if (obs.Count == 1)
				{
					object ob = obs[0];
					ShowObjectInDialog(ob);
				}
				else if (obs.Count > 0)
				{
					var ctxMenu = (ContextMenu)this.FindResource("objectSelectorContextMenu");

					ctxMenu.ItemsSource = obs;
					ctxMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
					ctxMenu.PlacementTarget = this.MapControl;
					var rect = this.MapControl.MapRectToScreenPointRect(new IntGrid2(ml.ToIntPoint(), new IntSize2(1, 1)));
					ctxMenu.PlacementRectangle = rect;

					ctxMenu.IsOpen = true;
				}

				ev.Handled = true;
			}
		}

		void OnObjectSelectContextMenuClick(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)e.OriginalSource;
			var ob = item.Header;
			ShowObjectInDialog(ob);
		}

		void ShowObjectInDialog(object ob)
		{
			var dlg = new ObjectEditDialog();
			dlg.DataContext = ob;
			dlg.Owner = this;
			dlg.Show();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			var p = (Win32.WindowPlacement)Properties.Settings.Default.MainWindowPlacement;
			if (p != null)
				Win32.Helpers.LoadWindowPlacement(this, p);

			if (ClientConfig.AutoConnect)
			{
				var task = GameData.Data.ConnectManager.StartServerAndConnectPlayer();
				task.ContinueWith((t) =>
				{
					MessageBox.Show(this, t.Exception.ToString(), "Start and Connect failed");
				}, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
			}
		}

		public MasterMapControl MapControl { get { return map; } }

		public GameData Data { get { return GameData.Data; } }

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			switch (m_closeStatus)
			{
				case CloseStatus.None:
					m_closeStatus = CloseStatus.ShuttingDown;

					e.Cancel = true;

					var p = Win32.Helpers.SaveWindowPlacement(this);
					Properties.Settings.Default.MainWindowPlacement = p;
					Properties.Settings.Default.Save();

					var task = GameData.Data.ConnectManager.DisconnectAndStop();
					task.ContinueWith((t) =>
					{
						if (t.Status != TaskStatus.RanToCompletion)
						{
							this.Dispatcher.BeginInvoke(new Action<Exception>((exc) =>
							{
								m_closeStatus = CloseStatus.None;
								MessageBox.Show(exc.ToString(), "Error closing down");
								Application.Current.Shutdown();
							}
							), t.Exception);
						}
						else
						{
							m_closeStatus = CloseStatus.Ready;
							this.Dispatcher.BeginInvoke(new Action(() => Close()));
						}
					});

					break;

				case CloseStatus.ShuttingDown:
					e.Cancel = true;
					break;

				case CloseStatus.Ready:
					break;
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			map.Dispose();

			base.OnClosed(e);
		}

		private void FilterItems(object sender, FilterEventArgs e)
		{
			if (e.Item is ItemObject)
				e.Accepted = true;
			else
				e.Accepted = false;
		}

		private void FilterLivings(object sender, FilterEventArgs e)
		{
			if (e.Item is LivingObject)
				e.Accepted = true;
			else
				e.Accepted = false;
		}

		public MovableObject FollowObject
		{
			get { return m_followObject; }

			set
			{
				if (m_followObject == value)
					return;

				if (m_followObject != null)
					m_followObject.ObjectMoved -= FollowedObjectMoved;

				m_followObject = value;

				if (m_followObject != null)
				{
					m_followObject.ObjectMoved += FollowedObjectMoved;
					FollowedObjectMoved(m_followObject, m_followObject.Environment, m_followObject.Location);
				}

				Notify("FollowObject");
			}
		}

		void FollowedObjectMoved(MovableObject ob, ContainerObject dst, IntPoint3 loc)
		{
			EnvironmentObject env = dst as EnvironmentObject;

			map.ScrollTo(env, loc);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			var toolData = MapToolBar.ToolDatas.Select(kvp => kvp.Value).FirstOrDefault(td => td.Key == e.Key);

			if (toolData != null)
			{
				this.mainWindowTools.ToolMode = toolData.Mode;

				if (e.Key == Key.Escape)
					map.Focus();

				e.Handled = true;
				base.OnKeyDown(e);
				return;
			}

			switch (e.Key)
			{
				case Key.OemPeriod:
					if (GameData.Data.User != null)
						GameData.Data.User.SendProceedTurn();
					break;

				case Key.Space:
					ClientCommands.AutoAdvanceTurnCommand.Execute(null, this);
					break;

				default:
					base.OnKeyDown(e);
					return;
			}

			e.Handled = true;
			base.OnKeyDown(e);
		}

		internal EnvironmentObject Map
		{
			get { return map.Environment; }
			set { map.Environment = value; }
		}

		private void MenuItem_Click_JobTreeView(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			string tag = (string)item.Tag;
			IJob job = (IJob)jobTreeView.SelectedValue;

			if (job == null)
				return;

			switch (tag)
			{
				case "Abort":
					job.Abort();
					break;

				default:
					throw new Exception();
			}
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		void Notify(string info)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(info));
		}

		private void ObjectsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0)
				return;

			if (e.AddedItems.Count != 1)
				throw new Exception();

			var movable = e.AddedItems[0] as MovableObject;

			this.FollowObject = null;

			if (movable == null || movable.Environment == null)
				return;

			map.ScrollTo(movable.Environment, movable.Location);
		}

		private void MessageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1)
				return;

			var msg = (GameEvent)e.AddedItems[0];

			if (msg.Environment == null)
				return;

			map.ScrollTo(msg.Environment, msg.Location);
		}
	}
}
