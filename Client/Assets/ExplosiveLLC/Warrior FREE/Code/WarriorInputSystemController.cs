using UnityEngine;

namespace WarriorAnimsFREE
{
	/// <summary>
	/// Placeholder script.  Extract the actual script from the "InputSystem Support - Requires InputSystem Package".
	/// </summary>
	public class WarriorInputSystemController:MonoBehaviour
	{
		// Placeholder inputs.
		[HideInInspector] public bool inputAttack;
		[HideInInspector] public bool inputJump;
		[HideInInspector] public float inputHorizontal = 0;
		[HideInInspector] public float inputVertical = 0;

		[HideInInspector] public Vector3 moveInput;
	}
}
