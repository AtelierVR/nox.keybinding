#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.Editor.Panel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.KeyBinding.Runtime {
	public class KeyBindingPanel : IEditorModInitializer, Nox.Editor.Panel.IPanel {
		internal IEditorModCoreAPI          API;
		internal KeyBindingPanelInstance    Instance;

		public void OnInitializeEditor(IEditorModCoreAPI api) => API = api;
		public void OnDisposeEditor() { Instance?.OnDestroy(); API = null; }
		public void OnUpdateEditor()  => Instance?.OnUpdate();

		public string[] GetPath()  => new[] { "keybinding" };
		public string   GetLabel() => "Keybinding";

		public IInstance[] GetInstances()
			=> Instance != null ? new IInstance[] { Instance } : Array.Empty<IInstance>();

		public IInstance Instantiate(IWindow window, Dictionary<string, object> data)
			=> Instance = new KeyBindingPanelInstance(this, window);
	}

	public class KeyBindingPanelInstance : IInstance {
		private readonly KeyBindingPanel _panel;
		private readonly IWindow         _window;
		private          VisualElement   _root;
		private          DateTime        _lastUpdate = DateTime.MinValue;

		public KeyBindingPanelInstance(KeyBindingPanel panel, IWindow window) {
			_panel  = panel;
			_window = window;
			KeyBindingSystem.OnKeyBindingAdded.AddListener(OnKeyBindingAdded);
			KeyBindingSystem.OnKeyBindingRemoved.AddListener(OnKeyBindingRemoved);
		}

		public Nox.Editor.Panel.IPanel GetPanel()  => _panel;
		public IWindow                 GetWindow() => _window;
		public string                  GetTitle()  => "Keybinding";

		public void OnDestroy() {
			KeyBindingSystem.OnKeyBindingAdded.RemoveListener(OnKeyBindingAdded);
			KeyBindingSystem.OnKeyBindingRemoved.RemoveListener(OnKeyBindingRemoved);
			_panel.Instance = null;
		}

		public VisualElement GetContent() {
			if (_root != null) return _root;
			_root = _panel.API.AssetAPI.GetAsset<VisualTreeAsset>("panel.uxml").CloneTree();
			_root.style.flexGrow = 1;
			foreach (var binding in KeyBindingSystem.Instance.Bindings)
				OnKeyBindingAdded(binding);
			UpdateEmptyState();
			return _root;
		}

		internal void OnUpdate() {
			if (DateTime.UtcNow - _lastUpdate < TimeSpan.FromSeconds(2.5)) return;
			_lastUpdate = DateTime.UtcNow;

			foreach (var binding in KeyBindingSystem.Instance.Bindings) {
				var child = GetBindingElement(binding.Id, binding.Category);
				if (child != null) UpdateBinding(child, binding);
				else OnKeyBindingAdded(binding);
			}
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
			=> _root?.Q("list")
				?.Children()
				.FirstOrDefault(c => c.userData is UserData data && data.Equals(id, category));

		private void OnKeyBindingAdded(KeyBinding binding) {
			var list = _root?.Q("list");
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
			UpdateEmptyState();
		}

		private void OnKeyBindingRemoved(KeyBinding binding) {
			var child = GetBindingElement(binding.Id, binding.Category);
			if (child == null) return;
			if (child.userData is UserData userData)
				userData.Dispose(binding);
			_root.Q("list")?.Remove(child);
			child.RemoveFromHierarchy();
			UpdateEmptyState();
		}

		private void UpdateEmptyState() {
			var list  = _root?.Q("list");
			var empty = _root?.Q("empty");
			if (list == null || empty == null) return;
			var hasItems = list.childCount > 0;
			empty.EnableInClassList("hidden", hasItems);
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

	}
}
#endif // UNITY_EDITOR