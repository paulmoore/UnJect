// http://paulmoore.mit-license.org/

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnJect {

	/// <summary>
	/// Add this component to any GameObject with components that have Inject attributes.
	/// This is the object which actually does the work of finding and injecting dependencies.
	/// </summary>
	/// <remarks>
	/// Implement an OnInject() method to get notified when dependency injection has completed.
	/// </remarks>
	public sealed class Injector : MonoBehaviour {

		/// <summary>
		/// Your objects might need to be initialized in some order.
		/// Using a non-zero frameDelay will cause the injection to wait for that number of frames.
		/// A frameDelay of zero will cause the injection to run immediately on Start().
		/// </summary>
		public int frameDelay = 0;

		private IEnumerable<EventInjection> cachedEvents;

		private static readonly BindingFlags flags;

		static Injector() {
			flags = 0;
			flags |= BindingFlags.Public;
			flags |= BindingFlags.NonPublic;
			flags |= BindingFlags.Instance;
			flags |= BindingFlags.FlattenHierarchy;
		}

		#region Field Injections

		private void InjectFieldDependencies() {
			foreach (FieldInjection injection in FindFieldInjections()) {
				MonoBehaviour component = injection.component;
				FieldInject inject = injection.inject;
				FieldInfo field = injection.field;
				object dependency = inject.FindDependency(component, field);
				if (dependency != null) {
					field.SetValue(component, dependency);
				} else if (!inject.Optional) {
					Debug.LogError("Could not find dependency!\n"+injection);
				}
			}
		}

		private IEnumerable<FieldInjection> FindFieldInjections() {
			// Finds all fields that have an inject tag on them
			List<FieldInjection> injections = new List<FieldInjection>();
			foreach (var component in GetComponents<MonoBehaviour>()) {
				foreach (var field in component.GetType().GetFields(flags)) {
					object[] attrs = field.GetCustomAttributes(typeof(FieldInject), true);
					if (attrs.Length > 0) {
						FieldInject inject = (FieldInject)attrs[0];
						injections.Add(new FieldInjection(component, inject, field));
					}
				}
			}
			return injections;
		}

		#endregion

		#region Event Injections

		private void RegisterEventDependencies() {
			UnregisterEventDependencies();
			cachedEvents = FindEventInjections();
			foreach (EventInjection injection in cachedEvents) {
				EventController.RegisterEvent(injection);
			}
		}

		private void UnregisterEventDependencies() {
			if (cachedEvents != null) {
				foreach (EventInjection injection in cachedEvents) {
					EventController.UnregisterEvent(injection);
				}
				cachedEvents = null;
			}
		}

		private IEnumerable<EventInjection> FindEventInjections() {
			// Finds all methods that have an Inject tag on them
			List<EventInjection> injections = new List<EventInjection>();
			foreach (var component in GetComponents<MonoBehaviour>()) {
				foreach (var method in component.GetType().GetMethods(flags)) {
					object[] attrs = method.GetCustomAttributes(typeof(EventInject), true);
					if (attrs.Length > 0) {
						EventInject inject = (EventInject)attrs[0];
						if (string.IsNullOrEmpty(inject.Name)) {
							inject.Name = method.Name;
						}
						injections.Add(new EventInjection(component, inject, method));
					}
				}
			}
			return injections;
		}

		#endregion

		#region Events

		private IEnumerator Start() {
			while (frameDelay --> 0) {
				yield return null;
			}
			InjectFieldDependencies();
			RegisterEventDependencies();
			SendMessage("OnInject", SendMessageOptions.DontRequireReceiver);
		}

		private void OnDestroy() {
			UnregisterEventDependencies();
		}

		#endregion
	}
}
