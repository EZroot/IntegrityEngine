public class SpriteRenderSystem
{
    private readonly List<SpriteObject> m_SpriteObjectList = new(); 
    
    public List<SpriteObject> SpriteObjectList => m_SpriteObjectList;
    public void RegisterObject(GameObject obj)
    {
        if (obj is SpriteObject spriteObj)
        {
            m_SpriteObjectList.Add(spriteObj);
        }
    }
}