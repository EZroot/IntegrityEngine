using Integrity.Assets;
using Integrity.Interface;
using Integrity.Utils;

namespace Integrity.Components;

public class SpriteComponent : IComponent
{
    public Texture Texture { get; }
    public Rect SourceRect { get; set; }

    public SpriteComponent(Texture texture)
    {
        Texture = texture ?? throw new ArgumentNullException(nameof(texture), "Texture resource must be provided.");
        SourceRect = new Rect(0, 0, texture.Width, texture.Height);
    }
}