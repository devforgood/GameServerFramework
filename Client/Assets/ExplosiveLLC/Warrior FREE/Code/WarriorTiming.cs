using UnityEngine;

namespace WarriorAnimsFREE
{
	public class WarriorTiming:MonoBehaviour
    {
		[HideInInspector] public WarriorController warriorController;

		/// <summary>
		/// Lock timing for all the Warrior attacks and actions.
		/// </summary>
		public float TimingLock(Warrior warrior, string action)
        {
			float timing = 0f;
			if(warrior == Warrior.Archer)
			{
				if(action == "attack1") timing = 0.7f;
			}
			else if(warrior == Warrior.Brute)
			{
				if(action == "attack1") timing = 1f;
			}
			else if(warrior == Warrior.Crossbow)
			{
				if(action == "attack1") timing = 0.8f;
			}
			else if(warrior == Warrior.Hammer)
			{
				if(action == "attack1") timing = 1.25f;
			}
			else if(warrior == Warrior.Karate)
			{
				if(action == "attack1") timing = 0.7f;
			}
			else if(warrior == Warrior.Knight)
			{
				if(action == "attack1") timing = 0.6f;
			}
			else if(warrior == Warrior.Mage)
			{
				if(action == "attack1") timing = 1.1f;
			}
			else if(warrior == Warrior.Ninja)
			{
				if(action == "attack1") timing = 0.6f;
			}
			else if(warrior == Warrior.Sorceress)
			{
				if(action == "attack1") timing = 1.2f;
			}
			else if(warrior == Warrior.Spearman)
			{
				if(action == "attack1") timing = 1f;
			}
			else if(warrior == Warrior.Swordsman)
			{
				if(action == "attack1") timing = 0.9f;
			}
			else if(warrior == Warrior.TwoHanded)
			{
				if(action == "attack1") timing = 0.9f;
			}
			return timing;
        }
	}
}