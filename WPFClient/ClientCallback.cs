using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace MyGame
{
	class ClientCallback : IClientCallback
	{
		public ClientCallback()
		{
			MyDebug.WriteLine("ClientCallback()");
		}

		#region IClientCallback Members

		public void LoginReply(ObjectID playerID)
		{
			try
			{
				ClientGameObject player = new ClientGameObject(playerID);
				GameData.Data.Player = player;
				MainWindow.s_mainWindow.map.FollowObject = player;

				MapLevel level = new MapLevel(1000, 1000); // xxx
				MainWindow.s_mainWindow.Map = level;
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Uncaught exception");
				MyDebug.WriteLine(e.ToString());
			}
		}

		public void MapChanged(Location l, int[] items)
		{
			try
			{
				MainWindow.s_mainWindow.Map.SetContents(l, items);
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Uncaught exception");
				MyDebug.WriteLine(e.ToString());
			}
		}

		public void DeliverMapTerrains(MapLocation[] locations)
		{
			try
			{
				MainWindow.s_mainWindow.Map.SetTerrains(locations);
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Uncaught exception");
				MyDebug.WriteLine(e.ToString());
			}
		}

		public void PlayerMoved(Location l)
		{
		}

		public void DeliverChanges(Change[] changes)
		{
			try
			{
				foreach (Change change in changes)
				{
					MyDebug.WriteLine("DeliverChanges: {0}", change);

					if (change is ObjectChange)
					{
						ObjectChange oc = (ObjectChange)change;
						ClientGameObject ob = ClientGameObject.FindObject(oc.ObjectID);

						if (ob == null)
						{
							MyDebug.WriteLine("New object appeared");
							ob = new ClientGameObject(oc.ObjectID);
						}

						if (change is LocationChange)
						{
							LocationChange lc = (LocationChange)change;
							// we should only get changes about events on this level
							// so if an ob doesn't have an env, it must be here
							if (ob.Environment == null)
								ob.SetEnvironment(MainWindow.s_mainWindow.Map, lc.TargetLocation);
							else
								ob.Location = lc.TargetLocation;
						}
						else
							throw new NotImplementedException();
					}
					else if (change is TurnChange)
					{
						TurnChange tc = (TurnChange)change;
					}
					else
						throw new NotImplementedException();
				}
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Uncaught exception");
				MyDebug.WriteLine(e.ToString());
			}
		}

		#endregion
	}
}
