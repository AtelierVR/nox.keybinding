using System;
using System.Collections.Generic;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.KeyBindings;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.KeyBinding.Runtime {
	public class KeyBindingSystem : IKeyBindingManager, IMainModInitializer {
		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
		}

		public void OnDisposeMain() {
			foreach (var binding in Bindings.ToArray())
				RemoveKeyBinding(binding.Id, binding.Category);
			CoreAPI  = null;
			Instance = null;
		}

		static internal          IMainModCoreAPI         CoreAPI;
		static internal          KeyBindingSystem       Instance;
		readonly internal        List<KeyBinding>       Bindings            = new();
		static readonly internal UnityEvent<KeyBinding> OnKeyBindingAdded   = new();
		static readonly internal UnityEvent<KeyBinding> OnKeyBindingRemoved = new();

		[NoxPublic(NoxAccess.Method)]
		public IKeyBinding AddKeyBinding(string id, string binding, string category = null) {
			if (string.IsNullOrWhiteSpace(id)) {
				Logger.LogError("Key binding id cannot be null or empty");
				return null;
			}

			if (string.IsNullOrWhiteSpace(binding)) {
				Logger.LogError("Key binding action cannot be null or empty");
				return null;
			}

			if (HasKeyBinding(id, category)) {
				Logger.LogWarning($"Key binding with id {id} already exists in category {category}");
				return Internal_GetKeyBinding(id, category);
			}

			var inputAction = new InputAction(id, InputActionType.Value, binding);
			var keybinding  = new KeyBinding(id.ToLowerInvariant(), category?.ToLowerInvariant(), inputAction);

			var config = Config.Load();
			var key    = keybinding.GetConfigPath();
			if (config.Has(key)) {
				var action = config.Get<string>(key);
				if (!string.IsNullOrWhiteSpace(action)) {
					keybinding.Action.LoadBindingOverridesFromJson(action);
					keybinding.Overridden = true;
				}
			}

			Bindings.Add(keybinding);
			CoreAPI.EventAPI.Emit("key_binding_added", keybinding);
			OnKeyBindingAdded.Invoke(keybinding);

			return keybinding;
		}

		[NoxPublic(NoxAccess.Method)]
		public IKeyBinding GetKeyBinding(string id, string category = null)
			=> Internal_GetKeyBinding(id, category);

		private KeyBinding Internal_GetKeyBinding(string id, string category = null)
			=> Bindings.Find(b => b.Id == id && (string.IsNullOrWhiteSpace(category) || b.Category == category));

		[NoxPublic(NoxAccess.Method)]
		public bool RemoveKeyBinding(string id, string category = null) {
			var binding = Internal_GetKeyBinding(id, category);
			if (binding == null) {
				Logger.LogError($"Key binding with id {id} does not exist");
				return true;
			}

			var canRemove = true;
			CoreAPI.EventAPI.Emit("key_binding_request_remove", binding, new Action<object[]>(OnRequestRemove));
			if (!canRemove) {
				Logger.LogDebug($"Canceling removing key binding {binding.Category}.{binding.Id}");
				return false;
			}

			var config = Config.Load();
			var key    = binding.GetConfigPath();
			if (binding.IsOverridden()) {
				var res = binding.Action.SaveBindingOverridesAsJson();
				if (!string.IsNullOrWhiteSpace(res)) {
					config.Set(key, res);
					config.Save();
				}
			} else if (config.Has(key)) {
				config.Remove(key);
				config.Save();
			}

			binding.Action.Disable();
			Bindings.Remove(binding);

			CoreAPI.EventAPI.Emit("key_binding_removed", binding);
			OnKeyBindingRemoved.Invoke(binding);
			return true;

			void OnRequestRemove(object[] rms) {
				if (rms.Length > 0 && rms[0] is false)
					canRemove = false;
			}
		}

		[NoxPublic(NoxAccess.Method)]
		public bool HasKeyBinding(string id, string category = null)
			=> Bindings.Exists(b => b.Id == id && (string.IsNullOrWhiteSpace(category) || b.Category == category));
	}
}