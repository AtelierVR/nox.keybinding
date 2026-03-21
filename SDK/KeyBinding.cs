using System;
using UnityEngine.InputSystem;

namespace Nox.KeyBindings {
	/// <summary>
	/// Represents a key binding in the game.
	/// </summary>
	public interface IKeyBinding {
		/// <summary>
		/// Gets the unique identifier for the key binding.
		/// Is lower-invariant and should be unique within its category.
		/// </summary>
		/// <returns></returns>
		public string GetId();

		/// <summary>
		/// Gets the category of the key binding.
		/// If null, the binding is not categorized.
		/// Is lower-invariant.
		/// </summary>
		/// <returns></returns>
		public string GetCategory();

		/// <summary>
		/// Gets the <see cref="InputAction"/> associated with the key binding.
		/// </summary>
		/// <returns></returns>
		public InputAction GetAction();

		/// <summary>
		/// Checks if the key binding has been overridden.
		/// </summary>
		/// <returns></returns>
		public bool IsOverridden();

		/// <summary>
		/// Add a listener for the key binding action.
		/// If the action is already registered, it will not be added again.
		/// </summary>
		/// <param name="action"></param>
		/// <typeparam name="T"></typeparam>
		public void AddListener<T>(Action<T> action) where T : struct;

		/// <summary>
		/// Removes a listener for the key binding action.
		/// </summary>
		/// <param name="action"></param>
		/// <typeparam name="T"></typeparam>
		public void RemoveListener<T>(Action<T> action) where T : struct;

		/// <summary>
		/// Checks if a listener is registered for the key binding action.
		/// </summary>
		/// <param name="action"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns>Returns true if the listener is registered, false otherwise.</returns>
		public bool HasListener<T>(Action<T> action) where T : struct;

		/// <summary>
		/// Gets the number of listeners for the key binding action.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public int GetListenerCount();
	}
}