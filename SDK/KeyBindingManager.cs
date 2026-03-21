namespace Nox.KeyBindings {
	/// <summary>
	/// Interface for managing key bindings in the game.
	/// </summary>
	public interface IKeyBindingManager {
		/// <summary>
		/// Adds a new key binding.
		/// If a binding with the same id already exists in the specified category, it will not be added.
		/// </summary>
		/// <param name="id">The unique identifier for the key binding in a category.</param>
		/// <param name="binding">The input binding string, e.g., "&lt;Keyboard&gt;/a"</param>
		/// <param name="category">The category of the key binding. If null, no category will be set.</param>
		/// <returns>An instance of <see cref="IKeyBinding"/> representing the added key binding.</returns>
		public IKeyBinding AddKeyBinding(string id, string binding, string category = null);

		/// <summary>
		/// Retrieves a key binding by its identifier and category.
		/// Returns null if no binding is found.
		/// </summary>
		/// <param name="id">The unique identifier for the key binding.</param>
		/// <param name="category"> The category of the key binding. If null, it will match any category.</param>
		/// <returns>An instance of <see cref="IKeyBinding"/> if found, otherwise null.</returns>
		public IKeyBinding GetKeyBinding(string id, string category = null);

		/// <summary>
		/// Removes a key binding by its identifier and category.
		/// </summary>
		/// <param name="id">The unique identifier for the key binding.</param>
		/// <param name="category"> The category of the key binding. If null, it will match any category.</param>
		/// <returns>True if the key binding was successfully removed or did not exist, otherwise false.</returns>
		public bool RemoveKeyBinding(string id, string category = null);

		/// <summary>
		/// Checks if a key binding exists by its identifier and category.
		/// </summary>
		/// <param name="id">The unique identifier for the key binding.</param>
		/// <param name="category">The category of the key binding. If null, it will match any category.</param>
		/// <returns>True if the key binding exists, otherwise false.</returns>
		public bool HasKeyBinding(string id, string category = null);
	}
}