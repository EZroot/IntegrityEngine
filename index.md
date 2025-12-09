# Integrity 2D API Documentation

Welcome to the **Integrity 2D API Reference**. This documentation details the complete public surface area of the Integrity Engine, a modular, service-oriented C# game development platform.

---

## Key Systems and Navigation

The Integrity Engine is structured around several core services. Use the **sidebar** to browse the reference material, organized by **Namespaces** (the engine's major systems).

| Namespace | Focus Area | Key Interfaces/Classes |
| :--- | :--- | :--- |
| **Integrity.Core** | Engine startup, services, and the main game loop (`Engine`). | `Engine`, `Service`, `IGame` |
| **Integrity.Assets** | Asset loading, caching, and management. | `IAssetManager` |
| **Integrity.Rendering** | Graphics pipeline, textures, and rendering utilities. | `IRenderPipeline`, `Texture`, `Camera2D` |
| **Integrity.Scenes** | Game scene management and hierarchy of objects. | `ISceneManager`, `Scene` |
| **Integrity.Interface** | Component factory and basic game object structure. | `IGameObjectFactory`, `ISpriteObject` |

---

## Example: Loading a Sprite Object

To get started quickly, the most common first task is loading and displaying a sprite. The engine handles this using the **`IGameObjectFactory`** and **`ISceneManager`**.

This pattern is typically executed within your main `IGame.Initialize()` method or during scene setup:

```csharp
// 1. Get the factory service from the engine's Service Locator
var factory = Service.Get<IGameObjectFactory>();
var sceneManager = Service.Get<ISceneManager>();

// 2. Load the asset and create the SpriteObject
// The factory handles loading the texture via IAssetManager internally.
string assetPath = "/path/to/your/assets/my_sprite.png";
var playerSprite = factory.CreateSpriteObject("PlayerCharacter", assetPath);

// 3. Adjust the object's transform (position, scale, rotation)
playerSprite.Transform.X = 250.0f;
playerSprite.Transform.Y = 150.0f;
playerSprite.Transform.ScaleX = 0.5f;

// 4. Register the object with the current scene
// Once registered, the Engine's main Render() loop will automatically draw it.
sceneManager.CurrentScene.RegisterGameObject(playerSprite);
```

When objects are registered in that scene, they're then drawn batched and instanced.

## Conventions

| **Public API** |: Only Public and Protected members are included in this documentation. Internal implementation details are excluded.

| **Assembly** |: All core classes reside in Integrity.dll.

| **Syntax** |: All documentation adheres to standard C# XML documentation conventions.