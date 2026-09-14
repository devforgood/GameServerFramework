using System.Collections;
using UnityEngine;

namespace WarriorAnimsFREE
{
	public class WarriorController:SuperStateMachine
	{
		[Header("Components")]
		public Warrior warrior;
		public GameObject target;
		public GameObject weapon;
		private Rigidbody rb;
		[HideInInspector] public SuperCharacterController superCharacterController;
		[HideInInspector] public WarriorMovementController warriorMovementController;
		[HideInInspector] public WarriorInputController warriorInputController;
		[HideInInspector] public WarriorInputSystemController warriorInputSystemController;
		[HideInInspector] public WarriorTiming warriorTiming;
		[HideInInspector] public Animator animator;
		[HideInInspector] public IKHands ikHands;

		// Inputs.
		[HideInInspector] public bool inputAttack;
		[HideInInspector] public bool inputJump;
		[HideInInspector] public float inputVertical = 0;
		[HideInInspector] public float inputHorizontal = 0;

		[HideInInspector] public Vector3 moveInput;

		private bool useInputSystem;

		public bool allowedInput { get { return _allowedInput; } }
		private bool _allowedInput = true;

		// Actions.
		[HideInInspector] public bool isMoving;
		[HideInInspector] public bool useRootMotion = false;

		public bool canAction { get { return _canAction; } }
		private bool _canAction = true;

		public bool canMove { get { return _canMove; } }
		private bool _canMove = true;

		public bool canJump { get { return _canJump; } }
		private bool _canJump = true;

		// Animation speed control. (doesn't affect lock timing)
		public float animationSpeed = 1;

		#region Initialization

		private void Awake()
		{
			// Get SuperCharacterController.
			superCharacterController = GetComponent<SuperCharacterController>();

			// Get Movement Controller.
			warriorMovementController = GetComponent<WarriorMovementController>();

			// Add Timing Controllers.
			warriorTiming = gameObject.AddComponent<WarriorTiming>();
			warriorTiming.warriorController = this;

			// Add IKHands.
			ikHands = GetComponentInChildren<IKHands>();
			if (ikHands != null) {
				if (warrior == Warrior.TwoHanded
					|| warrior == Warrior.Hammer
					|| warrior == Warrior.Crossbow
					|| warrior == Warrior.Spearman) {
					ikHands.canBeUsed = true;
					ikHands.BlendIK(true, 0, 0.25f);
				}
			}

			// Setup Animator, add AnimationEvents script.
			animator = GetComponentInChildren<Animator>();
			if (animator == null) {
				Debug.LogError("ERROR: There is no Animator component for character.");
				Debug.Break();
			} else {
				animator.gameObject.AddComponent<WarriorCharacterAnimatorEvents>();
				animator.GetComponent<WarriorCharacterAnimatorEvents>().warriorController = this;
				animator.gameObject.AddComponent<AnimatorParentMove>();
				animator.GetComponent<AnimatorParentMove>().animator = animator;
				animator.GetComponent<AnimatorParentMove>().warriorController = this;
				animator.updateMode = AnimatorUpdateMode.Fixed;   // AnimatePhysics 는 Unity 6 에서 제거됨
				animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
			}

			// Determine input source.
			warriorInputController = GetComponent<WarriorInputController>();
			if (warriorInputController != null) {
				useInputSystem = false;
			} else {
				warriorInputSystemController = GetComponent<WarriorInputSystemController>();
				if (warriorInputSystemController != null) { useInputSystem = true; } else { Debug.LogError("No inputs!"); }
			}

			// Setup Rigidbody.
			rb = GetComponent<Rigidbody>();
			if (rb != null) { rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; }

			currentState = WarriorState.Idle;
		}

		#endregion

		#region Input

		/// <summary>
		/// Takes input from either WarriorInputController or WarriorInputSystemController.
		/// </summary>
		private void GetInput()
		{
			if (allowedInput) {

				// Use input from WarriorInputController / Input Manager.
				if (!useInputSystem) {
					if (warriorInputController != null) {
						inputAttack = warriorInputController.inputAttack;
						inputJump = warriorInputController.inputJump;
						moveInput = warriorInputController.moveInput;
					}

					// Use input from WarriorInputSystemController / Warrior Input Actions.
				} else {
					if (warriorInputSystemController != null) {
						inputAttack = warriorInputSystemController.inputAttack;
						inputJump = warriorInputSystemController.inputJump;
						moveInput = warriorInputSystemController.moveInput;
					}
				}
			}
		}

		/// <summary>
		/// Checks move input and returns if active.
		/// </summary>
		public bool HasMoveInput()
		{
			return moveInput != Vector3.zero;
		}

		/// <summary>
		/// Shuts off input from WarriorInputController or WarriorInputSystemController. GUI still enabled.
		/// </summary>
		public void AllowInput(bool b)
		{
			_allowedInput = b;
		}

		#endregion

		#region Updates

		private void Update()
		{
			GetInput();

			// Character is on ground.
			if (MaintainingGround() && canAction) { Attacking(); }

			UpdateAnimationSpeed();
		}

		/// <summary>
		/// Updates the Animator with the animation speed multiplier.
		/// </summary>
		private void UpdateAnimationSpeed()
		{
			SetAnimatorFloat("Animation Speed", animationSpeed);
		}

		#endregion

		#region Combat

		/// <summary>
		/// Warrior jumps.
		/// </summary>
		public void Jump()
		{
			if (warrior == Warrior.Crossbow) {
				ikHands.SetIKOff();
			}
		}

		/// <summary>
		/// Warrior lands.
		/// </summary>
		public void Land()
		{
			if (warrior == Warrior.Crossbow) {
				ikHands.BlendIK(true, 0.5f, 0.25f);
			}
		}

