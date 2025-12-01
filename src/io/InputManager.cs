using Silk.NET.SDL;
using System.Numerics;

public class InputManager : IInputManager
{
    private readonly Dictionary<Scancode, bool> m_KeyStates = new();
    private readonly Dictionary<byte, bool> m_MouseButtonStates = new();
    private Vector2 m_MousePosition = Vector2.Zero;

    /// <summary>
    /// Processes a single SDL event and updates the internal key states.
    /// This is called in the Engine's HandleInput() loop for every polled event.
    /// </summary>
    public unsafe void ProcessInput(Event ev)
    {
        switch ((EventType)ev.Type)
        {
            case EventType.Keydown:
            case EventType.Keyup:
                {
                    Scancode scancode = ev.Key.Keysym.Scancode;
                    bool isDown = (EventType)ev.Type == EventType.Keydown;

                    m_KeyStates[scancode] = isDown;
                    break;
                }

            case EventType.Mousebuttondown:
            case EventType.Mousebuttonup:
                {
                    byte buttonId = ev.Button.Button;
                    bool isDown = (EventType)ev.Type == EventType.Mousebuttondown;

                    m_MouseButtonStates[buttonId] = isDown;
                    break;
                }

            case EventType.Mousemotion:
                {
                    m_MousePosition = new Vector2(ev.Motion.X, ev.Motion.Y);
                    break;
                }
        }
    }

    /// <summary>
    /// Checks if a key is currently in the down state. 
    /// This is the method your game logic calls in the Update loop.
    /// </summary>
    /// <param name="scancode">The key to check.</param>
    /// <returns>True if the key is pressed, false otherwise.</returns>
    public bool IsKeyDown(Scancode scancode)
    {
        return m_KeyStates.TryGetValue(scancode, out bool isDown) && isDown;
    }
}