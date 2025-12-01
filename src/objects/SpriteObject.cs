public class SpriteObject : GameObject
{
    public SpriteComponent Sprite { get; }

    public SpriteObject(GLTexture glTexture) : base()
    {
        Sprite = new SpriteComponent(glTexture);
        AddComponent(Sprite);
    }
}