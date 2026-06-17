using System.Text.Json;
using BattleTanks_Backend.Application.DTOs.Auth;
using BattleTanks_Backend.Application.Interfaces;
using BattleTanks_Backend.Domain.Entities;
using BattleTanks_Backend.Domain.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace BattleTanks_Backend.Application.Services;

public class AuthService : IAuthService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly FirebaseAuth _firebaseAuth;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _webApiKey;
    private readonly string _projectId;

    public AuthService(IPlayerRepository playerRepository, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _playerRepository = playerRepository;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _webApiKey = configuration["Firebase:WebApiKey"] ?? "";
        _projectId = configuration["Firebase:ProjectId"] ?? "";

        if (FirebaseApp.DefaultInstance == null)
        {
            var credentialsPath = configuration["Firebase:CredentialsPath"];
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(credentialsPath)
            });
        }

        _firebaseAuth = FirebaseAuth.DefaultInstance;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        try
        {
            if (await _playerRepository.ExistsByUsernameAsync(dto.Username))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "El nombre de usuario ya está en uso"
                };
            }

            if (await _playerRepository.ExistsByEmailAsync(dto.Email))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "El email ya está registrado"
                };
            }

            var signUpUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_webApiKey}";

            var signUpRequest = new FirebaseLoginRequest
            {
                Email = dto.Email,
                Password = dto.Password,
                ReturnSecureToken = true
            };

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsJsonAsync(signUpUrl, signUpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<FirebaseErrorResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var errorMessage = errorResponse?.Error?.Message switch
                {
                    "EMAIL_EXISTS" => "El email ya está registrado en Firebase",
                    "WEAK_PASSWORD" => "La contraseña es muy débil (mínimo 6 caracteres)",
                    "INVALID_EMAIL" => "El email no es válido",
                    _ => $"Error de Firebase: {errorResponse?.Error?.Message}"
                };

                return new AuthResponseDto
                {
                    Success = false,
                    Message = errorMessage
                };
            }

            var firebaseResponse = JsonSerializer.Deserialize<FirebaseSignUpResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (firebaseResponse == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Error al procesar respuesta de Firebase"
                };
            }

            await _firebaseAuth.UpdateUserAsync(new UserRecordArgs
            {
                Uid = firebaseResponse.LocalId,
                DisplayName = dto.Username
            });

            var player = new Player
            {
                FirebaseUid = firebaseResponse.LocalId,
                Username = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CreatedAt = DateTime.UtcNow
            };

            var createdPlayer = await _playerRepository.CreateAsync(player);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Usuario registrado exitosamente. Inicia sesión para obtener tu token.",
                Token = null,
                Player = new PlayerAuthDto
                {
                    Id = createdPlayer.Id,
                    FirebaseUid = createdPlayer.FirebaseUid,
                    Username = createdPlayer.Username,
                    Email = createdPlayer.Email,
                    FirstName = createdPlayer.FirstName,
                    LastName = createdPlayer.LastName,
                    TotalScore = 0,
                    GamesPlayed = 0,
                    Wins = 0
                }
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Error en el registro: {ex.Message}"
            };
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        try
        {
            var verifyPasswordUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_webApiKey}";

            var loginRequest = new FirebaseLoginRequest
            {
                Email = dto.Email,
                Password = dto.Password,
                ReturnSecureToken = true
            };

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsJsonAsync(verifyPasswordUrl, loginRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<FirebaseErrorResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var errorMessage = errorResponse?.Error?.Message switch
                {
                    "EMAIL_NOT_FOUND" => "El email no está registrado",
                    "INVALID_PASSWORD" => "Contraseña incorrecta",
                    "USER_DISABLED" => "La cuenta está deshabilitada",
                    "INVALID_LOGIN_CREDENTIALS" => "Credenciales inválidas",
                    _ => "Credenciales inválidas"
                };

                return new AuthResponseDto
                {
                    Success = false,
                    Message = errorMessage
                };
            }

            var firebaseResponse = JsonSerializer.Deserialize<FirebaseAuthResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (firebaseResponse == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Error al procesar respuesta de Firebase"
                };
            }

            var player = await _playerRepository.GetByEmailAsync(dto.Email);

            if (player == null)
            {
                var firebaseUser = await _firebaseAuth.GetUserAsync(firebaseResponse.LocalId);
                player = new Player
                {
                    FirebaseUid = firebaseResponse.LocalId,
                    Username = firebaseUser.DisplayName ?? dto.Email.Split('@')[0],
                    Email = dto.Email,
                    CreatedAt = DateTime.UtcNow
                };
                player = await _playerRepository.CreateAsync(player);
            }

            player.LastLogin = DateTime.UtcNow;
            await _playerRepository.UpdateAsync(player);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login exitoso",
                Token = firebaseResponse.IdToken,
                Player = new PlayerAuthDto
                {
                    Id = player.Id,
                    FirebaseUid = player.FirebaseUid,
                    Username = player.Username,
                    Email = player.Email,
                    FirstName = player.FirstName,
                    LastName = player.LastName,
                    TotalScore = player.TotalScore,
                    GamesPlayed = player.GamesPlayed,
                    Wins = player.Wins
                }
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Error en el login: {ex.Message}"
            };
        }
    }

    public async Task<PlayerAuthDto?> GetCurrentPlayerAsync(string firebaseUid)
    {
        var player = await _playerRepository.GetByFirebaseUidAsync(firebaseUid);

        if (player == null) return null;

        return new PlayerAuthDto
        {
            Id = player.Id,
            FirebaseUid = player.FirebaseUid,
            Username = player.Username,
            Email = player.Email,
            FirstName = player.FirstName,
            LastName = player.LastName,
            TotalScore = player.TotalScore,
            GamesPlayed = player.GamesPlayed,
            Wins = player.Wins
        };
    }

    public async Task<PlayerAuthDto?> GetOrCreatePlayerFromFirebaseAsync(string firebaseUid, string email, string username)
    {
        var existingPlayer = await GetCurrentPlayerAsync(firebaseUid);
        if (existingPlayer != null)
        {
            return existingPlayer;
        }

        var player = new Player
        {
            FirebaseUid = firebaseUid,
            Username = username,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        var createdPlayer = await _playerRepository.CreateAsync(player);

        return new PlayerAuthDto
        {
            Id = createdPlayer.Id,
            FirebaseUid = createdPlayer.FirebaseUid,
            Username = createdPlayer.Username,
            Email = createdPlayer.Email,
            TotalScore = 0,
            GamesPlayed = 0,
            Wins = 0
        };
    }

    public async Task<string?> VerifyTokenAsync(string token)
    {
        try
        {
            var decodedToken = await _firebaseAuth.VerifyIdTokenAsync(token);
            return decodedToken.Uid;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token verification failed: {ex.Message}");
            return null;
        }
    }
}
