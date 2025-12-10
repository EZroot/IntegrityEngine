using System.Numerics;
using Integrity.Assets;
using Integrity.Interface;
using Integrity.Utils;

namespace Integrity.Components;

public class SpriteComponent : IComponent
{
    private Vector2 m_Pivot = new Vector2(0.5f, 0.5f);

    public Texture Texture { get; }
    public Rect SourceRect { get; set; }
    public Vector2 Pivot => m_Pivot;
    public Vector4 Color { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f); 
    public int Layer { get; }

    public SpriteComponent(Texture texture, int layer = 0)
    {
        Texture = texture ?? throw new ArgumentNullException(nameof(texture), "Texture resource must be provided.");
        SourceRect = new Rect(0, 0, texture.Width, texture.Height);
        Layer = layer;
    }

    public SpriteComponent(Texture texture, Vector2 pivot, int layer = 0)
    {
        Texture = texture ?? throw new ArgumentNullException(nameof(texture), "Texture resource must be provided.");
        SourceRect = new Rect(0, 0, texture.Width, texture.Height);
        m_Pivot = pivot;
        Layer = layer;
    }
}