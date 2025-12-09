# Integrity 2D - SDL2/OpenGL Engine

A work-in-progress, modern 2D graphics engine built entirely in **C#** to explore and implement the **OpenGL 3.3 Core Profile** pipeline from the ground up.

[![Integrity 2D Documentation](https://img.shields.io/badge/Documentation-Integrity%202D-blue.svg)](https://ezroot.github.io/Integrity2D/)

---

### IntegrityGameDemo Showcase

Our primary example project showcases scene setup, input handling, and the engine's draw call efficiency by instancing over 10,000 sprites.

<img src="Screenshots/screenshot_0.png" width="600" alt="IntegrityGameDemo Screenshot showing 10,000 sprites on screen."/>

* **View the Demo Repository:** [https://github.com/EZroot/IntegrityGameDemo](https://github.com/EZroot/IntegrityGameDemo)

---

### Requirements

1.  **.NET 8+ SDK**
2.  **SDL2 runtime libraries** (managed via Silk.NET/your environment setup)

### Example: Scene Setup

New game objects are instantiated via the factory and registered with the active scene to be rendered automatically.

```csharp
// Inside Engine.Initialize() (Will become IGame.Initialize() later)
// Create a new scene
Scene defaultScene = new Scene("DefaultScene");

// Get the factory service from the engine's Service Locator
var factory = Service.Get<IGameObjectFactory>();
var sceneManager = Service.Get<ISceneManager>();

// Load the asset and create the SpriteObject
// The factory handles loading the texture via IAssetManager internally.
string assetPath = "/path/to/your/assets/my_sprite.png";
var playerSprite = factory.CreateSpriteObject("PlayerCharacter", assetPath);

// Adjust the object's transform (position, scale, rotation)
playerSprite.Transform.X = 250.0f;
playerSprite.Transform.Y = 150.0f;
playerSprite.Transform.ScaleX = 0.5f;

// Register the object with the current scene
// Once registered, the Engine's main Render() loop will automatically draw it batched and instanced.
sceneManager.CurrentScene.RegisterGameObject(playerSprite);

// Load and set the scene
m_SceneManager.AddScene(defaultScene);
m_SceneManager.LoadScene(defaultScene);
```
---

## Contributing

For more detailed contribution guidelines, please see the separate **[CONTRIBUTING.md](CONTRIBUTING.md)** file in the repository.
