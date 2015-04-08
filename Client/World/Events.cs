using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	public sealed class ClientEvent
	{
		public string Message { get; private set; }
		public EnvironmentObject Environment { get; private set; }
		public IntVector3 Location { get; private set; }

		public ClientEvent(string str)
		{
			this.Message = str;
		}

		public ClientEvent(string str, EnvironmentObject env, IntVector3 location)
		{
			this.Message = str;
			this.Environment = env;
			this.Location = location;
		}
	}

	public static class Events
	{
		static ObservableCollection<ClientEvent> s_events;
		public static ReadOnlyObservableCollection<ClientEvent> EventsCollection { get; private set; }
		static bool s_previousWasTickEvent = true;

		static Events()
		{
			s_events = new ObservableCollection<ClientEvent>();
			EventsCollection = new ReadOnlyObservableCollection<ClientEvent>(s_events);
		}

		public static void AddGameEvent(EnvironmentObject env, IntVector3 location, string format, params object[] args)
		{
			AddGameEventInternal(env, location, String.Format(format, args));
			s_previousWasTickEvent = false;
		}

		public static void AddGameEvent(EnvironmentObject env, IntVector3 location, string message)
		{
			AddGameEventInternal(env, location, message);
			s_previousWasTickEvent = false;
		}

		public static void AddGameEvent(MovableObject ob, string format, params object[] args)
		{
			AddGameEvent(ob.Environment, ob.Location, String.Format(format, args));
		}

		public static void AddGameEvent(MovableObject ob, string message)
		{
			AddGameEvent(ob.Environment, ob.Location, message);
		}

		public static void AddTickGameEvent()
		{
			if (s_previousWasTickEvent)
				return;

			AddGameEventInternal(null, new IntVector3(), "---");

			s_previousWasTickEvent = true;
		}

		public static void AddInformative(string message, params object[] args)
		{
			AddGameEventInternal(null, new IntVector3(), String.Format(message, args));
			s_previousWasTickEvent = false;
		}

		static void AddGameEventInternal(EnvironmentObject env, IntVector3 location, string message)
		{
			if (s_events.Count > 100)
				s_events.RemoveAt(0);

			//Trace.TraceInformation(message);

			s_events.Add(new ClientEvent(message, env, location));
		}
	}
}
