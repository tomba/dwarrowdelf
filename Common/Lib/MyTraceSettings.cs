using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
	public class MyTraceSettings : ConfigurationSection
	{
		static MyTraceSettings s_settings = ConfigurationManager.GetSection("myTraceSettings") as MyTraceSettings;

		public static MyTraceSettings Settings { get { return s_settings; } }

		[ConfigurationProperty("defaultTraceLevels", IsDefaultCollection = true)]
		public DefaultTraceLevelCollection DefaultTraceLevels
		{
			get { return (DefaultTraceLevelCollection)this["defaultTraceLevels"]; }
		}
	}

	[ConfigurationCollection(typeof(DefaultTraceLevelElement))]
	public class DefaultTraceLevelCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new DefaultTraceLevelElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((DefaultTraceLevelElement)(element)).Name;
		}

		public DefaultTraceLevelElement this[int idx]
		{
			get { return (DefaultTraceLevelElement)BaseGet(idx); }
		}

		public new DefaultTraceLevelElement this[string name]
		{
			get { return (DefaultTraceLevelElement)BaseGet(name); }
		}
	}

	public class DefaultTraceLevelElement : ConfigurationElement
	{
		[ConfigurationProperty("name", DefaultValue = "", IsKey = true, IsRequired = true)]
		public string Name
		{
			get { return ((string)(base["name"])); }
			set { base["name"] = value; }
		}

		[ConfigurationProperty("level", DefaultValue = TraceLevel.Off, IsKey = false, IsRequired = true)]
		public TraceLevel Level
		{
			get { return ((TraceLevel)(base["level"])); }
			set { base["level"] = value; }
		}
	}
}
