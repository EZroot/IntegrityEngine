using System.Numerics;

namespace Integrity.Rendering;
public class Camera2D
{
    public Guid Id { get; }
    public string Name { get; }
    public Vector2 Position { get; set; } = Vector2.Zero;
    public float Zoom { get; set; } = 1.0f;

    private int m_Width;
    private int m_Height;

    public Camera2D(string name, int width, int height)
    {
        Id = Guid.NewGuid();
        Name = name;
        m_Width = width;
        m_Height = height;
    }

    /// <summary>
    /// Updates the camera's viewport dimensions (called on window resize).
    /// </summary>
    public void UpdateViewportSize(int width, int height)
    {
        m_Width = width;
        m_Height = height;
    }

    /// <summary>
    /// Calculates the combined View * Projection Matrix.
    /// </summary>
    public Matrix4x4 GetViewProjectionMatrix()
    {
        float scaledWidth = m_Width / Zoom;
        float scaledHeight = m_Height / Zoom;

        Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(
            0,
            scaledWidth,
            scaledHeight,
            0,
            -1.0f,
            1.0f
        );

        var snappedX = (int)Position.X;
        var snappedY = (int)Position.Y;

        Matrix4x4 view = Matrix4x4.CreateTranslation(-snappedX, -snappedY, 0);
        return view * projection;
    }

    /// <summary>
    /// Converts a screen pixel coordinate to a world coordinate, respecting zoom and position.
    /// </summary>
    /// <param name="screenPosition">The raw pixel screen position.</param>
    /// <returns>The position in world space.</returns>
    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        Matrix4x4 viewProjection = GetViewProjectionMatrix();
        if (Matrix4x4.Invert(viewProjection, out Matrix4x4 inverseMatrix))
        {
            var clip = new Vector3(screenPosition.X / m_Width * 2f - 1f, 1f - screenPosition.Y / m_Height * 2f, 0f);
            var worldPosition4 = Vector4.Transform(clip, inverseMatrix);
            var worldPosition = new Vector2(worldPosition4.X / worldPosition4.W, worldPosition4.Y / worldPosition4.W);
            return worldPosition;
        }

        return Vector2.Zero; 
    }
}