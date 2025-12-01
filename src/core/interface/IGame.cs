public interface IGame
{
    void Initialize();
    void Update(float deltaTime);
    void Render();
    void Cleanup();
}