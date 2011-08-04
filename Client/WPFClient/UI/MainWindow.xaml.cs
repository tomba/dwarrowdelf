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
using Dwarrowdelf.Client.UI;

namespace Dwarrowdelf.Client
{
	enum ToolMode
	{
		Info,
		DesignationRemove,
		DesignationMine,
		DesignationFellTree,
		SetTerrain,
		CreateStockpile,
		CreateItem,
		CreateLiving,
	}

	partial class MainWindow : Window, INotifyPropertyChanged
	{
		GameObject m_followObject;
		bool m_closing;
		DispatcherTimer m_timer;
		ManualJobSource m_manualJobSource;

		bool m_serverInAppDomain = true;

		bool m_autoConnect = true;
		bool m_autoEnterGame = true;

		public MainWindow()
		{
			this.CurrentTileInfo = new TileInfo();

			m_toolMode = Client.ToolMode.Info;

			InitializeComponent();

			map.MouseDown += MapControl_MouseDown;

			this.CommandBindings.Add(new CommandBinding(ClientCommands.AutoAdvanceTurnCommand, AutoAdvanceTurnHandler));
			this.CommandBindings.Add(new CommandBinding(ClientCommands.OpenBuildItemDialogCommand, OpenBuildItemHandler));
			this.CommandBindings.Add(new CommandBinding(ClientCommands.OpenConstructBuildingDialogCommand, OpenConstructBuildingHandler));
			this.CommandBindings.Add(new CommandBinding(ClientCommands.OpenConsoleCommand, OpenConsoleHandler));

			this.MapControl.GotSelection += new Action<MapSelection>(MapControl_GotSelection);
		}

		ToolMode m_toolMode;
		public ToolMode ToolMode
		{
			get { return m_toolMode; }
			set
			{
				m_toolMode = value;

				switch (m_toolMode)
				{
					case Client.ToolMode.Info:
						this.MapControl.SelectionMode = MapSelectionMode.None;
						break;

					case Client.ToolMode.CreateItem:
						this.MapControl.SelectionMode = MapSelectionMode.Point;
						break;

					case Client.ToolMode.DesignationMine:
					case Client.ToolMode.DesignationFellTree:
					case Client.ToolMode.DesignationRemove:
					case Client.ToolMode.SetTerrain:
					case Client.ToolMode.CreateLiving:
						this.MapControl.SelectionMode = MapSelectionMode.Cuboid;
						break;

					case Client.ToolMode.CreateStockpile:
						this.MapControl.SelectionMode = MapSelectionMode.Rectangle;
						break;

					default:
						break;
				}

				Notify("ToolMode");
			}
		}

		SetTerrainData m_setTerrainData;

		void MapControl_GotSelection(MapSelection selection)
		{
			var env = this.Map;

			switch (m_toolMode)
			{
				case Client.ToolMode.DesignationRemove:
					env.Designations.RemoveArea(selection.SelectionCuboid);
					break;

				case Client.ToolMode.DesignationMine:
					env.Designations.AddArea(selection.SelectionCuboid, DesignationType.Mine);
					break;

				case Client.ToolMode.DesignationFellTree:
					env.Designations.AddArea(selection.SelectionCuboid, DesignationType.FellTree);
					break;

				case Client.ToolMode.SetTerrain:
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
								Cube = map.Selection.SelectionCuboid,
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

				case Client.ToolMode.CreateStockpile:
					{
						var dialog = new StockpileDialog();
						dialog.Owner = this;
						dialog.SetContext(env, selection.SelectionIntRectZ);
						var res = dialog.ShowDialog();

						if (res == true)
						{
							var type = dialog.StockpileType;
							var stockpile = new Stockpile(env, selection.SelectionIntRectZ, type);
							env.AddStockpile(stockpile);
						}
					}
					break;

				case Client.ToolMode.CreateItem:
					{
						var dialog = new CreateItemDialog();
						dialog.Owner = this;
						dialog.SetContext(env, selection.SelectionPoint);
						var res = dialog.ShowDialog();

						if (res == true)
						{
							GameData.Data.Connection.Send(new CreateItemMessage()
							{
								ItemID = dialog.ItemID,
								MaterialID = dialog.MaterialID,
								EnvironmentID = dialog.Environment != null ? dialog.Environment.ObjectID : ObjectID.NullObjectID,
								Location = dialog.Location,
							});
						}
					}
					break;

				case Client.ToolMode.CreateLiving:
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

				default:
					break;
			}

			this.MapControl.Selection = new MapSelection();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			PopulateMenus();

			var dpd = DependencyPropertyDescriptor.FromProperty(GameData.CurrentObjectProperty, typeof(GameData));
			dpd.AddValueChanged(GameData.Data, (ob, ev) => this.FollowObject = GameData.Data.CurrentObject);

			CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
			m_timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, OnTimerCallback, this.Dispatcher);

