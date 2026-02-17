public interface IAuthView
{
    string UserName { get; }
    string Email { get; }
    string Password { get; }
    string PasswordConfirm { get; }

    void SetLoading(bool isLoading);
    void ShowLoginPanel();
    void ShowRegisterPanel();
}