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

namespace Dwarrowdelf.Client.UI
{
	partial class MainWindow : Window, INotifyPropertyChanged
	{
		GameObject m_followObject;
		bool m_closing;

		bool m_serverInAppDomain = true;

		bool m_autoConnect = true;
		bool m_autoEnterGame = true;

		// Stores previous user values for setTerrainData
		SetTerrainData m_setTerrainData;

		ServerInAppDomain m_server;

		LogOnDialog m_logOnDialog;

		MainWindowCommandHandler m_cmdHandler;

		public MainWindow()
		{
			InitializeComponent();

			this.MapControl.GotSelection += MapControl_GotSelection;
			this.mainWindowTools.ToolModeChanged += MainWindowTools_ToolModeChanged;

			this.mainWindowTools.ToolMode = ClientToolMode.Info;
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
				case ClientToolMode.DesignationFellTree:
				case ClientToolMode.DesignationRemove:
				case ClientToolMode.SetTerrain:
				case ClientToolMode.CreateLiving:
					this.MapControl.SelectionMode = MapSelectionMode.Cuboid;
					break;

				case ClientToolMode.CreateStockpile:
				case ClientToolMode.ConstructBuilding:
					this.MapControl.SelectionMode = MapSelectionMode.Rectangle;
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
					env.Designations.RemoveArea(selection.SelectionCuboid);
					break;

				case ClientToolMode.DesignationMine:
					env.Designations.AddArea(selection.SelectionCuboid, DesignationType.Mine);
					break;

				case ClientToolMode.DesignationStairs:
					env.Designations.AddArea(selection.SelectionCuboid, DesignationType.CreateStairs);
					break;

				case ClientToolMode.DesignationFellTree:
					env.Designations.AddArea(selection.SelectionCuboid, DesignationType.FellTree);
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

							GameData.Data.Connection.Send(new SetTilesMessage()
							{
								MapID = map.Environment.ObjectID,
								Cube = selection.SelectionCuboid,
								TerrainID = data.TerrainID,
								TerrainMaterialID = data.TerrainMaterialID,
								InteriorID = data.InteriorID,
								InteriorMaterialID = data.InteriorMaterialID,
								Grass = data.Grass,
								WaterLevel = data.Water.HasValue ? (data.Water == true ? (byte?)TileData.MaxWaterLevel : (byte?)0) : null,
							});
						}
					}
					break;

				case ClientToolMode.CreateStockpile:
					{
						var stockpile = new Stockpile(env, selection.SelectionIntRectZ);
						env.AddMapElement(stockpile);

						// Add one empty, so StockpileEditor works...
						stockpile.Criterias.Add(new StockpileCriteria());

						var dlg = new ObjectEditDialog();
						dlg.DataContext = stockpile;
						dlg.Owner = this;
						dlg.Show();
					}
					break;

