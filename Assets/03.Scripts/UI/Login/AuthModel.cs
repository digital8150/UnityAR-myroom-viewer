using System;

[Serializable]
public class RegisterRequest
{
    public string name;
    public string email;
    public string password;
}

[Serializable]
public class LoginRequest
{
    public string email;
    public string password;
}

[Serializable]
public class LoginResponse
{
    public string token;
}

public class JWTToken
{
    public static string Token;
}