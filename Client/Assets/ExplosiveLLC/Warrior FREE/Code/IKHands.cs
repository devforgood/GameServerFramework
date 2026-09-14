using System.Collections;
using UnityEngine;

namespace WarriorAnimsFREE
{
	public class IKHands:MonoBehaviour
	{
		private WarriorController warriorController;
		private Animator animator;
		public Transform leftHandObj;
		public Transform attachLeft;
		public bool canBeUsed;
		[Range(0, 1)] public float leftHandPositionWeight;
		[Range(0, 1)] public float leftHandRotationWeight;

		private void Awake()
		{
			animator = GetComponent<Animator>();
			warriorController = GetComponentInParent<WarriorController>();
		}

		/// <summary>
		/// If there is movement and/or rotation data in the animation for the Left Hand, use IK to 
		/// set the position of the Left Hand of the character.
		/// </summary>
		private void OnAnimatorIK(int layerIndex)
		{
			if (canBeUsed) {
				if (leftHandObj != null) {
					animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandPositionWeight);
					animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftHandRotationWeight);
					if (attachLeft != null) {
						animator.SetIKPosition(AvatarIKGoal.LeftHand, attachLeft.position);
						animator.SetIKRotation(AvatarIKGoal.LeftHand, attachLeft.rotation);
					}
				}
			}
		}

		/// <summary>
		/// Smoothly blend IK on and off so there's no snapping into position.
		/// </summary>
		public void BlendIK(bool blendOn, float delay, float timeToBlend)
		{
			StopAllCoroutines();
			Coroutine co;
			co = StartCoroutine(_BlendIK(blendOn, delay, timeToBlend));
		}

		private IEnumerator _BlendIK(bool blendOn, float delay, float timeToBlend)
		{
			if (canBeUsed) {
				yield return new WaitForSeconds(delay);
				float t = 0f;
				float blendTo = 0;
				float blendFrom = 0;
				if (blendOn) { blendTo = 1; } 
				else { blendFrom = 1; }
				while (t < 1) {
					t += Time.deltaTime / timeToBlend;
					leftHandPositionWeight = Mathf.Lerp(blendFrom, blendTo, t);
					leftHandRotationWeight = Mathf.Lerp(blendFrom, blendTo, t);
					yield return null;
				}
				yield break;
			}
		}

		/// <summary>
		/// Pauses IK while Warrior uses Left Hand during an animation.
		/// </summary>
		public void SetIKPause(float pauseTime)
		{
			StopAllCoroutines();
			Coroutine co;
			co = StartCoroutine(_SetIKPause(pauseTime));
		}

		private IEnumerator _SetIKPause(float pauseTime)
		{
			if (canBeUsed) {
				float t = 0f;
				while (t < 1) {
					t += Time.deltaTime / 0.1f;
					leftHandPositionWeight = Mathf.Lerp(1, 0, t);
					leftHandRotationWeight = Mathf.Lerp(1, 0, t);
					yield return null;
				}
				yield return new WaitForSeconds(pauseTime - 0.2f);
				t = 0f;
				while (t < 1) {
					t += Time.deltaTime / 0.1f;
					leftHandPositionWeight = Mathf.Lerp(0, 1, t);
					leftHandRotationWeight = Mathf.Lerp(0, 1, t);
					yield return null;
				}
			}
		}

		public void SetIKOff()
		{
			if (canBeUsed) {
				StopAllCoroutines();
				leftHandPositionWeight = 0;
				leftHandRotationWeight = 0;
			}
		}

		public void SetIKOn()
		{
			if (canBeUsed) {
				StopAllCoroutines();
				leftHandPositionWeight = 1;
				leftHandRotationWeight = 1;
			}
		}
	}
}