# Integrity 2D - SDL2/OpenGL Engine

A work-in-progress, modern 2D graphics engine built entirely in **C#** to explore and implement the **OpenGL 3.3 Core Profile** pipeline from the ground up.

Integrity utilizes **SDL2** (via Silk.NET) solely for cross-platform window management and input handling, separating the rendering concerns completely into a custom OpenGL rendering pipeline.

[![Integrity 2D Documentation](https://img.shields.io/badge/Documentation-Integrity%202D-blue.svg)](https://ezroot.github.io/Integrity2D/)

---

## Core Architecture & Design

The engine is built on a strong **Service-Oriented Architecture (SOA)**, relying on the **Service Locator** pattern to manage engine subsystems. This promotes **loose coupling** and **testability** across all major components.

### Key Components

* **Service Locator:** The central `Service.Get<T>()` mechanism is used to resolve all engine dependencies, including:
    * `IAssetManager`: Handles asset caching and loading.
    * `IRenderPipeline`: Manages all OpenGL interaction.
    * `ICameraManager`: Controls the current world view.
* **Main Loop (`Engine.Run()`):** Explicitly handles timing (`deltaTime`), profiling (`IProfiler`), and delegates work to the separate `HandleInput()`, `Update()`, and `Render()` methods.

---

## Modern Rendering Pipeline

Integrity's greatest focus is on high-performance 2D rendering using modern graphics techniques.

### Core Features

* **OpenGL 3.3 Core:** Enforces the use of modern VAOs, VBOs, and programmable shaders (GLSL 330).
* **Instanced Rendering (Batching):** All sprite rendering is performed using hardware **instancing**. During the `Render()` loop, objects are grouped by their **`GLTexture`** into the `m_RenderingBatchMap`, and then drawn in a single, highly efficient `DrawSpritesInstanced` call.
* **2D Coordinate System:** Implements a top-left origin (Y-down) orthographic projection, ideal for 2D development.
* **Asset Pipeline:** Dedicated managers handle loading image files (via **StbImageSharp**) and converting them directly into GPU-ready **`GLTexture`** resources.
* **Debugging/GUI:** Integrated **ImGui** (via `IImGuiPipeline`) for real-time debugging tools and engine statistics visualization (FPS, Profiler data).

---

## Getting Started

This project is structured as a series of pipelines and managers.

Dive deeper into the Integrity Engine architecture, detailed component guides, and advanced usage examples in the **official documentation**:
[**Integrity 2D Documentation (ezroot.github.io/Integrity2D/)**](https://ezroot.github.io/Integrity2D/)

That's an excellent, detailed README.md for your engine! To best showcase your new IntegrityGameDemo and its screenshot without diluting the engine's architectural focus, the ideal place to add it is in a new section focused on Usage and Examples.

I recommend adding a section titled "4. 🕹️ Usage & Examples" or "4. Game Demo" immediately before the "Contributing" section.

Here is the revised structure with the new section and its content:

Recommended Placement and Content

Place the new section right after "Getting Started" and before "Contributing".

New Section:

Markdown

---

## 4. Usage & Examples

The best way to understand the engine's minimal boilerplate and high-performance capabilities is through a working game project.

### IntegrityGameDemo Showcase

Our primary example project showcases scene setup, input handling, and the engine's draw call efficiency by instancing over 10,000 sprites.

<img src="Screenshots/screenshot_0.png" width="600" alt="IntegrityGameDemo Screenshot showing 10,000 sprites on screen."/>

* **View the Demo Repository:** [https://github.com/EZroot/IntegrityGameDemo](https://github.com/EZroot/IntegrityGameDemo)
* **Key Features Demonstrated:**
    * **Service Locator:** Accessing `IInputManager`, `ICameraManager`, and `IGameObjectFactory`.
    * **Asset Management:** Loading assets via relative paths.
    * **Stress Testing:** High-performance instanced rendering of 10,000+ objects.
    * **Camera Control:** Simple WASD movement implemented in the game loop.

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

We welcome contributions to the Integrity 2D Engine! Whether you are fixing a bug, improving documentation, or adding a new feature, your help is appreciated.

### How to Contribute

1.  **Fork** the repository.
2.  **Clone** your forked repository locally.
3.  Create a new feature branch (`git checkout -b feature/my-new-feature`).
4.  Make your changes and ensure the existing tests (if any) pass.
5.  **Commit** your changes with clear, descriptive commit messages.
6.  **Push** your branch to your fork (`git push origin feature/my-new-feature`).
7.  Open a **Pull Request (PR)** against the `main` branch of this repository, describing the changes you've made.

### Code Style

* We primarily follow **C# convention** and general best practices. (But we prefer **m_PascalCase** over the conventional **_camelCase** for private fields that are object references or services)
* **Service Locator** usage should be limited to `Service.Get<T>()` to retrieve engine subsystems.* Please ensure code is well-commented, especially for complex rendering or architecture logic.

For more detailed contribution guidelines, please see the separate **[CONTRIBUTING.md](CONTRIBUTING.md)** file in the repository (recommended).

## Found a Bug?

If you encounter a bug or unexpected behavior:

1.  **Check Existing Issues:** Search the repository's [Issues] page to see if the problem has already been reported.
2.  **Open a New Issue:** If it's a new issue, please open a new bug report.
    * **Be Descriptive:** Include a clear and concise title.
    * **Steps to Reproduce:** Detail the exact steps to recreate the bug.
    * **Expected vs. Actual Behavior:** Explain what you expected to happen and what actually occurred.
    * **Environment:** Note your operating system and .NET SDK version.