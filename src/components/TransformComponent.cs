public class TransformComponent : IComponent
{
    public float X, Y;
    public float Rotation;
    public float ScaleX, ScaleY;

    public TransformComponent()
    {
        X = 0;
        Y = 0;
        Rotation = 0;
        ScaleX = 1;
        ScaleY = 1;
    }

    public void Shutdown()
    {
        // No resources to clean up
    }
}