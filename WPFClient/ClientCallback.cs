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
			ClientGameObject player = new ClientGameObject(playerID);
			GameData.Data.Player = player;
			MainWindow.s_mainWindow.map.FollowObject = player;

			MapLevel level = new MapLevel(1000, 1000); // xxx
			MainWindow.s_mainWindow.Map = level;
		}

		public void MapChanged(Location l, int[] items)
		{
			MainWindow.s_mainWindow.Map.SetContents(l, items);
		}

		public void DeliverMapTerrains(MapLocation[] locations)
		{
			MainWindow.s_mainWindow.Map.SetTerrains(locations);
		}

		public void PlayerMoved(Location l)
		{
		}

		public void DeliverChanges(Change[] changes)
		{
			foreach (Change change in changes)
			{
				//MyDebug.WriteLine("DeliverChanges: {0}", change);

				ClientGameObject ob = ClientGameObject.FindObject(change.ObjectID);

				if (ob == null)
				{
					MyDebug.WriteLine("New object appeared");
					ob = new ClientGameObject(change.ObjectID);
				}

				if (change is LocationChange)
				{
					LocationChange lc = (LocationChange)change;
					ob.Location = lc.Location;
				}
				else if (change is EnvironmentChange)
				{
					EnvironmentChange ec = (EnvironmentChange)change;
					ob.SetEnvironment(MainWindow.s_mainWindow.Map, ec.Location);
				}
			}
		}

		#endregion
	}
}
