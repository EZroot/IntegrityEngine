using Integrity.Assets;
using Integrity.Components;

namespace Integrity.Objects;
public class SpriteObject : GameObject
{
    public SpriteComponent Sprite { get; }

    public SpriteObject(string name, Texture texture, int layer = 0) : base(name)
    {
        Sprite = new SpriteComponent(texture, layer);
        AddComponent(Sprite);
    }
}