			toolModeComboBox.ItemsSource = Enum.GetValues(typeof(ToolMode)).Cast<ToolMode>().OrderBy(id => id.ToString()).ToArray();
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

		int m_fpsCounter;
		void CompositionTarget_Rendering(object sender, EventArgs e)
		{
			m_fpsCounter++;
		}

		void OnTimerCallback(object ob, EventArgs args)
		{
			fpsTextBlock.Text = ((double)m_fpsCounter).ToString();
			m_fpsCounter = 0;
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
				else
				{
					//map.InvalidateTiles(); ??? XXX

					this.CurrentTileInfo.Environment = null;
					this.CurrentTileInfo.Location = new IntPoint3D();
				}

				Notify("FollowObject");
			}
		}

		void FollowedObjectMoved(GameObject ob, GameObject dst, IntPoint3D loc)
		{
			Environment env = dst as Environment;

			map.ScrollTo(env, loc);

			this.CurrentTileInfo.Environment = env;
			this.CurrentTileInfo.Location = loc;
		}

		public TileInfo CurrentTileInfo { get; private set; }


		void OpenConstructBuildingHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var area = map.Selection.SelectionIntRectZ;
			var env = map.Environment;

			if (area.IsNull)
				return;

			var dialog = new ConstructBuildingDialog();
			dialog.Owner = this;
			dialog.SetContext(env, area);
			var res = dialog.ShowDialog();

