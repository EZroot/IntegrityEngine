# 🤝 Contributing to Integrity 2D Engine

We welcome and appreciate all forms of contribution to the Integrity 2D Engine, from code and documentation improvements to bug reports and feature suggestions!

By contributing, you agree to abide by the project's Code of Conduct.

## Found a Bug?

If you encounter a bug or unexpected behavior:

1.  **Check Existing Issues:** Search the repository's [Issues] page to see if the problem has already been reported.
2.  **Open a New Issue:** If it's a new issue, please open a new bug report.
    * **Be Descriptive:** Include a clear and concise title.
    * **Steps to Reproduce:** Detail the exact steps to recreate the bug.
    * **Expected vs. Actual Behavior:** Explain what you expected to happen and what actually occurred.
    * **Environment:** Note your operating system and .NET SDK version.

## Suggesting a Feature

We are actively developing the engine, and new feature suggestions are welcome.

1.  **Check Existing Issues:** Look for similar features in open or closed issues.
2.  **Open a New Issue:** Use the feature request template (if available) or clearly explain:
    * **The Goal:** What problem does this feature solve?
    * **Technical Details (Optional):** Suggest how it might fit into the existing **Service-Oriented Architecture (SOA)** (e.g., as a new `IService`).

## Contribution Workflow (Code)

To contribute code, please follow the standard GitHub **Fork & Pull Request** workflow:

1.  **Fork** the `ezroot/Integrity2D` repository to your own GitHub account.
2.  **Clone** your fork locally:
    ```bash
    git clone [https://github.com/EZroot/Integrity2D.git](https://github.com/EZroot/Integrity2D.git)
    ```
3.  **Create a Branch:** Create a dedicated branch for your feature or fix. Use descriptive names like `bugfix/issue-123` or `feature/add-new-input-system`.
    ```bash
    git checkout -b feature/my-new-input
    ```
4.  **Make Changes:** Write your code, tests, and documentation updates.
5.  **Test:** Ensure the engine builds correctly and all existing functionality remains intact.
6.  **Commit:** Commit your changes with clear, descriptive commit messages.
    ```bash
    git commit -m "feat: Implement a dedicated keyboard input manager"
    ```
7.  **Push:** Push the changes to your fork.
    ```bash
    git push origin feature/my-new-input
    ```
8.  **Open a Pull Request (PR):** Navigate to the original `ezroot/Integrity2D` repository and open a PR targeting the `main` branch. Provide a brief summary of your changes.

## Code Style Guidelines

* **Language:** C#
* **Conventions:** Follow standard Microsoft C# coding conventions and best practices. **Note:** Private fields that are object references or services should use the **m_PascalCase** format (e.g., `m_SdlApi`), deviating from the more conventional `_camelCase`.
* **Architecture:** Maintain the **Service-Oriented Architecture (SOA)**. If you need engine functionality, use the **Service Locator** (`Service.Get<T>()`) to retrieve the correct subsystem interface.
* **Clarity:** Write clean, self-documenting code. Use comments strategically, especially for complex algorithms related to the OpenGL pipeline or scene management.
* **Formatting:** Use consistent indentation and brace style (K&R or Allman, depending on the current project style).