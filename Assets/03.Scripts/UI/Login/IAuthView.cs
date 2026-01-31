public interface IAuthView
{
    string UserName { get; }
    string Email { get; }
    string Password { get; }
    string PasswordConfirm { get; }

    void ShowMessage(string message);
    void CloseMessage();
    void SetLoading(bool isLoading);
    void ShowLoginPanel();
    void ShowRegisterPanel();
}