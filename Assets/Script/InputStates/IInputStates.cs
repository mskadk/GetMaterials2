public interface IInputState
{
    void OnEnter(InputManager context);
    void OnUpdate(InputManager context);
    void OnExit(InputManager context);
}
