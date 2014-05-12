// http://paulmoore.mit-license.org/

using System;
using System.Reflection;
using UnityEngine;

namespace UnJect {

	/// <summary>
	/// This class contains all information needed for a dependency injection into a field.
	/// </summary>
	public sealed class FieldInjection {
		public MonoBehaviour component;
		public FieldInject inject;
		public FieldInfo field;

		private const string format =
@"Field Injection:
	Component: {0}({1}),
	Field: {2}({3}),
	Attribute: {4}";

		public FieldInjection(MonoBehaviour component, FieldInject inject, FieldInfo field) {
			this.component = component;
			this.inject = inject;
			this.field = field;
		}

		override public string ToString() {
			return string.Format(format, component.name, component.GetType().Name, field.Name, field.FieldType.Name, inject);
		}
	}

	/// <summary>
	/// This class contains all information needed for a dependency injection into an event listener.
	/// </summary>
	public sealed class EventInjection : IEquatable<EventInjection> {
		public MonoBehaviour component;
		public EventInject inject;
		public MethodInfo method;

		private const string format =
@"Event Injection:
	Component: {0}({1}),
	Method: {2},
	Attribute: {3}";

		public EventInjection(MonoBehaviour component, EventInject inject, MethodInfo method) {
			this.component = component;
			this.inject = inject;
			this.method = method;
		}

		public bool Equals(EventInjection other) {
			return component == other.component && method == other.method;
		}

		override public int GetHashCode() {
			return component.GetHashCode() ^ method.GetHashCode();
		}

		override public string ToString() {
			return string.Format(format, component.name, component.GetType().Name, method.Name, inject);
		}
	}
}