		/// <summary>
		/// The different attack types.
		/// </summary>
		private void Attacking()
		{
			if (inputAttack) { Attack1(); }
		}

		/// <summary>
		/// First attack in the AttackChain.
		/// </summary>
		public void Attack1()
		{
			SetAnimatorInt("Action", 1);
			SetAnimatorTrigger(AnimatorTrigger.AttackTrigger);
			Lock(true, true, true, 0, warriorTiming.TimingLock(warrior, "attack1"));
		}

		#endregion

		#region Locks

		/// <summary>
		/// Lock character movement and/or action, on a delay for a set time.
		/// </summary>
		/// <param name="lockMovement">If set to <c>true</c> lock movement.</param>
		/// <param name="lockAction">If set to <c>true</c> lock action.</param>
		/// <param name="timed">If set to <c>true</c> timed.</param>
		/// <param name="delayTime">Delay time.</param>
		/// <param name="lockTime">Lock time.</param>
		public void Lock(bool lockMovement, bool lockAction, bool timed, float delayTime, float lockTime)
		{
			StopCoroutine("_Lock");
			StartCoroutine(_Lock(lockMovement, lockAction, timed, delayTime, lockTime));
		}

		//Timed -1 = infinite, 0 = no, 1 = yes.
		public IEnumerator _Lock(bool lockMovement, bool lockAction, bool timed, float delayTime, float lockTime)
		{
			if (delayTime > 0) { yield return new WaitForSeconds(delayTime); }
			if (lockMovement) { LockMove(true); }
			if (lockAction) { LockAction(true); }
			if (timed) {
				if (lockTime > 0) {
					yield return new WaitForSeconds(lockTime);
					UnLock(lockMovement, lockAction);
				}
			}
		}

		/// <summary>
		/// Keep character from moving and use or diable Rootmotion.
		/// </summary>
		public void LockMove(bool b)
		{
			if (b) {
				SetAnimatorBool("Moving", false);
				SetAnimatorRootMotion(true);
				_canMove = false;
				moveInput = Vector3.zero;
			} else {
				_canMove = true;
				SetAnimatorRootMotion(false);
			}
		}

		/// <summary>
		/// Keep character from doing actions.
		/// </summary>
		public void LockAction(bool b)
		{
			_canAction = !b;
		}

		/// <summary>
		/// Keep character from jumping.
		/// </summary>
		public void LockJump(bool b)
		{
			_canJump = !b;
		}

		/// <summary>
		/// Let character move and act again.
		/// </summary>
		private void UnLock(bool movement, bool actions)
		{
			if (movement) { LockMove(false); }
			if (actions) { _canAction = true; }
		}

		#endregion

		#region Misc

		/// <summary>
		/// Returns whether the character is near the ground.
		/// </summary>
		public bool AcquiringGround()
		{
			return superCharacterController.currentGround.IsGrounded(false, 0.01f);
		}

		/// <summary>
		/// Returns whether the character is on the ground.
		/// </summary>
		public bool MaintainingGround()
		{
			return superCharacterController.currentGround.IsGrounded(true, 0.5f);
		}

		/// <summary>
		/// Set Animator Trigger.
		/// </summary>
		public void SetAnimatorTrigger(AnimatorTrigger trigger)
		{
			//Debug.Log("SetAnimatorTrigger: " + trigger + " - " + ( int )trigger);
			animator.SetInteger("Trigger Number", ( int )trigger);
			animator.SetTrigger("Trigger");
		}

		/// <summary>
		/// Set Animator Bool.
		/// </summary>
		public void SetAnimatorBool(string name, bool b)
		{
			//Debug.Log("SetAnimatorBool: " + name + b);
			animator.SetBool(name, b);
		}

		/// <summary>
		/// Set Animator float.
		/// </summary>
		public void SetAnimatorFloat(string name, float f)
		{
			//Debug.Log("SetAnimatorFloat: " + name + f);
			animator.SetFloat(name, f);
		}

		/// <summary>
		/// Set Animator ingeter.
		/// </summary>
		public void SetAnimatorInt(string name, int i)
		{
			//Debug.Log("SetAnimatorInt: " + name + i);
			animator.SetInteger(name, i);
		}

		/// <summary>
		/// Set Animator to use root motion or not.
		/// </summary>
		public void SetAnimatorRootMotion(bool b)
		{
			useRootMotion = b;
		}

		/// <summary>
		/// Prints out all the WarriorController variables.
		/// </summary>
		public void ControllerDebug()
		{
			Debug.Log("CONTROLLER SETTINGS---------------------------");
			Debug.Log("useInputSystem: " + useInputSystem);
			Debug.Log("allowedInput: " + allowedInput);
			Debug.Log("isMoving: " + isMoving);
			Debug.Log("useRootMotion: " + useRootMotion);
			Debug.Log("canAction: " + canAction);
			Debug.Log("canMove: " + canMove);
			Debug.Log("canJump: " + canJump);
			Debug.Log("animationSpeed: " + animationSpeed);
		}

		/// <summary>
		/// Prints out all the Animator parameters.
		/// </summary>
		public void AnimatorDebug()
		{
			Debug.Log("ANIMATOR SETTINGS---------------------------");
			Debug.Log("Moving: " + animator.GetBool("Moving"));
			Debug.Log("Jumping: " + animator.GetInteger("Jumping"));
			Debug.Log("Trigger Number: " + animator.GetInteger("Trigger Number"));
			Debug.Log("Velocity: " + animator.GetFloat("Velocity"));
		}

		#endregion
	}
}