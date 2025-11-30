using Silk.NET.SDL;

public interface IInputManager : IService
{
    unsafe void ProcessInput(Event ev);
    bool IsKeyDown(Scancode scancode);
}