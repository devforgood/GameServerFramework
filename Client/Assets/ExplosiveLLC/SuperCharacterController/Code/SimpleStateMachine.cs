using System;
using System.Collections.Generic;
using UnityEngine;

public class SimpleStateMachine:MonoBehaviour
{
	public bool DebugGui;
	public Vector2 DebugGuiPosition;

	public string DebugGuiTitle = "Simple Machine";

	protected Enum queueCommand;

	private void OnGUI()
	{
		if (DebugGui) {
			GUI.Box(new Rect(DebugGuiPosition.x, DebugGuiPosition.y, 200, 50), DebugGuiTitle);
			GUI.TextField(new Rect(DebugGuiPosition.x + 10, DebugGuiPosition.y + 20, 180, 20), string.Format("State: {0}", currentState));
		}
	}

	protected float timeEnteredState;

	public class State
	{
		public Action DoUpdate = DoNothing;
		public Action DoFixedUpdate = DoNothing;
		public Action DoLateUpdate = DoNothing;
		public Action DoManualUpdate = DoNothing;
		public Action enterState = DoNothing;
		public Action exitState = DoNothing;

		public Enum currentState;
	}

	public State state = new State();

	public Enum currentState
	{
		get => state.currentState;
		set
		{
			if (state.currentState == value) { return; }

			ChangingState();
			state.currentState = value;
			ConfigureCurrentState();
		}
	}

	[HideInInspector]
	public Enum lastState;

	private void ChangingState()
	{
		lastState = state.currentState;
		timeEnteredState = Time.time;
	}

	private void ConfigureCurrentState()
	{
		if (state.exitState != null) { state.exitState(); }

		// Now we need to configure all of the methods.
		state.DoUpdate = ConfigureDelegate<Action>("Update", DoNothing);
		state.DoFixedUpdate = ConfigureDelegate<Action>("FixedUpdate", DoNothing);
		state.DoLateUpdate = ConfigureDelegate<Action>("LateUpdate", DoNothing);
		state.DoManualUpdate = ConfigureDelegate<Action>("ManualUpdate", DoNothing);
		state.enterState = ConfigureDelegate<Action>("EnterState", DoNothing);
		state.exitState = ConfigureDelegate<Action>("ExitState", DoNothing);

		if (state.enterState != null) { state.enterState(); }
	}

	private Dictionary<Enum, Dictionary<string, Delegate>> _cache = new Dictionary<Enum, Dictionary<string, Delegate>>();

	private T ConfigureDelegate<T>(string methodRoot, T Default) where T : class
	{

		if (!_cache.TryGetValue(state.currentState, out Dictionary<string, Delegate> lookup)) {
			_cache[state.currentState] = lookup = new Dictionary<string, Delegate>();
		}
		if (!lookup.TryGetValue(methodRoot, out Delegate returnValue)) {
			System.Reflection.MethodInfo mtd = GetType().GetMethod(state.currentState.ToString() + "_" + methodRoot, System.Reflection.BindingFlags.Instance
				| System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod);

			if (mtd != null) { returnValue = Delegate.CreateDelegate(typeof(T), this, mtd); }
			else { returnValue = Default as Delegate; }

			lookup[methodRoot] = returnValue;
		}
		return returnValue as T;
	}

	private void Update()
	{
		EarlyGlobalSuperUpdate();

		state.DoUpdate();

		LateGlobalSuperUpdate();
	}

	private void FixedUpdate()
	{
		state.DoFixedUpdate();
	}

	private void LateUpdate()
	{
		state.DoLateUpdate();
	}

	protected virtual void EarlyGlobalSuperUpdate() { }

	protected virtual void LateGlobalSuperUpdate() { }

	private static void DoNothing() { }
}