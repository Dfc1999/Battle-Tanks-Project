namespace BattleTanks_Backend.Application.DTOs.Auth;

public class FirebaseLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool ReturnSecureToken { get; set; } = true;
}

public class FirebaseAuthResponse
{
    public string IdToken { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string ExpiresIn { get; set; } = string.Empty;
    public string LocalId { get; set; } = string.Empty;
    public bool Registered { get; set; }
}

public class FirebaseSignUpResponse
{
    public string IdToken { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string ExpiresIn { get; set; } = string.Empty;
    public string LocalId { get; set; } = string.Empty;
}

public class FirebaseErrorResponse
{
    public FirebaseError? Error { get; set; }
}

public class FirebaseError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
}
