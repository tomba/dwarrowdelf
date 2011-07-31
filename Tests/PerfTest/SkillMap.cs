using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace PerfTest
{
	class SkillMapTestSuite : TestSuite
	{
		public override void DoTests()
		{
			RunTest(new ByteByteSkillMap());
			RunTest(new ByteIntSkillMap());
			RunTest(new IntByteSkillMap());
			RunTest(new IntIntSkillMap());
		}

		class ByteByteSkillMap : ITest
		{
			public enum SkillID : byte
			{
				None,

				Mining,
				WoodCutting,
				Carpentry,
				Masonry,

				Fighting,
			}

			public Dictionary<SkillID, byte> m_skillMap = new Dictionary<SkillID, byte>();

			public void DoTest(int loops)
			{
				for (int i = 0; i < loops; ++i)
				{
					Test1(SkillID.Carpentry);
					Test1(SkillID.Fighting);
					Test1(SkillID.Masonry);
					Test2(SkillID.Carpentry);
					Test2(SkillID.Fighting);
					Test2(SkillID.Masonry);
				}
			}

			public byte Test1(SkillID skillID)
			{
				byte skillLevel;
				skillLevel = GetSkillLevel(skillID);
				SetSkillLevel(skillID, 50);
				skillLevel = GetSkillLevel(skillID);
				return skillLevel;
			}

			public byte Test2(SkillID skillID)
			{
				byte skillLevel;
				skillLevel = GetSkillLevel(skillID);
				SetSkillLevel(skillID, 0);
				skillLevel = GetSkillLevel(skillID);
				return skillLevel;
			}

			public byte GetSkillLevel(SkillID skill)
			{
				byte skillValue;
				if (m_skillMap.TryGetValue(skill, out skillValue))
					return skillValue;
				return 0;
			}

			public void SetSkillLevel(SkillID skill, byte level)
			{
				byte oldLevel = GetSkillLevel(skill);

				if (level == 0)
					m_skillMap.Remove(skill);
				else
					m_skillMap[skill] = level;
			}
		}

		class ByteIntSkillMap : ITest
		{
			public enum SkillID : byte
			{
				None,

				Mining,
				WoodCutting,
				Carpentry,
				Masonry,

				Fighting,
			}

			public Dictionary<SkillID, int> m_skillMap = new Dictionary<SkillID, int>();

			public void DoTest(int loops)
			{
				for (int i = 0; i < loops; ++i)
				{
					Test1(SkillID.Carpentry);
					Test1(SkillID.Fighting);
					Test1(SkillID.Masonry);
					Test2(SkillID.Carpentry);
					Test2(SkillID.Fighting);
					Test2(SkillID.Masonry);
				}
			}

			public int Test1(SkillID skillID)
			{
				int skillLevel;
				skillLevel = GetSkillLevel(skillID);
				SetSkillLevel(skillID, 50);
				skillLevel = GetSkillLevel(skillID);
				return skillLevel;
			}

			public int Test2(SkillID skillID)
			{
				int skillLevel;
				skillLevel = GetSkillLevel(skillID);
				SetSkillLevel(skillID, 0);
				skillLevel = GetSkillLevel(skillID);
				return skillLevel;
			}

			public int GetSkillLevel(SkillID skill)
			{
				int skillValue;
				if (m_skillMap.TryGetValue(skill, out skillValue))
					return skillValue;
				return 0;
			}

			public void SetSkillLevel(SkillID skill, int level)
			{
				int oldLevel = GetSkillLevel(skill);

				if (level == 0)
					m_skillMap.Remove(skill);
				else
					m_skillMap[skill] = level;
			}
		}

		class IntByteSkillMap : ITest
		{
			public enum SkillID
			{
				None,

				Mining,
				WoodCutting,
				Carpentry,
				Masonry,

				Fighting,
			}

			public Dictionary<SkillID, byte> m_skillMap = new Dictionary<SkillID, byte>();

			public void DoTest(int loops)
			{
				for (int i = 0; i < loops; ++i)
				{
					Test1(SkillID.Carpentry);
					Test1(SkillID.Fighting);
					Test1(SkillID.Masonry);
					Test2(SkillID.Carpentry);
					Test2(SkillID.Fighting);
					Test2(SkillID.Masonry);
				}
			}

			public byte Test1(SkillID skillID)
			{
				byte skillLevel;
				skillLevel = GetSkillLevel(skillID);
				SetSkillLevel(skillID, 50);
				skillLevel = GetSkillLevel(skillID);
				return skillLevel;
			}

			public byte Test2(SkillID skillID)
			{
				byte skillLevel;
				skillLevel = GetSkillLevel(skillID);
				SetSkillLevel(skillID, 0);
				skillLevel = GetSkillLevel(skillID);
				return skillLevel;
			}

			public byte GetSkillLevel(SkillID skill)
			{
				byte skillValue;
				if (m_skillMap.TryGetValue(skill, out skillValue))
					return skillValue;
				return 0;
			}

			public void SetSkillLevel(SkillID skill, byte level)
			{
				byte oldLevel = GetSkillLevel(skill);

				if (level == 0)
					m_skillMap.Remove(skill);
				else
					m_skillMap[skill] = level;
			}
		}


		class IntIntSkillMap : ITest
		{
			public enum SkillID
			{
				None,

				Mining,
				WoodCutting,
				Carpentry,
				Masonry,

				Fighting,
			}

			public Dictionary<SkillID, int> m_skillMap = new Dictionary<SkillID, int>();

			public void DoTest(int loops)
			{
				for (int i = 0; i < loops; ++i)
				{
					Test1(SkillID.Carpentry);
					Test1(SkillID.Fighting);
					Test1(SkillID.Masonry);
					Test2(SkillID.Carpentry);
					Test2(SkillID.Fighting);
					Test2(SkillID.Masonry);
				}
			}

			public int Test1(SkillID skillID)
			{
				int skillLevel;
				skillLevel = GetSkillLevel(skillID);
				SetSkillLevel(skillID, 50);
				skillLevel = GetSkillLevel(skillID);
				return skillLevel;
			}

			public int Test2(SkillID skillID)
			{
				int skillLevel;
				skillLevel = GetSkillLevel(skillID);
				SetSkillLevel(skillID, 0);
				skillLevel = GetSkillLevel(skillID);
				return skillLevel;
			}

			public int GetSkillLevel(SkillID skill)
			{
				int skillValue;
				if (m_skillMap.TryGetValue(skill, out skillValue))
					return skillValue;
				return 0;
			}

			public void SetSkillLevel(SkillID skill, int level)
			{
				int oldLevel = GetSkillLevel(skill);

				if (level == 0)
					m_skillMap.Remove(skill);
				else
					m_skillMap[skill] = level;
			}
		}
	}
}