			if (res == true)
			{
				var id = dialog.BuildingID;

				env.CreateConstructionSite(id, area);
			}
		}

		void OpenBuildItemHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var p = map.Selection.SelectionCuboid.Corner1;
			var env = map.Environment;

			var building = env.GetBuildingAt(p);

			if (building == null)
				return;

			var dialog = new BuildItemDialog();
			dialog.Owner = this;
			dialog.SetContext(building);
			var res = dialog.ShowDialog();

			if (res == true)
			{
				building.AddBuildOrder(dialog.BuildableItem);
			}
		}

		void OpenConsoleHandler(object sender, ExecutedRoutedEventArgs e)
		{
			var dialog = new ConsoleDialog();
			dialog.Owner = this;
			dialog.Show();
		}

		void AutoAdvanceTurnHandler(object sender, ExecutedRoutedEventArgs e)
		{
			GameData.Data.IsAutoAdvanceTurn = !GameData.Data.IsAutoAdvanceTurn;
		}

		static Direction KeyToDir(Key key)
		{
			Direction dir;

			switch (key)
			{
				case Key.Up: dir = Direction.North; break;
				case Key.Down: dir = Direction.South; break;
				case Key.Left: dir = Direction.West; break;
				case Key.Right: dir = Direction.East; break;
				case Key.Home: dir = Direction.NorthWest; break;
				case Key.End: dir = Direction.SouthWest; break;
				case Key.PageUp: dir = Direction.NorthEast; break;
				case Key.PageDown: dir = Direction.SouthEast; break;
				default:
					throw new Exception();
			}

			return dir;
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
				ClientCommands.OpenConstructBuildingDialogCommand.Execute(null, this);
			}
			else if (e.Key == Key.A)
			{
				ClientCommands.OpenBuildItemDialogCommand.Execute(null, this);
			}
			else if (e.Key == Key.M)
			{
				this.ToolMode = Client.ToolMode.DesignationMine;
			}
			else if (e.Key == Key.R)
			{
				this.ToolMode = Client.ToolMode.DesignationRemove;
			}
			else if (e.Key == Key.F)
			{
				this.ToolMode = Client.ToolMode.DesignationFellTree;
			}
			else if (e.Key == Key.T)
			{
				this.ToolMode = Client.ToolMode.SetTerrain;
			}
			else if (e.Key == Key.S)
			{
				this.ToolMode = Client.ToolMode.CreateStockpile;
			}
			else if (e.Key == Key.L)
			{
				this.ToolMode = Client.ToolMode.CreateLiving;
			}
			else if (e.Key == Key.I)
			{
				this.ToolMode = Client.ToolMode.CreateItem;
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
				this.ToolMode = Client.ToolMode.Info;
				this.MapControl.Focus();
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

		void MapControl_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.RightButton == MouseButtonState.Pressed)
			{
				var ml = new IntPoint3D(map.ScreenPointToMapLocation(e.GetPosition(map)), map.Z);

				if (map.Selection.SelectionCuboid.Contains(ml))
					return;

				map.Selection = new MapSelection(ml, ml);
			}
		}

		internal Environment Map
		{
			get { return map.Environment; }
			set { map.Environment = value; }
		}

		private void MenuItem_Click_Job(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			string tag = (string)item.Tag;

			if (tag == "Mine")
			{
				var env = map.Environment;

				foreach (var p in map.Selection.SelectionCuboid.Range())
				{
					if (env.GetTerrainID(p) != TerrainID.NaturalWall)
						continue;

					var job = new Jobs.AssignmentGroups.MoveMineAssignment(null, ActionPriority.Normal, env, p, MineActionType.Mine);
					m_manualJobSource.Add(job);
				}
			}
			else if (tag == "FellTree")
			{
				var env = map.Environment;

				var job = new Jobs.JobGroups.FellTreeParallelJob(env, ActionPriority.Normal, map.Selection.SelectionCuboid);
				m_manualJobSource.Add(job);
			}
			else if (tag == "MineArea")
			{
				var area = map.Selection.SelectionCuboid;
				var env = map.Environment;

				var job = new Jobs.JobGroups.MineAreaJob(env, ActionPriority.Normal, area, MineActionType.Mine);
				m_manualJobSource.Add(job);
			}
			else if (tag == "Goto")
			{
				var p = map.Selection.SelectionCuboid.Corner1;
				var env = map.Environment;

				var job = new Jobs.Assignments.MoveAssignment(null, ActionPriority.Normal, env, p, DirectionSet.Exact);
				m_manualJobSource.Add(job);
			}
			else if (tag == "Loiter")
			{
				var job = new Jobs.AssignmentGroups.LoiterAssignment(null, ActionPriority.Normal, map.Environment);
				m_manualJobSource.Add(job);
			}
			else if (tag == "Consume")
			{
				var env = map.Environment;
				var consumables = map.Selection.SelectionCuboid.Range()
					.SelectMany(l =>
						env.GetContents(l).OfType<ItemObject>().Where(o => o.RefreshmentValue > 0 || o.NutritionalValue > 0)
					);

				foreach (var c in consumables)
				{
					var job = new Jobs.AssignmentGroups.MoveConsumeAssignment(null, ActionPriority.Normal, c);
					m_manualJobSource.Add(job);
				}
			}
			else if (tag == "Attack")
			{
				var p = map.Selection.SelectionCuboid.Corner1;
				var env = map.Environment;
				var ob = env.GetFirstObject(p);
				if (ob is Living)
				{
					var job = new Jobs.Assignments.AttackAssignment(null, ActionPriority.Normal, (ILiving)ob);
					m_manualJobSource.Add(job);
				}
			}
			else
			{
				throw new Exception();
			}
		}

		private void Get_Button_Click(object sender, RoutedEventArgs e)
		{
			var plr = GameData.Data.CurrentObject;
			if (!(plr.Environment is Environment))
				throw new Exception();

			var list = currentTileItems.SelectedItems.Cast<GameObject>();

			if (list.Count() == 0)
				return;

			if (list.Contains(plr))
				return;

			Debug.Assert(list.All(o => o.Environment == plr.Environment));
			Debug.Assert(list.All(o => o.Location == plr.Location));

			plr.RequestAction(new GetAction(list, ActionPriority.Normal));
		}

		private void Drop_Button_Click(object sender, RoutedEventArgs e)
		{
			var list = inventoryListBox.SelectedItems.Cast<GameObject>();

			if (list.Count() == 0)
				return;

			var plr = GameData.Data.CurrentObject;

			plr.RequestAction(new DropAction(list, ActionPriority.Normal));
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

		LogOnDialog m_logOnDialog;

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


		ServerInAppDomain m_server;

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

			m_manualJobSource = new ManualJobSource(GameData.Data.World.JobManager);

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

		private void currentObjectCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			currentObjectComboBox.SelectedItem = GameData.Data.World.Controllables.FirstOrDefault();
		}

		private void currentObjectCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			currentObjectComboBox.SelectedItem = null;
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
	}

	class MyInteriorsConverter : ListConverter<Tuple<InteriorInfo, MaterialInfo>>
	{
		public MyInteriorsConverter() : base(item => item.Item1.Name + " (" + item.Item2.Name + ")") { }
	}

	class MyTerrainsConverter : ListConverter<Tuple<TerrainInfo, MaterialInfo>>
	{
		public MyTerrainsConverter() : base(item => item.Item1.Name + " (" + item.Item2.Name + ")") { }
	}

	class MyWatersConverter : ListConverter<byte>
	{
		public MyWatersConverter() : base(item => item.ToString()) { }
	}

	class MyBuildingsConverter : ListConverter<BuildingObject>
	{
		public MyBuildingsConverter() : base(item => item.BuildingInfo.Name) { }
	}

	class MyGrassesConverter : ListConverter<bool>
	{
		public MyGrassesConverter() : base(item => item.ToString()) { }
	}
}
