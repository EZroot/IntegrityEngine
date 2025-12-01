
public class SpriteComponent : IComponent
{
    public GLTexture Texture { get; }
    public Rect SourceRect { get; set; }

    public SpriteComponent(GLTexture texture)
    {
        Texture = texture ?? throw new ArgumentNullException(nameof(texture), "Texture resource must be provided.");
        SourceRect = new Rect(0, 0, texture.Width, texture.Height);
    }
}