using UnityEngine;

namespace WarriorAnimsFREE
{
	public class GUIControls:MonoBehaviour
	{
		private WarriorController warriorController;

		private void Awake()
		{
			warriorController = GetComponent<WarriorController>();
		}

		private void OnGUI()
		{
			if (warriorController.canAction) {
				Attacking();
				Jumping();
			}

			Debug();
		}

		private void Attacking()
		{
			if (warriorController.MaintainingGround() && warriorController.canAction) {
					if (GUI.Button(new Rect(25, 85, 100, 30), "Attack1")) { warriorController.Attack1(); }
			}
		}

		private void Jumping()
		{
			if (warriorController.canJump
				&& warriorController.canAction) {
				if (warriorController.MaintainingGround()) {
					if (GUI.Button(new Rect(25, 175, 100, 30), "Jump")) {
						if (warriorController.canJump) { warriorController.inputJump = true; ; }
					}
				}
			}
		}

		private void Debug()
		{
			if (GUI.Button(new Rect(600, 15, 120, 30), "Debug Controller")) { warriorController.ControllerDebug(); }
			if (GUI.Button(new Rect(600, 50, 120, 30), "Debug Animator")) { warriorController.AnimatorDebug(); }
		}
	}
}