using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Panels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

#if UNITY_EDITOR
namespace Nox.KeyBinding.Runtime {
	public class KeyBindingPanel : IEditorPanelBuilder, IDisposable {
		public string GetId()
			=> "keybinding";

		public string GetName()
			=> "Keybinding";

		public bool IsHidden()
			=> false;

		private readonly VisualElement _root       = new();
		private          DateTime      _lastUpdate = DateTime.MinValue;

		public void OnUpdate() {
			if (DateTime.UtcNow - _lastUpdate < TimeSpan.FromSeconds(2.5)) return;
			_lastUpdate = DateTime.UtcNow;

			foreach (var binding in KeyBindingSystem.Instance.Bindings) {
				var child = GetBindingElement(binding.Id, binding.Category);
				if (child != null) UpdateBinding(child, binding);
				else OnKeyBindingAdded(binding);
			}
		}

		public KeyBindingPanel() {
			KeyBindingSystem.OnKeyBindingAdded.AddListener(OnKeyBindingAdded);
			KeyBindingSystem.OnKeyBindingRemoved.AddListener(OnKeyBindingRemoved);
		}

		public void Dispose() {
			KeyBindingSystem.OnKeyBindingAdded.RemoveListener(OnKeyBindingAdded);
			KeyBindingSystem.OnKeyBindingRemoved.RemoveListener(OnKeyBindingRemoved);
		}

		private class UserData {
			private readonly  string     _id;
			private readonly  string     _category;
			internal readonly UnityEvent OnUpdateDetected = new();

			internal UserData(KeyBinding binding) {
				_id                      =  binding.Id;
				_category                =  binding.Category;
				binding.Action.performed += OnUpdate;
				binding.Action.canceled  += OnUpdate;
				binding.Action.started   += OnUpdate;
			}

			internal void Dispose(KeyBinding kb) {
				kb.Action.performed -= OnUpdate;
				kb.Action.canceled  -= OnUpdate;
				kb.Action.started   -= OnUpdate;
				OnUpdateDetected.RemoveAllListeners();
			}


			private void OnUpdate(InputAction.CallbackContext obj)
				=> OnUpdateDetected.Invoke();

			public override string ToString()
				=> string.IsNullOrEmpty(_category) ? _id : $"{_category}.{_id}";

			public bool Equals(string id, string category = null) {
				if (string.IsNullOrEmpty(category))
					return _id == id;
				return _id == id && _category == category;
			}
		}

		private VisualElement GetBindingElement(string id, string category = null)
			=> _root.Q("list")
				?.Children()
				.FirstOrDefault(c => c.userData is UserData data && data.Equals(id, category));

		private void OnKeyBindingAdded(KeyBinding binding) {
			var list = _root.Q("list");
			if (list == null) return;
			var child = GetBindingElement(binding.Id, binding.Category);
			if (child != null) {
				UpdateBinding(child, binding);
				return;
			}

			child                = KeyBindingSystem.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("binding.uxml").CloneTree();
			child.style.flexGrow = 1;
			var ud = new UserData(binding);
			child.userData = ud;
			ud.OnUpdateDetected.AddListener(
				() => {
					var updatedChild = GetBindingElement(binding.Id, binding.Category);
					if (updatedChild != null) UpdateBinding(updatedChild, binding);
				}
			);
			UpdateBinding(child, binding);
			list.Add(child);
		}

		private void OnKeyBindingRemoved(KeyBinding binding) {
			var child = GetBindingElement(binding.Id, binding.Category);
			if (child == null) return;
			if (child.userData is UserData userData)
				userData.Dispose(binding);
			_root.Q("list")?.Remove(child);
			child.RemoveFromHierarchy();
		}

		private void UpdateBinding(VisualElement child, KeyBinding binding) {
			if (child == null || binding == null) {
				Logger.LogError("Cannot update binding: child or binding is null");
				return;
			}

			var label = child.Q<Foldout>("name");
			label.text = string.Join(
				"",
				binding.Category == null ? "" : binding.Category + ".",
				binding.Id,
				binding.Action == null ? " - No action bound" : binding.Action.enabled ? "" : " - Disabled",
				binding.IsOverridden() ? " - Override" : "",
				binding.Action?.IsPressed() ?? false ? " (Pressed)" : ""
			);

			var keys = child.Q<ListView>("keys");
			keys.Clear();
			if (binding.Action != null && binding.Action.bindings.Any()) {
				keys.itemsSource = binding.Action.bindings.Select(
						b => b.effectivePath
					)
					.ToArray();
				keys.makeItem = () => new Label();
				keys.bindItem = (e, i) => ((Label)e).text = keys.itemsSource[i].ToString();
			} else {
				keys.itemsSource = new[] { "No keys bound" };
				keys.makeItem    = () => new Label();
				keys.bindItem    = (e, i) => ((Label)e).text = keys.itemsSource[i].ToString();
			}

			var actions = child.Q<ListView>("actions");
			actions.Clear();

			actions.itemsSource = binding.Actions.ToArray();
			actions.makeItem    = () => new Label();
			actions.bindItem    = (e, i) => ((Label)e).text = actions.itemsSource[i].ToString();

			keys.Rebuild();
		}

		public VisualElement Make(Dictionary<string, object> data) {
			_root.ClearBindings();
			_root.Clear();

			var child = KeyBindingSystem.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("panel.uxml").CloneTree();
			child.style.flexGrow = 1;
			_root.Add(child);

			_root.Q<Label>("version").text = "v" + KeyBindingSystem.CoreAPI.ModMetadata.GetVersion();

			foreach (var binding in KeyBindingSystem.Instance.Bindings)
				OnKeyBindingAdded(binding);

			return _root;
		}
	}
}
#endif // UNITY_EDITOR