using System.Numerics;
using Silk.NET.SDL;

namespace Integrity.Interface;
public interface IInputManager : IService
{
    Vector2 MousePosition { get; }
    unsafe void ProcessInput(Event ev);
    bool IsKeyDown(Scancode scancode);
}