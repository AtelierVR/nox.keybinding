#if UNITY_EDITOR
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;

namespace Nox.KeyBinding.Runtime {
	public class KeyBindingEditor : IEditorModInitializer {
		internal static IEditorModCoreAPI CoreAPI;

		private static IEditorPanel     _kbPanel;
		private        KeyBindingPanel _kb;

		public void OnInitializeEditor(IEditorModCoreAPI api) {
			CoreAPI  = api;
			_kb      = new KeyBindingPanel();
			_kbPanel = api.PanelAPI.AddLocalPanel(_kb);
		}

		public void OnUpdateEditor() {
			_kb.OnUpdate();
		}

		public void OnDisposeEditor() {
			CoreAPI.PanelAPI.RemoveLocalPanel(_kbPanel);
			_kbPanel = null;
			_kb      = null;
			CoreAPI  = null;
		}
	}
}
#endif // UNITY_EDITOR