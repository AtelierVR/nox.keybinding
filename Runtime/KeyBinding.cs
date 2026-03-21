using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Utils;
using Nox.KeyBindings;
using UnityEngine.InputSystem;

namespace Nox.KeyBinding.Runtime
{
	public class KeyBinding : IKeyBinding, INoxObject
	{
		internal KeyBinding(string id, string category, InputAction action)
		{
			Id = id.ToLowerInvariant();
			Category = category?.ToLowerInvariant();
			Action = action;
			Overridden = false;
			Actions = new List<KeyCallback>();
		}

		internal readonly List<KeyCallback> Actions;

		internal readonly string Id;
		internal readonly string Category;
		internal readonly InputAction Action;
		internal bool Overridden;

		public override string ToString()
			=> $"{GetType().Name}[Id={Id}, Category={Category}, IsOverridden={Overridden}, Action={Action}]";

		[NoxPublic(NoxAccess.Method)]
		public string GetId()
			=> Id;

		[NoxPublic(NoxAccess.Method)]
		public string GetCategory()
			=> Category;

		[NoxPublic(NoxAccess.Method)]
		public InputAction GetAction()
			=> Action;

		[NoxPublic(NoxAccess.Method)]
		public bool IsOverridden()
			=> Overridden;

		[NoxPublic(NoxAccess.Method)]
		private KeyCallback GetCallback<T>(Action<T> action) where T : struct
		{
			if (action == null) throw new ArgumentNullException(nameof(action));
			return Actions.FirstOrDefault(cb => cb.Id == action.GetHashCode());
		}

		[NoxPublic(NoxAccess.Method)]
		public void AddListener<T>(Action<T> action) where T : struct
		{
			if (action == null) throw new ArgumentNullException(nameof(action));
			if (HasListener(action))
			{
				Logger.LogWarning($"Listener for action {action.Method.Name} already exists in key binding {Id}");
				return;
			}

			var callback = new KeyCallback(
				action.GetHashCode(),
				o =>
				{
					try
					{
						action(o != null ? (T)o : default);
					}
					catch (Exception e)
					{
						Logger.LogError($"Error invoking action {action.Method.Name}: {e.Message}");
						Logger.LogError(e);
					}
				}
			);
			Actions.Add(callback);
			Action.performed += callback.OnPerformed;
			Action.canceled += callback.OnCanceled;
			Action.started += callback.OnStarted;
			if (!Action.enabled) Action.Enable();
		}

		[NoxPublic(NoxAccess.Method)]
		public void RemoveListener<T>(Action<T> action) where T : struct
		{
			if (action == null) throw new ArgumentNullException(nameof(action));
			var callback = GetCallback(action);
			if (callback == null) return;
			Action.performed -= callback.OnPerformed;
			Action.canceled -= callback.OnCanceled;
			Action.started -= callback.OnStarted;
			Actions.Remove(callback);
			if (GetListenerCount() == 0 && Action.enabled)
				Action.Disable();
		}

		[NoxPublic(NoxAccess.Method)]
		public bool HasListener<T>(Action<T> action) where T : struct
		{
			if (action == null) throw new ArgumentNullException(nameof(action));
			return GetCallback(action) != null;
		}

		[NoxPublic(NoxAccess.Method)]
		public int GetListenerCount()
			=> Actions.Count;

		public string[] GetConfigPath()
			=> new[] { "settings", "key_bindings", Category, Id }
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.ToArray();
	}
}