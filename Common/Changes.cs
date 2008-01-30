using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract, KnownType(typeof(LocationChange)), KnownType(typeof(EnvironmentChange))]
	public abstract class Change
	{
		GameObject m_target;
		[DataMember]
		public ObjectID ObjectID { get; set; }

		public Change(GameObject target)
		{
			m_target = target;
			this.ObjectID = m_target.ObjectID;
		}
	}

	[DataContract]
	public class LocationChange : Change
	{
		[DataMember]
		public Location Location { get; set; }

		public LocationChange(GameObject target, Location location)
			: base(target)
		{
			this.Location = location;
		}
	}

	[DataContract]
	public class EnvironmentChange : Change
	{
		[DataMember]
		public Location Location { get; set; }
		[DataMember]
		public ObjectID MapID { get; set; }

		public EnvironmentChange(GameObject target, ObjectID mapID, Location location)
			: base(target)
		{
			this.Location = location;
			this.MapID = mapID;
		}
	}
}