				case ClientToolMode.CreateItem:
					{
						var dialog = new CreateItemDialog();
						dialog.Owner = this;
						dialog.SetContext(env, selection.SelectionCuboid);
						var res = dialog.ShowDialog();

						if (res == true)
						{
							GameData.Data.Connection.Send(new CreateItemMessage()
							{
								ItemID = dialog.ItemID,
								MaterialID = dialog.MaterialID,
								EnvironmentID = dialog.Environment != null ? dialog.Environment.ObjectID : ObjectID.NullObjectID,
								Area = dialog.Area,
							});
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
							GameData.Data.Connection.Send(new CreateLivingMessage()
							{
								EnvironmentID = dialog.Environment.ObjectID,
								Area = dialog.Area,

								Name = dialog.LivingName,
								LivingID = dialog.LivingID,

								IsControllable = dialog.IsControllable,
								IsHerd = dialog.IsHerd,
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
							env.AddMapElement(site);
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

			PopulateMenus();

			m_cmdHandler = new MainWindowCommandHandler(this);

			AddHandler(UI.MapControl.MouseClickedEvent, new MouseButtonEventHandler(OnMouseClicked));
		}

		void OnMouseClicked(object sender, MouseButtonEventArgs ev)
		{
			if (this.mainWindowTools.ToolMode == ClientToolMode.Info)
			{
				var ml = this.MapControl.MapControl.ScreenPointToMapLocation(ev.GetPosition(this.MapControl));

				var env = this.MapControl.Environment;

				List<object> obs = new List<object>();
				obs.AddRange(env.GetContents(ml));
				obs.AddRange(env.GetElementsAt(ml));

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
					ctxMenu.PlacementTarget = this.MapControl.MapControl;
					var rect = this.MapControl.MapControl.MapRectToScreenPointRect(new IntRect(ml.ToIntPoint(), new IntSize(1, 1)));
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

		void PopulateMenus()
		{
			foreach (var content in dockingManager.DockableContents)
			{
				var item = new MenuItem()
				{
					Tag = content,
					Header = content.Title,
					IsChecked = true,
				};

				item.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Click_ShowWindow));
				contentMenu.Items.Add(item);

				content.StateChanged += new RoutedEventHandler(dockableContent_StateChanged);
			}
		}

		void dockableContent_StateChanged(object sender, RoutedEventArgs e)
		{
			var content = (AvalonDock.DockableContent)e.Source;
			MenuItem item = null;
			foreach (MenuItem i in contentMenu.Items)
			{
				if (i.Tag == content)
				{
					item = i;
					break;
				}
			}

			if (item == null)
				throw new Exception();

			if (content.State == AvalonDock.DockableContentState.Hidden)
				item.IsChecked = false;
			else
				item.IsChecked = true;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			var p = (Win32.WindowPlacement)Properties.Settings.Default.MainWindowPlacement;
			if (p != null)
				Win32.Helpers.LoadWindowPlacement(this, p);

			if (m_autoConnect)
				Connect();
		}

		private void dockingManager_Loaded(object sender, RoutedEventArgs e)
		{
			RestoreLayout();
		}

		public AvalonDock.DockingManager Dock { get { return dockingManager; } }

		public MasterMapControl MapControl { get { return map; } }

		public GameData Data { get { return GameData.Data; } }

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			if (m_closing)
				return;

			var p = Win32.Helpers.SaveWindowPlacement(this);
			Properties.Settings.Default.MainWindowPlacement = p;
			Properties.Settings.Default.Save();

			SaveLayout();

			if (GameData.Data.Connection != null)
			{
				e.Cancel = true;
				m_closing = true;

				Disconnect();
			}

			this.FollowObject = null;
			map.Environment = null;
			map.Dispose();
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
			if (e.Item is Living)
				e.Accepted = true;
			else
				e.Accepted = false;
		}

		public GameObject FollowObject
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

		void FollowedObjectMoved(GameObject ob, GameObject dst, IntPoint3D loc)
		{
			Environment env = dst as Environment;

			map.ScrollTo(env, loc);
		}

		static bool KeyIsDir(Key key)
		{
			switch (key)
			{
				case Key.Up: break;
				case Key.Down: break;
				case Key.Left: break;
				case Key.Right: break;
				case Key.Home: break;
				case Key.End: break;
				case Key.PageUp: break;
				case Key.PageDown: break;
				default:
					return false;
			}
			return true;
		}

		void SetScrollDirection()
		{
			var dir = Direction.None;

			if (Keyboard.IsKeyDown(Key.Home))
				dir |= Direction.NorthWest;
			else if (Keyboard.IsKeyDown(Key.PageUp))
				dir |= Direction.NorthEast;
			if (Keyboard.IsKeyDown(Key.PageDown))
				dir |= Direction.SouthEast;
			else if (Keyboard.IsKeyDown(Key.End))
				dir |= Direction.SouthWest;

			if (Keyboard.IsKeyDown(Key.Up))
				dir |= Direction.North;
			else if (Keyboard.IsKeyDown(Key.Down))
				dir |= Direction.South;

			if (Keyboard.IsKeyDown(Key.Left))
				dir |= Direction.West;
			else if (Keyboard.IsKeyDown(Key.Right))
				dir |= Direction.East;

			var fast = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

			var v = IntVector.FromDirection(dir);

			if (fast)
				v *= 4;

			map.ScrollToDirection(v);
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if (GameData.Data.User == null)
			{
				base.OnPreviewKeyDown(e);
				return;
			}

			e.Handled = true;

			if (KeyIsDir(e.Key) || e.Key == Key.LeftShift || e.Key == Key.RightShift)
			{
				SetScrollDirection();
			}
			else if (e.Key == Key.OemPeriod)
			{
				GameData.Data.User.SendProceedTurn();
			}
			else if (e.Key == Key.Space)
			{
				ClientCommands.AutoAdvanceTurnCommand.Execute(null, this);
			}
			else if (e.Key == Key.B)
			{
				this.mainWindowTools.ToolMode = ClientToolMode.ConstructBuilding;
			}
			else if (e.Key == Key.M)
			{
				this.mainWindowTools.ToolMode = ClientToolMode.DesignationMine;
			}
			else if (e.Key == Key.R)
			{
				this.mainWindowTools.ToolMode = ClientToolMode.DesignationRemove;
			}
			else if (e.Key == Key.F)
			{
				this.mainWindowTools.ToolMode = ClientToolMode.DesignationFellTree;
			}
			else if (e.Key == Key.T)
			{
				this.mainWindowTools.ToolMode = ClientToolMode.SetTerrain;
			}
			else if (e.Key == Key.S)
			{
				this.mainWindowTools.ToolMode = ClientToolMode.CreateStockpile;
			}
			else if (e.Key == Key.L)
			{
				this.mainWindowTools.ToolMode = ClientToolMode.CreateLiving;
			}
			else if (e.Key == Key.I)
			{
				this.mainWindowTools.ToolMode = ClientToolMode.CreateItem;
			}
			else if (e.Key == Key.Add)
			{
				map.ZoomIn();
			}
			else if (e.Key == Key.Subtract)
			{
				map.ZoomOut();
			}
			if (e.Key == Key.Escape)
			{
				this.mainWindowTools.ToolMode = ClientToolMode.Info;
				this.Focus(); // XXX focus mainwindow instead of mapcontrol, it works somehow better
			}
			else
			{
				e.Handled = false;
			}

			base.OnPreviewKeyDown(e);
		}

		protected override void OnPreviewKeyUp(KeyEventArgs e)
		{
			e.Handled = true;

			if (KeyIsDir(e.Key) || e.Key == Key.LeftShift || e.Key == Key.RightShift)
			{
				SetScrollDirection();
			}
			else
			{
				e.Handled = false;
			}

			base.OnPreviewKeyUp(e);
		}

		protected override void OnPreviewTextInput(TextCompositionEventArgs e)
		{
			string text = e.Text;

			e.Handled = true;

			if (text == ">")
			{
				map.Z--;
			}
			else if (text == "<")
			{
				map.Z++;
			}
			else
			{
				e.Handled = false;
			}

			base.OnPreviewTextInput(e);
		}

		internal Environment Map
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

		void SetLogOnText(string text)
		{
			if (m_logOnDialog == null)
			{
				this.IsEnabled = false;

				m_logOnDialog = new LogOnDialog();
				m_logOnDialog.Owner = this;
				m_logOnDialog.SetText(text);
				m_logOnDialog.Show();
			}
			else
			{
				m_logOnDialog.SetText(text);
			}
		}

		void CloseLoginDialog()
		{
			if (m_logOnDialog != null)
			{
				m_logOnDialog.Close();
				m_logOnDialog = null;

				this.IsEnabled = true;
				this.Focus();
			}
		}

		private void Connect_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection != null)
				return;

			Connect();
		}

