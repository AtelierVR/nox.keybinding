using System;
using UnityEngine.InputSystem;

namespace Nox.KeyBinding.Runtime {
	public class KeyCallback {
		public KeyCallback(int id, Action<object> callback) {
			Id       = id;
			Callback = callback;
		}

		public readonly   int            Id;
		internal readonly Action<object> Callback;

		public void OnPerformed(InputAction.CallbackContext context)
			=> Callback?.Invoke(context.ReadValueAsObject());

		public void OnCanceled(InputAction.CallbackContext context)
			=> Callback?.Invoke(context.ReadValueAsObject());

		public void OnStarted(InputAction.CallbackContext context)
			=> Callback?.Invoke(context.ReadValueAsObject());

		public override string ToString()
			=> $"{GetType().Name}[Callback={Callback?.Method.Name ?? "null"}]";
	}
}