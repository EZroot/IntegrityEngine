using Integrity.Assets;
using Integrity.Components;

namespace Integrity.Objects;
public class SpriteObject : GameObject
{
    public SpriteComponent Sprite { get; }

    public SpriteObject(string name, Texture Texture) : base(name)
    {
        Sprite = new SpriteComponent(Texture);
        AddComponent(Sprite);
    }
}