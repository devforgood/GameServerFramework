using UnityEngine;
using UnityEngine.Events;

namespace WarriorAnimsFREE
{
    public class WarriorCharacterAnimatorEvents:MonoBehaviour
    {
		/// <summary>
		/// Placeholder functions for Animation events.
		/// </summary>
		public UnityEvent OnHit = new UnityEvent();
		public UnityEvent OnFootR = new UnityEvent();
		public UnityEvent OnFootL = new UnityEvent();
		public UnityEvent OnLand = new UnityEvent();
		public UnityEvent OnShoot = new UnityEvent();
		public UnityEvent OnWeaponSwitch = new UnityEvent();

		[HideInInspector] public WarriorController warriorController;

		public void Hit()
		{
			OnHit.Invoke();
		}

		public void FootR()
		{
			OnFootR.Invoke();
		}

		public void FootL()
		{
			OnFootL.Invoke();
		}

		public void Land()
		{
			OnLand.Invoke();
		}

		public void Shoot()
		{
			OnShoot.Invoke();
		}
	}
}