﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	static class Events
	{
		static ObservableCollection<GameEvent> s_events;
		public static ReadOnlyObservableCollection<GameEvent> EventsCollection { get; private set; }
		static bool s_previousWasTickEvent = true;

		static Events()
		{
			s_events = new ObservableCollection<GameEvent>();
			EventsCollection = new ReadOnlyObservableCollection<GameEvent>(s_events);
		}

		public static void AddGameEvent(EnvironmentObject env, IntPoint3 location, string format, params object[] args)
		{
			AddGameEventInternal(env, location, String.Format(format, args));
			s_previousWasTickEvent = false;
		}

		public static void AddGameEvent(EnvironmentObject env, IntPoint3 location, string message)
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

			AddGameEventInternal(null, new IntPoint3(), "---");

			s_previousWasTickEvent = true;
		}

		static void AddGameEventInternal(EnvironmentObject env, IntPoint3 location, string message)
		{
			if (s_events.Count > 100)
				s_events.RemoveAt(0);

			//Trace.TraceInformation(message);

			s_events.Add(new GameEvent(message, env, location));
		}
	}
}
