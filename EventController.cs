// http://paulmoore.mit-license.org/

using UnityEngine;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

// TODO: Might be worth it to use WeakReferences to prevent leaks, but it seem unlikely it will be needed

namespace UnJect {

	/// <summary>
	/// Manages all event dependencies.
	/// Hooks up all event listeners.
	/// Fires events on demand.
	/// </summary>
	public static class EventController {
		private static Dictionary<string, HashSet<EventInjection>> eventMap;

		static EventController() {
			eventMap = new Dictionary<string, HashSet<EventInjection>>();
		}

		private static void FireEvent(EventInjection injection, object[] args) {
			MonoBehaviour component = injection.component;
			MethodInfo method = injection.method;
			try {
				object result = method.Invoke(component, args);
				// Allow for Unity style coroutines to be used
				if (result is IEnumerator) {
					component.StartCoroutine((IEnumerator)result);
				}
			} catch (System.Exception e) {
				Debug.LogError(injection);
				Debug.LogError(e);
			}
		}

		#region Event Mapping

		/// <summary>
		/// Registers an event listener.
		/// </summary>
		/// <param name="injection">The event listener info.</param>
		public static void RegisterEvent(EventInjection injection) {
			HashSet<EventInjection> listeners;
			if (!eventMap.TryGetValue(injection.inject.Name, out listeners)) {
				listeners = new HashSet<EventInjection>();
				eventMap.Add(injection.inject.Name, listeners);
			}
			listeners.Add(injection);
		}

		/// <summary>
		/// Unregisters an event listener.
		/// </summary>
		/// <param name="injection">The event listener info.</param>
		public static void UnregisterEvent(EventInjection injection) {
			HashSet<EventInjection> listeners;
			if (eventMap.TryGetValue(injection.inject.Name, out listeners)) {
				listeners.Remove(injection);
			}
		}

		#endregion

		#region Extention Methods to Fire Events

		/// <summary>
		/// Fires an event to all components attached to the GameObject.
		/// </summary>
		/// <param name="go">The GameObject.</param>
		/// <param name="name">The name of the event.</param>
		/// <param name="args">The event arguments.</param>
		public static void SendEvent(this GameObject go, string name, params object[] args) {
			HashSet<EventInjection> listeners;
			if (eventMap.TryGetValue(name, out listeners)) {
				foreach (EventInjection injection in listeners) {
					if (!injection.component) {
						continue;
					}
					if (go.transform == injection.component.transform) {
						FireEvent(injection, args);
					}
				}
			}
		}

		/// <summary>
		/// Sends an event to this component and all other components attached to the GameObject.
		/// </summary>
		/// <param name="component">The Component.</param>
		/// <param name="name">The name of the event.</param>
		/// <param name="args">The event arguments.</param>
		public static void SendEvent(this MonoBehaviour component, string name, params object[] args) {
			SendEvent(component.gameObject, name, args);
		}

		/// <summary>
		/// Sends an event to all components of this GameObject or the GameObject's parents.
		/// </summary>
		/// <param name="go">The GameObject.</param>
		/// <param name="name">The name of the event.</param>
		/// <param name="args">The event arguments.</param>
		public static void SendEventUp(this GameObject go, string name, params object[] args) {
			HashSet<EventInjection> listeners;
			if (eventMap.TryGetValue(name, out listeners)) {
				foreach (EventInjection injection in listeners) {
					if (!injection.component) {
						continue;
					}
					if (go.transform.IsChildOf(injection.component.transform)) {
						FireEvent(injection, args);
					}
				}
			}
		}

		/// <summary>
		/// Sends an event to this component and all other components attached to the GameObject and all parents of the GameObject.
		/// </summary>
		/// <param name="component">The Component.</param>
		/// <param name="name">The name of the event.</param>
		/// <param name="args">The event arguments.</param>
		public static void SendEventUp(this MonoBehaviour component, string name, params object[] args) {
			SendEventUp(component.gameObject, name, args);
		}

		/// <summary>
		/// Sends an event to this GameObject and all children of this GameObject.
		/// </summary>
		/// <param name="go">The GameObject.</param>
		/// <param name="name">The name of the event.</param>
		/// <param name="args">The event arguments.</param>
		public static void SendEventDown(this GameObject go, string name, params object[] args) {
			HashSet<EventInjection> listeners;
			if (eventMap.TryGetValue(name, out listeners)) {
				foreach (EventInjection injection in listeners) {
					if (!injection.component) {
						continue;
					}
					if (injection.component.transform.IsChildOf(go.transform)) {
						FireEvent(injection, args);
					}
				}
			}
		}

		/// <summary>
		/// Sends an event to this Component and all other components attached to the GameObject, and all children of the GameObject.
		/// </summary>
		/// <param name="component">The Component.</param>
		/// <param name="name">The name of the event.</param>
		/// <param name="args">The event arguments.</param>
		public static void SendEventDown(this MonoBehaviour component, string name, params object[] args) {
			SendEventDown(component.gameObject, name, args);
		}

		/// <summary>
		/// Sends an event to every registered listener to this event.
		/// </summary>
		/// <param name="go">The GameObject sending the event.</param>
		/// <param name="name">The name of the event.</param>
		/// <param name="args">The event arguments.</param>
		public static void SendGlobalEvent(this GameObject go, string name, params object[] args) {
			HashSet<EventInjection> listeners;
			if (eventMap.TryGetValue(name, out listeners)) {
				foreach (EventInjection injection in listeners) {
					if (!injection.component) {
						continue;
					}
					FireEvent(injection, args);
				}
			}
		}

		/// <summary>
		/// Sends an event to every registered listener to this event.
		/// </summary>
		/// <param name="component">The Component sending the event.</param>
		/// <param name="name">The name of the event.</param>
		/// <param name="args">The event arguments.</param>
		public static void SendGlobalEvent(this MonoBehaviour component, string name, params object[] args) {
			SendGlobalEvent(component.gameObject, name, args);
		}

		#endregion
	}
}
