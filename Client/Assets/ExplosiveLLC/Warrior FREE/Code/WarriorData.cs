namespace WarriorAnimsFREE
{
	/// <summary>
	/// The type of Warrior.  Determines which animations can play, and the 
	/// timings for those animations in WarriorTiming.cs.
	/// </summary>
	public enum Warrior
	{
		Archer,
		Brute,
		Crossbow,
		Hammer,
		Karate,
		Knight,
		Mage,
		Ninja,
		Sorceress,
		Spearman,
		Swordsman,
		TwoHanded
	}

	/// <summary>
	/// The different movement / situational states the Warrior can be in.
	/// </summary>
	public enum WarriorState
	{
		Idle = 0,
		Move = 1,
		Jump = 2,
		Fall = 3
	}

	/// <summary>
	/// Enum to use with the "TriggerNumber" parameter of the animator. Convert to (int) to set.
	/// </summary>
	public enum AnimatorTrigger
	{
		JumpTrigger = 1,
		AttackTrigger = 2
	}
}