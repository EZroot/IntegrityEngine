using Integrity.Interface;

namespace Integrity.Components;
public class TransformComponent : IComponent
{
    public float X, Y;
    public float Rotation; // In Radians
    public float ScaleX, ScaleY;

    public TransformComponent()
    {
        X = 0;
        Y = 0;
        Rotation = 0;
        ScaleX = 1;
        ScaleY = 1;
    }
}