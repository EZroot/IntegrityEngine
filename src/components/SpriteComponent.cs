using System.Numerics;
using Integrity.Assets;
using Integrity.Interface;
using Integrity.Utils;

namespace Integrity.Components;

public class SpriteComponent : IComponent
{
    public Texture Texture { get; }
    public Rect SourceRect { get; set; }
    public Vector2 Pivot => m_Pivot;
    private Vector2 m_Pivot = new Vector2(0.5f, 0.5f);

    public SpriteComponent(Texture texture)
    {
        Texture = texture ?? throw new ArgumentNullException(nameof(texture), "Texture resource must be provided.");
        SourceRect = new Rect(0, 0, texture.Width, texture.Height);
    }

    public SpriteComponent(Texture texture, Vector2 pivot)
    {
        Texture = texture ?? throw new ArgumentNullException(nameof(texture), "Texture resource must be provided.");
        SourceRect = new Rect(0, 0, texture.Width, texture.Height);
        m_Pivot = pivot;
    }
}