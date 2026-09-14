using UnityEngine;

namespace WarriorAnimsFREE
{
	public class WarriorInputController:MonoBehaviour
	{
		[HideInInspector] public bool inputAttack;
		[HideInInspector] public bool inputJump;
		[HideInInspector] public float inputHorizontal = 0;
		[HideInInspector] public float inputVertical = 0;

		public Vector3 moveInput { get { return CameraRelativeInput(inputHorizontal, inputVertical); } }

		private void Update()
		{
			Inputs();
			Toggles();
		}

		/// <summary>
		/// Input abstraction for easier asset updates using outside control schemes.
		/// </summary>
		private void Inputs()
		{
			inputAttack = Input.GetButtonDown("Attack");
			inputJump = Input.GetButtonDown("Jump");
			inputHorizontal = Input.GetAxisRaw("Horizontal");
			inputVertical = Input.GetAxisRaw("Vertical");
		}

		private void Toggles()
		{
			// Slow time toggle.
			if (Input.GetKeyDown(KeyCode.T)) {
				if (Time.timeScale != 1) { Time.timeScale = 1; } 
				else { Time.timeScale = 0.125f; }
			}
			// Pause toggle.
			if (Input.GetKeyDown(KeyCode.P)) {
				if (Time.timeScale != 1) { Time.timeScale = 1; } 
				else { Time.timeScale = 0f; }
			}
		}

		/// <summary>
		/// Movement based off camera facing.
		/// </summary>
		private Vector3 CameraRelativeInput(float inputX, float inputZ)
		{
			// Forward vector relative to the camera along the x-z plane.  
			Vector3 forward = Camera.main.transform.TransformDirection(Vector3.forward);
			forward.y = 0;
			forward = forward.normalized;

			// Right vector relative to the camera always orthogonal to the forward vector.
			Vector3 right = new Vector3(forward.z, 0, -forward.x);
			Vector3 relativeVelocity = inputHorizontal * right + inputVertical * forward;

			// Reduce input for diagonal movement.
			if (relativeVelocity.magnitude > 1) { relativeVelocity.Normalize(); }

			return relativeVelocity;
		}
	}
}
