using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		int GetTotalTicks(AttackAction action)
		{
			return 1;
		}

		Random m_random = new Random();

		bool PerformAction(AttackAction action)
		{
			var attacker = this;
			var target = this.World.FindObject<LivingObject>(action.Target);

			if (target == null)
			{
				SendFailReport(new AttackActionReport(this, null), "target doesn't exist");
				return false;
			}

			if (!attacker.Location.IsAdjacentTo(target.Location, DirectionSet.Planar))
			{
				SendFailReport(new AttackActionReport(this, target), "target isn't near");
				return false;
			}

			var roll = m_random.Next(20) + 1;
			bool hit;

			var str = attacker.Strength;
			str = (int)((20.0 / 100) * str);
			var strBonus = (str / 2) - 5;
			if (strBonus < 0)
				strBonus = 0;

			if (roll == 1)
			{
				hit = false;
			}
			else if (roll == 20)
			{
				hit = true;
			}
			else
			{
				var dex = target.Dexterity;
				dex = (int)((20.0 / 100) * dex);
				var dexBonus = (dex / 2) - 5;
				if (dexBonus < 0)
					dexBonus = 0;

				var ac = 10 + target.ArmorClass + dexBonus;

				hit = roll + strBonus >= ac;

				Trace.TraceInformation("{0} attacks {1}: {2} + {3} >= 10 + {4} + {5} == {6} >= {7}",
					attacker, target,
					roll, strBonus,
					target.ArmorClass, dexBonus,
					roll + strBonus, ac);
			}

			int damage;
			DamageCategory damageCategory;

			if (hit)
			{
				var weapon = attacker.Weapon;
				int dieSides;

				if (weapon == null)
					dieSides = 3;
				else
					dieSides = weapon.WeaponInfo.WC;

				damage = m_random.Next(dieSides) + 1 + strBonus;
				damageCategory = DamageCategory.Melee;
				Trace.TraceInformation("{0} hits {1}, {2} damage", attacker, target, damage);
			}
			else
			{
				damage = 0;
				damageCategory = DamageCategory.None;
				Trace.TraceInformation("{0} misses {1}", attacker, target);
			}

			SendReport(new AttackActionReport(this, target) { IsHit = hit, Damage = damage, DamageCategory = damageCategory });

			if (hit)
				target.ReceiveDamage(attacker, damageCategory, damage);

			return true;
		}

	}
}