		private void Disconnect_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection == null)
				return;

			Disconnect();
		}


		private void EnterGame_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.User == null || GameData.Data.User.IsPlayerInGame)
				return;

			EnterGame();
		}

		private void ExitGame_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.User == null || !GameData.Data.User.IsPlayerInGame)
				return;

			ExitGame();
		}

		private void Save_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection == null)
				return;

			var msg = new SaveRequestMessage();

			GameData.Data.Connection.Send(msg);
		}

		private void Load_Button_Click(object sender, RoutedEventArgs e)
		{

		}


		void Connect()
		{
			if (m_serverInAppDomain)
			{
				SetLogOnText("Starting server");

				m_server = new ServerInAppDomain();
				m_server.Started += () => this.Dispatcher.BeginInvoke(new Action(OnServerStarted));
				m_server.StatusChanged += (str) => this.Dispatcher.BeginInvoke(new Action<string>(SetLogOnText), str);
				m_server.Start();
			}
			else
			{
				OnServerStarted();
			}
		}

		void OnServerStarted()
		{
			var world = new World();
			GameData.Data.World = world;

			GameData.Data.Connection = new ClientConnection(world);
			GameData.Data.Connection.DisconnectEvent += OnDisconnected;

			SetLogOnText("Connecting");

			GameData.Data.Connection.BeginLogOn("tomba", OnConnected);
		}

		void OnConnected(ClientUser user, string error)
		{
			GameData.Data.Connection.LogOutEvent += OnLoggedOut;

			if (error != null)
			{
				CloseLoginDialog();
				MessageBox.Show(error, "Connection Failed");
				return;
			}

			GameData.Data.User = user;

			if (user.IsPlayerInGame || !m_autoEnterGame)
			{
				CloseLoginDialog();
				return;
			}

			EnterGame();
		}



		void Disconnect()
		{
			if (GameData.Data.User.IsPlayerInGame)
			{
				SetLogOnText("Saving");

				ClientSaveManager.SaveEvent += OnGameSaved;

				GameData.Data.Connection.Send(new SaveRequestMessage());
			}
			else
			{
				GameData.Data.User = null;
				SetLogOnText("Logging Out");
				GameData.Data.Connection.SendLogOut();
			}
		}

		void OnGameSaved()
		{
			GameData.Data.User = null;

			ClientSaveManager.SaveEvent -= OnGameSaved;

			SetLogOnText("Logging Out");
			GameData.Data.Connection.SendLogOut();
		}

		void OnLoggedOut()
		{
			CloseLoginDialog();
			GameData.Data.Connection.LogOutEvent -= OnLoggedOut;
			GameData.Data.Connection.DisconnectEvent -= OnDisconnected;
			GameData.Data.Connection = null;

			if (m_server != null)
			{
				m_server.Stop();
				m_server = null;
			}

			if (m_closing)
				Close();
		}

		void OnDisconnected()
		{
			GameData.Data.Connection.LogOutEvent -= OnLoggedOut;
			GameData.Data.Connection.DisconnectEvent -= OnDisconnected;
			GameData.Data.Connection = null;

			if (m_server != null)
			{
				m_server.Stop();
				m_server = null;
			}
		}








		void EnterGame()
		{
			SetLogOnText("Entering Game");

			GameData.Data.User.SendEnterGame(OnEnteredGame);
		}

		void OnEnteredGame()
		{
			CloseLoginDialog();
			GameData.Data.User.ExitedGameEvent += OnExitedGame;
		}

		void ExitGame()
		{
			GameData.Data.User.SendExitGame();
		}

		void OnExitedGame()
		{
			GameData.Data.User.ExitedGameEvent -= OnExitedGame;
		}




		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		void Notify(string info)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(info));
		}

		private void Button_Click_GC(object sender, RoutedEventArgs e)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		private void Button_Click_Break(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Debugger.Break();
		}

		void SaveLayout()
		{
			var path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
			path = System.IO.Path.Combine(path, "Dwarrowdelf");
			if (!System.IO.Directory.Exists(path))
				System.IO.Directory.CreateDirectory(path);
			path = System.IO.Path.Combine(path, "WindowLayout.xml");
			dockingManager.SaveLayout(path);
		}

		void RestoreLayout()
		{
			var path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
			path = System.IO.Path.Combine(path, "Dwarrowdelf", "WindowLayout.xml");
			if (System.IO.File.Exists(path))
				dockingManager.RestoreLayout(path);
		}

		private void MenuItem_Click_ShowWindow(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			var content = (AvalonDock.DockableContent)item.Tag;
			content.Show();
		}

		private void Button_Click_FullScreen(object sender, RoutedEventArgs e)
		{
			var button = (System.Windows.Controls.Primitives.ToggleButton)sender;

			if (button.IsChecked.Value)
			{
				this.WindowStyle = System.Windows.WindowStyle.None;
				this.Topmost = true;
				this.WindowState = System.Windows.WindowState.Maximized;
			}
			else
			{
				this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
				this.Topmost = false;
				this.WindowState = System.Windows.WindowState.Normal;
			}
		}

		private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (GameData.Data.User != null)
			{
				GameData.Data.Connection.Send(new SetWorldConfigMessage()
				{
					MinTickTime = TimeSpan.FromMilliseconds(slider.Value),
				});
			}
		}

		private void ObjectsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0)
				return;

			var ob = (GameObject)e.AddedItems[0];

			this.FollowObject = null;

			map.ScrollTo(ob.Environment, ob.Location);
		}

		private void ObjectsListBoxItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			var item = (ListBoxItem)sender;
			var ob = (GameObject)item.Content;

			this.FollowObject = null;

			map.ScrollTo(ob.Environment, ob.Location);
		}

		private void ObjectsListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var item = (ListBoxItem)sender;
			var ob = (GameObject)item.Content;

			this.FollowObject = ob;
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

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Dwarrowdelf.Client.Symbols.SymbolEditorDialog();
			dialog.SymbolDrawingCache = GameData.Data.SymbolDrawingCache;
			dialog.Show();
		}
	}
}
