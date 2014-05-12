UnJect for Unity3D
======

## Wire up your Components quickly and easily in Unity3D with UnJect
---

### What is UnJect?

I found myself writting a lot of boilerplate code to wire my objects together.  The alternative is to use the editor to drag everything into place.  I found this cluttered up the GUI for our content creators, was error prone, and was too time consuming to create clean custom Editors for everything.  I also didn't want to have to wrestle with some big fancy DI framework and get it to work with Unity for such simple tasks.  UnJect uses a simple set of Attributes to transform these...

- `GameObject.Find`
- `GameObject.FindWithTag`
- `GameObject.FindObjectOfType`
- `GameObject.FindObjectsOfType`
- `Transform.Find`
- `Component.GetComponent`
- `Component.GetComponents`
- `Component.GetComponentInChildren`

Into this that you markup your fields with...

- `[InjectByName]`
- `[InjectByTag]`
- `[InjectByType]`
- `[InjectByLayer]`

UnJect is also a simple event system that can replace the `SendMessage` family of methods.  This has a couple benefits:

1. UnJect can send *global* events to any object, not just within the immediate GameObject hierarchy.
2. Events can have any number of arguments.
3. Event listeners are marked up with `[InjectEventByName]` for readability and so you don't mistakenly make methods receive same-named events.
4. Event listeners are cached for faster dispatching.

### Examples

Using the various Inject tags to markup dependencies in a Component class

```csharp
using UnJect;

[RequireComponent(typeof(Injector))]
class Player : MonoBehaviour {
	
	// Finds the first object named "footstepAudio" (case-insensitive) or in any child
	[InjectByName] AudioSource footstepAudio;

	// Alternative syntax - different name than variable
	[InjectByName("Footstep_Audio")] footsteps;

	// Same as InjectByName, but finds ANY object by name (not just in this GameObject hierarchy)
	[InjectSharedByName] Transform spawnPoint;

	// UnJect recognizes Lists and Arrays and finds all matching objects
	[InjectByName] Collider[] hitbox;

	// Finds the first object of type Weapon in this GameObject or in any child
	[InjectByType] Weapon weapon;

	// Starts the search at the root of this GameObject hierarchy, useful if this component is deeply nested
	[InjectByType(SearchParents = true)] CharacterController controller;

	// Similar search using tags
	[InjectByTag("Hitbox")] Collider[] allTaggedHitboxes;

	// Turning recursive searching off
	[InjectByTag("Hitbox", Recursive = false)] List<Collider> hitboxesInImmediateChildren;

	// Similar search using layers
	[InjectByLayer("Trigger")] Collider triggerCollider;

	// Mark objects as optional to prevent an error if the dependency could not be found
	[InjectByName(Optional = true)] GameObject optionalLootDrop;

	void OnInject() {
		// Called once all dependencies have been injected into this component
		// Use this to do further initialization
		// Ran sometime during Start()
	}
}
```

Using the event system

```csharp
using UnJect;

[RequireComponent(typeof(Injector))]
class MyEventDemo : MonoBehaviour {
	
	[InjectEventByName]
	void OnMyEvent() {
		// Listens for the "OnMyEvent" event
	}

	[InjectEventByName]
	void OnMyEvent2(int one, string two, object three) {
		// Listeners can have any number of parameters
	}

	[InjectEventByName("OnMyEvent")]
	void Event_OnMyEvent() {
		// You can specify a different event name than the method name
	}

	void OnInject() {
		// Sends "OnMyEvent" to every component on this GameObject
		this.SendEvent("OnMyEvent");

		// Sends "OnMyEvent" to every component on this GameObject
		// and to all components on all nested children of this GameObject
		this.SendEventDown("OnMyEvent");

		// Sends "OnMyEvent" to every component on this GameObject
		// and to all components on all direct parents of this GameObject
		this.SendEventUp("OnMyEvent");

		// Multi-argument example
		this.SendEvent("OnMyEvent2", 1, "two", new object());

		// Sends "OnMyEvent" to any GameObject (does not have to be in this GameObject hierarchy)
		this.SendGlobalEvent("OnMyEvent");
	}
}
```

---

### What's Left

- [x] Inject by GameObject name
- [x] Inject by GameObject layer
- [x] Inject by GameObject tag
- [x] Inject by Component type
- [x] Inject "shared" (singleton) objects
- [x] Inject all matching components into a List or Array container
- [x] Redo `SendMessage` methods with global events and multi-argument support
- [ ] Events filters 
- [ ] Search by name with Regex
- [ ] Polymorphic Array and List types
- [ ] Custom Editor for the Injector that will show what dependencies each component has
- [ ] Custom Editor for the Injector that will show what events each component is listening to
- [ ] Custom EditorWindow that will show where events are being dispatched and to where
- [ ] Tests

---

[MIT License](http://paulmoore.mit-license.org/)
