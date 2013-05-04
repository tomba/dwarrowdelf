using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
	[Serializable]
	public class EnumBitMask<TEnum>
	{
		static int s_numBits;
		BitArray m_mask;

		static EnumBitMask()
		{
			s_numBits = EnumHelpers.GetEnumMax<TEnum>();
		}

		public EnumBitMask()
		{
			m_mask = null;
		}

		public EnumBitMask(TEnum enumValue)
		{
			BitArray mask = null;

			if (EnumConv.ToInt32(enumValue) != 0)
			{
				mask = new BitArray(s_numBits);
				mask.Set(EnumConv.ToInt32(enumValue) - 1, true);
			}

			m_mask = mask;
		}

		public EnumBitMask(IEnumerable<TEnum> enumValues)
		{
			BitArray mask = null;

			if (enumValues.Any())
			{
				mask = new BitArray(s_numBits);
				foreach (TEnum e in enumValues)
					mask.Set(EnumConv.ToInt32(e) - 1, true);
			}

			m_mask = mask;
		}

		public bool Get(TEnum enumValue)
		{
			return m_mask != null && m_mask.Get(EnumConv.ToInt32(enumValue) - 1);
		}

		public IEnumerable<TEnum> EnumValues
		{
			get
			{
				if (m_mask == null)
					yield break;

				for (int i = 0; i < s_numBits; ++i)
				{
					if (m_mask.Get(i))
						yield return EnumConv.ToEnum<TEnum>(i + 1);
				}
			}
		}

		public override string ToString()
		{
			if (m_mask == null)
				return "null";

			var sb = new StringBuilder("0b", s_numBits + 2);

			bool zeroes = true;

			for (int i = s_numBits - 1; i >= 0; --i)
			{
				bool b = m_mask.Get(i);

				if (zeroes)
				{
					if (!b)
						continue;

					zeroes = false;
				}

				sb.Append(b ? '1' : '0');
			}

			return sb.ToString();
		}
	}
}
