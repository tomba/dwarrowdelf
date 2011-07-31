using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs
{
	public class HerbivoreHerd
	{
		List<HerbivoreAI> m_members = new List<HerbivoreAI>();

		public HerbivoreHerd()
		{

		}

		public void AddMember(HerbivoreAI ai)
		{
			Debug.Assert(ai.Herd == null);

			ai.Herd = this;
			m_members.Add(ai);
			ai.Worker.Destructed += OnDestructed;
		}

		public void RemoveMember(HerbivoreAI ai)
		{
			Debug.Assert(ai.Herd == this);

			m_members.Remove(ai);
		}

		void OnDestructed(IBaseGameObject ob)
		{
			var a = m_members.Single(ai => ai.Worker == ob);

			RemoveMember(a);
		}

		public int HerdSize
		{
			get { return m_members.Count; }
		}

		public IntPoint3D GetCenter()
		{
			var locations = m_members.Select(ai => ai.Worker.Location);

			return IntPoint3D.Center(locations);
		}
	}
}
