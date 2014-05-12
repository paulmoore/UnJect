// http://paulmoore.mit-license.org/

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnJect {

	#region Base Injector

	/// <summary>
	/// Base inject attribute for injecting dependencies into fields.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public abstract class FieldInject : Attribute {

		/// <summary>
		/// If True, a warning will not be thrown if the dependency cannot be found.
		/// </summary>
		public bool Optional { get; set; }

		/// <summary>
		/// Finds the dependency for this field.
		/// </summary>
		/// <returns>The dependency.</returns>
		/// <param name="comp">Comp.</param>
		/// <param name="field">Field.</param>
		abstract public object FindDependency(Component comp, FieldInfo field);

		/// <summary>
		/// Some dependencies may require a component of the GameObject or the GameObject itself.
		/// </summary>
		/// <returns>The component of the proper type in the GameObject.</returns>
		/// <param name="go">The GameObject to extract the component from.</param>
		/// <param name="type">The type of the component.</param>
		protected object ExtractComponent(GameObject go, Type type) {
			if (!go) {
				return null;
			}
			if (typeof(GameObject).IsAssignableFrom(type)) {
				return go;
			}
			return go.GetComponent(type);
		}

		protected bool IsContainerType(FieldInfo field) {
			Type fieldType = field.FieldType;
			return fieldType.IsArray || typeof(IList).IsAssignableFrom(fieldType);
		}

		protected object CreateArrayOrList(FieldInfo field, IEnumerable<GameObject> deps) {
			Type containerType = field.FieldType;
			if (field.FieldType.IsArray) {
				Type elementType = containerType.GetElementType();
				List<object> objs = new List<object>();
				foreach (GameObject go in deps) {
					object obj = ExtractComponent(go, elementType);
					if (obj != null) {
						objs.Add(obj);
					}
				}
				Array arr = Array.CreateInstance(elementType, objs.Count);
				for (int i = 0; i < objs.Count; i++) {
					arr.SetValue(objs[i], i);
				}
				return arr;
			} else if (typeof(IList).IsAssignableFrom(containerType)) {
				Type[] genericArgs = containerType.GetGenericArguments();
				if (genericArgs.Length == 0) {
					Debug.LogError(string.Format("{0} Must be used with a generic List<T> or typed array but the container type was {1}", this, containerType));
					return null;
				}
				Type elementType = genericArgs[0];
				IList objs = (IList)Activator.CreateInstance(containerType);
				foreach (GameObject go in deps) {
					object obj = ExtractComponent(go, elementType);
					if (obj != null) {
						objs.Add(obj);
					}
				}
				return objs;
			}
			Debug.LogError(string.Format("{0} Cannot be used because the field type {1} does not derive from IList or Array!", this, field.FieldType));
			return null;
		}

		override public string ToString() {
			return string.Format("[{0}]", GetType().Name);
		}
	}

	#endregion

	#region Name Injectors

	/// <summary>
	/// Injects a dependency based on the name of the GameObject.
	/// </summary>
	/// <remarks>
	/// If the field type is an array or an IList, all matching dependencies will be stored in that container type.
	/// </remarks>
	/// <seealso cref="UnJect.InjectSharedByName"/>
	public sealed class InjectByName : FieldInject {
		private StringComparison compareType;

		/// <summary>
		/// The name of the dependency.
		/// Leave as Null (default) to use the name of the field as the name to look for.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// If True (default) will search all nested children of the GameObject as well for the dependency.
		/// If False, it will only search the immediate children of the GameObject.
		/// </summary>
		public bool Recursive { get; set; }

		/// <summary>
		/// If True will first search for the dependency in the transform root,
		/// doing a bredth-first-search for the dependency.
		/// </summary>
		public bool SearchParents { get; set; }

		/// <summary>
		/// Should the name search be case-sensitive (False by default).
		/// </summary>
		public bool CaseSensitive {
			get {
				return compareType == StringComparison.InvariantCulture;
			}
			set {
				compareType = value ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			}
		}

		public InjectByName(string name = null, bool recursive = true) {
			Name = name;
			Recursive = recursive;
			compareType = StringComparison.InvariantCultureIgnoreCase;
		}

		override public object FindDependency(Component comp, FieldInfo field) {
			GameObject go = SearchParents ? comp.transform.root.gameObject : comp.gameObject;
			string name = string.IsNullOrEmpty(Name) ? field.Name : Name;
			if (IsContainerType(field)) {
				List<GameObject> deps = new List<GameObject>();
				if (name.Equals(go.name, compareType)) {
					deps.Add(go);
				}
				FindDependenciesInternal(name, go, deps);
				return CreateArrayOrList(field, deps);
			}
			if (name.Equals(go.name, compareType)) {
				return ExtractComponent(go, field.FieldType);
			}
			GameObject dep = FindDependencyInternal(name, go);
			return ExtractComponent(dep, field.FieldType);
		}

		private GameObject FindDependencyInternal(string name, GameObject go) {
			foreach (Transform child in go.transform) {
				if (name.Equals(child.name, compareType)) {
					return child.gameObject;
				}
			}
			if (Recursive) {
				foreach (Transform child in go.transform) {
					GameObject dep = FindDependencyInternal(name, child.gameObject);
					if (dep) {
						return dep;
					}
				}
			}
			return null;
		}

		private void FindDependenciesInternal(string name, GameObject go, List<GameObject> deps) {
			foreach (Transform child in go.transform) {
				if (name.Equals(child.name, compareType)) {
					deps.Add(child.gameObject);
				}
			}
			if (Recursive) {
				foreach (Transform child in go.transform) {
					FindDependenciesInternal(name, child.gameObject, deps);
				}
			}
		}

		override public string ToString() {
			return string.Format("[InjectByName(Name = {0}, Recursive = {1}, SearchParents = {2}, CaseSensitive = {3})]", Name, Recursive, SearchParents, CaseSensitive);
		}
	}

	/// <summary>
	/// Like InjectByName, but will find the Singleton instance of the dependency based on the name.
	/// </summary>
	/// <remarks>
	/// Be careful because this is case-sensitive by default (due to GameObject.Find).
	/// If the field type is an array or an IList, all matching dependencies will be stored in that container type.
	/// </remarks>
	/// <seealso cref="UnJect.InjectByName"/>
	public sealed class InjectSharedByName : FieldInject {
		private StringComparison compareType;

		/// <summary>
		/// The name of the dependency.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Should the name search be case-sensitive (True by default).
		/// </summary>
		public bool CaseSensitive {
			get {
				return compareType == StringComparison.InvariantCulture;
			}
			set {
				compareType = value ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			}
		}

		public InjectSharedByName(string name = null) {
			Name = name;
			compareType = StringComparison.InvariantCulture;
		}
		
		override public object FindDependency(Component comp, FieldInfo field) {
			string name = string.IsNullOrEmpty(Name) ? field.Name : Name;
			if (IsContainerType(field)) {
				List<GameObject> deps = new List<GameObject>();
				foreach (Transform obj in GameObject.FindObjectsOfType(typeof(Transform))) {
					if (name.Equals(obj.name, compareType)) {
						deps.Add(obj.gameObject);
					}
				}
				return CreateArrayOrList(field, deps);
			}
			if (CaseSensitive) {
				GameObject go = GameObject.Find(name);
				if (go) {
					return ExtractComponent(go, field.FieldType);
				}
			} else {
				foreach (Transform obj in GameObject.FindObjectsOfType(typeof(Transform))) {
					if (name.Equals(obj.name, compareType)) {
						return ExtractComponent(obj.gameObject, field.FieldType);
					}
				}
			}
			return null;
		}
		
		override public string ToString() {
			return string.Format("[InjectSharedByName(Name = {0}, CaseSensitive = {1})]", Name, CaseSensitive);
		}
	}
	
	#endregion
	
	#region Type Injectors
	
	/// <summary>
	/// Injects a dependency based on the field type.
	/// </summary>
	///	<remarks>
	/// If the field type is an array or an IList, all matching dependencies will be stored in that container type.
	/// </remarks>
	/// <seealso cref="UnJect.InjectSharedByType"/>
	public sealed class InjectByType : FieldInject {

		/// <summary>
		/// If True (default) will search children of the GameObject for the dependency.
		/// </summary>
		public bool Recursive { get; set; }
	
		/// <summary>
		/// If True (default) will include all inactive children.
		/// This only works if the field is a container type!
		/// </summary>
		public bool IncludeInactive { get; set; }

		/// <summary>
		/// If True will first search for the dependency in the transform root,
		/// doing a depth-first-search for the dependency.
		/// </summary>
		public bool SearchParents { get; set; }

		public InjectByType(bool recursive = true, bool includeInactive = true) {
			Recursive = recursive;
			IncludeInactive = includeInactive;
		}

		private GameObject[] ConvertToGOArray(Component[] comps) {
			GameObject[] gos = new GameObject[comps.Length];
			for (int i = 0; i < comps.Length; i++) {
				gos[i] = comps[i].gameObject;
			}
			return gos;
		}

		override public object FindDependency(Component comp, FieldInfo field) {
			if (SearchParents) {
				comp = comp.transform.root;
			}
			if (IsContainerType(field)) {
				Type type;
				if (field.FieldType.IsArray) {
					type = field.FieldType.GetElementType();
				} else if (typeof(IList).IsAssignableFrom(field.FieldType)) {
					if (field.FieldType.GetGenericArguments().Length >= 1) {
						type = field.FieldType.GetGenericArguments()[0];
					} else {
						Debug.LogError(string.Format("{0} expected at least one generic argument from the type {1}, use List<T>", this, field.FieldType));
						return null;
					}
				} else {
					Debug.LogError("Shouldn't be here");
					type = null;
				}
				if (Recursive) {
					return CreateArrayOrList(field, ConvertToGOArray(comp.GetComponentsInChildren(type, IncludeInactive)));
				}
				return CreateArrayOrList(field, ConvertToGOArray(comp.GetComponents(type)));
			}
			if (Recursive) {
				return comp.GetComponentInChildren(field.FieldType);
			}
			return comp.GetComponent(field.FieldType);
		}

		override public string ToString() {
			return string.Format("[InjectByType(Recursive = {0}, SearchParents = {1})]", Recursive, SearchParents);
		}
	}

	/// <summary>
	/// Like InjectByType, but will find the Singleton instance of the dependency based on the field type.
	/// </summary>
	///	<remarks>
	/// If the field type is an array or an IList, all matching dependencies will be stored in that container type.
	/// </remarks>
	/// <seealso cref="UnJect.InjectByType"/>
	public sealed class InjectSharedByType : FieldInject {
		
		public InjectSharedByType() {
		}
		
		override public object FindDependency(Component comp, FieldInfo field) {
			if (IsContainerType(field)) {
				UnityEngine.Object[] objs = GameObject.FindObjectsOfType(field.FieldType);
				if (typeof(GameObject).IsAssignableFrom(field.FieldType)) {
					return CreateArrayOrList(field, Array.ConvertAll(objs, elem => (GameObject)elem));
				} else if (typeof(Component).IsAssignableFrom(field.FieldType)) {
					return CreateArrayOrList(field, Array.ConvertAll(objs, elem => ((GameObject)elem).gameObject));
				}
				Debug.LogError("Expected field type of GameObject or component but got: "+field.FieldType);
				return null;
			}
			return GameObject.FindObjectOfType(field.FieldType);
		}
	}

	#endregion

	#region Tag Injectors

	/// <summary>
	/// Inject a dependency based on the tag of the GameObject.
	/// </summary>
	///	<remarks>
	/// If the field type is an array or an IList, all matching dependencies will be stored in that container type.
	/// </remarks>
	/// <seealso cref="UnJect.InjectSharedByTag"/>
	public sealed class InjectByTag : FieldInject {
		
		/// <summary>
		/// The tag of the GameObject to search for.
		/// </summary>
		public string Tag { get; set; }
		
		/// <summary>
		/// If True (default) will search children of the GameObject for the dependency.
		/// </summary>
		public bool Recursive { get; set; }

		/// <summary>
		/// If True will first search for the dependency in the transform root,
		/// doing a bredth-first-search for the dependency.
		/// </summary>
		public bool SearchParents { get; set; }
		
		public InjectByTag(string tag, bool recursive = true) {
			Tag = tag;
			Recursive = recursive;
		}
		
		override public object FindDependency(Component comp, FieldInfo field) {
			if (SearchParents) {
				comp = comp.transform.root;
			}
			if (IsContainerType(field)) {
				List<GameObject> deps = new List<GameObject>();
				if (comp.CompareTag(Tag)) {
					deps.Add(comp.gameObject);
				}
				FindDependenciesInternal(comp.gameObject, deps);
				return CreateArrayOrList(field, deps);
			}
			if (comp.CompareTag(Tag)) {
				return ExtractComponent(comp.gameObject, field.FieldType);
			}
			GameObject dep = FindDependencyInternal(comp.gameObject);
			return ExtractComponent(dep, field.FieldType);
		}
		
		private GameObject FindDependencyInternal(GameObject go) {
			foreach (Transform child in go.transform) {
				if (child.CompareTag(Tag)) {
					return child.gameObject;
				}
			}
			if (Recursive) {
				foreach (Transform child in go.transform) {
					GameObject dep = FindDependencyInternal(child.gameObject);
					if (dep) {
						return dep;
					}
				}
			}
			return null;
		}

		private void FindDependenciesInternal(GameObject go, List<GameObject> deps) {
			foreach (Transform child in go.transform) {
				if (child.CompareTag(Tag)) {
					deps.Add(child.gameObject);
				}
			}
			if (Recursive) {
				foreach (Transform child in go.transform) {
					FindDependenciesInternal(child.gameObject, deps);
				}
			}
		}
		
		override public string ToString() {
			return string.Format("[InjectByTag(Tag = {0}, Recursive = {1}, SearchParents = {2})]", Tag, Recursive, SearchParents);
		}
	}
	
	/// <summary>
	/// Like InjectByTag, but will find the Singleton instance of the dependency based on the tag.
	/// </summary>
	///	<remarks>
	/// If the field type is an array or an IList, all matching dependencies will be stored in that container type.
	/// </remarks>
	/// <seealso cref="UnJect.InjectByTag"/>
	public sealed class InjectSharedByTag : FieldInject {

		/// <summary>
		/// The tag of the GameObject to search for.
		/// </summary>
		public string Tag { get; set; }
		
		public InjectSharedByTag(string tag) {
			Tag = tag;
		}
		
		override public object FindDependency(Component comp, FieldInfo field) {
			if (IsContainerType(field)) {
				return CreateArrayOrList(field, GameObject.FindGameObjectsWithTag(Tag));
			}
			GameObject go = GameObject.FindWithTag(Tag);
			if (go) {
				return ExtractComponent(go, field.FieldType);
			}
			return null;
		}
		
		override public string ToString() {
			return string.Format("[InjectSharedByTag(Tag = {0})]", Tag);
		}
	}

	#endregion

	#region Layer Injectors

	public sealed class InjectByLayer : FieldInject {

		/// <summary>
		/// The Layer index.
		/// </summary>
		public int Layer { get; set; }

		/// <summary>
		/// If True (default) will search children of the GameObject as well for the dependency.
		/// </summary>
		public bool Recursive { get; set; }
		
		/// <summary>
		/// If True will first search for the dependency in the transform root,
		/// doing a depth-first-search for the dependency.
		/// </summary>
		public bool SearchParents { get; set; }

		public InjectByLayer(string layerName) {
			Layer = LayerMask.NameToLayer(layerName);
		}
		
		public InjectByLayer(int layer) {
			Layer = layer;
		}
		
		override public object FindDependency(Component comp, FieldInfo field) {
			GameObject go = SearchParents ? comp.transform.root.gameObject : comp.gameObject;
			if (IsContainerType(field)) {
				List<GameObject> deps = new List<GameObject>();
				if (go.layer == Layer) {
					deps.Add(go);
				}
				FindDependenciesInternal(go, deps);
				return CreateArrayOrList(field, deps);
			}
			if (go.layer == Layer) {
				return ExtractComponent(go, field.FieldType);
			}
			GameObject dep = FindDependencyInternal(go);
			return ExtractComponent(dep, field.FieldType);
		}
		
		private GameObject FindDependencyInternal(GameObject go) {
			foreach (Transform child in go.transform) {
				if (child.gameObject.layer == Layer) {
					return child.gameObject;
				}
			}
			if (Recursive) {
				foreach (Transform child in go.transform) {
					GameObject dep = FindDependencyInternal(child.gameObject);
					if (dep) {
						return dep;
					}
				}
			}
			return null;
		}
		
		private void FindDependenciesInternal(GameObject go, List<GameObject> deps) {
			foreach (Transform child in go.transform) {
				if (child.gameObject.layer == Layer) {
					deps.Add(child.gameObject);
				}
			}
			if (Recursive) {
				foreach (Transform child in go.transform) {
					FindDependenciesInternal(child.gameObject, deps);
				}
			}
		}
		
		override public string ToString() {
			return string.Format("[InjectByLayer(Layer = {0}, Recursive = {1}, SearchParents = {2})]", LayerMask.LayerToName(Layer), Recursive, SearchParents);
		}
	}

	public sealed class InjectSharedByLayer : FieldInject {
		
		/// <summary>
		/// The Layer index.
		/// </summary>
		public int Layer { get; set; }
		
		public InjectSharedByLayer(string layerName) {
			Layer = LayerMask.NameToLayer(layerName);
		}
		
		public InjectSharedByLayer(int layer) {
			Layer = layer;
		}
		
		override public object FindDependency(Component comp, FieldInfo field) {
			List<GameObject> deps = null;
			bool isContainer = IsContainerType(field);
			if (isContainer) {
				deps = new List<GameObject>();
			}
			foreach (Transform obj in GameObject.FindObjectsOfType<Transform>()) {
				if (obj.gameObject.layer == Layer) {
					if (isContainer) {
						deps.Add(obj.gameObject);
					} else {
						return ExtractComponent(obj.gameObject, field.FieldType);
					}
				}
			}
			if (isContainer) {
				return CreateArrayOrList(field, deps);
			}
			return null;
		}
		
		override public string ToString() {
			return string.Format("[InjectSharedByLayer(Layer = {0})]", LayerMask.LayerToName(Layer));
		}
	}

	#endregion

	#region Event Injectors

	public enum EventSource {
		All,   // Accept events from any of the send event methods
		Local, // Only accept events from SendEvent, SendEventDown, or SendEventUp
		Global // Only accept events sent from SendGlobalEvent
	}

	/// <summary>
	/// Base class for Event injections attributes.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public abstract class EventInject : Attribute {

		/// <summary>
		/// The name of the event.
		/// If Null (default) will use the name of the method as the event name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The source to accept events from (default EventSource.All).
		/// </summary>
		public EventSource Source { get; set; }

		public EventInject(string name = null, EventSource source = EventSource.All) {
			Name = name;
			Source = source;
		}
	}

	/// <summary>
	/// Registers a method as an event listener based on the name of the event.
	/// </summary>
	public sealed class InjectEventByName : EventInject {

		override public string ToString() {
			return string.Format("[InjectEventByName(Name = {0}, Source = {1})]", Name, Source);
		}
	}

	#endregion
}
