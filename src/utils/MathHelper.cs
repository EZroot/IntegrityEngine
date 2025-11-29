public class MathHelper
{
    public static System.Numerics.Matrix4x4 Translation(float x, float y, float width, float height)
    {
        System.Numerics.Matrix4x4 model = System.Numerics.Matrix4x4.Identity;
        System.Numerics.Matrix4x4 scaleMatrix = System.Numerics.Matrix4x4.CreateScale(width, height, 1.0f);
        model = System.Numerics.Matrix4x4.Multiply(model, scaleMatrix);
        System.Numerics.Matrix4x4 translateMatrix = System.Numerics.Matrix4x4.CreateTranslation(x, y, 0.0f);
        model = System.Numerics.Matrix4x4.Multiply(model, translateMatrix);
        return model;
    }
}