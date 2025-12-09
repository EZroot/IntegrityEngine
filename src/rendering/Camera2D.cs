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
}