using System.Net.Http.Headers;
using System.Net.Http.Json;
using proyectosena.DTOs.User;
using proyectosena.Models;

namespace proyectosena.Tests.Infrastructure
{
    /// <summary>
    /// Lo que hace el frontend, escrito una vez: registrarse, entrar, llamar con token.
    /// </summary>
    /// <remarks>
    /// Los cuerpos van como objetos anónimos en camelCase, igual que los manda el
    /// navegador, y no con los DTO de C#: así una prueba se rompe si cambia el
    /// contrato JSON, no solo si cambia una clase.
    /// </remarks>
    public static class Api
    {
        public const string Password = "Password123*";

        private static int _lastIp;

        /// <summary>Cliente HTTP con una IP inventada que no comparte ninguna otra prueba.</summary>
        public static HttpClient NewClient(this RecyRouteApiFactory api)
        {
            var client = api.CreateClient();
            var n = Interlocked.Increment(ref _lastIp);
            client.DefaultRequestHeaders.Add(RecyRouteApiFactory.ClientIpHeader, $"10.0.{n / 256}.{n % 256}");
            return client;
        }

        public static string NewEmail() => $"test-{Guid.NewGuid():N}@recyroute.test";

        // Diez cifras: cabe en el máximo de 20 y no choca con el índice único del documento
        public static string NewDocumentNumber() => Random.Shared.NextInt64(1_000_000_000, 10_000_000_000).ToString();

        public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string? documentNumber = null)
            => client.PostAsJsonAsync("/api/auth/Register", new
            {
                idDocumentType = SeedIds.DocumentTypes.CedulaCiudadania,
                name = "Ana",
                lastName = "Test",
                documentNumber = documentNumber ?? NewDocumentNumber(),
                phoneNumber = "3001234567",
                address = "Calle 1 # 2-3",
                email,
                password = Password
            });

        public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password = Password)
            => client.PostAsJsonAsync("/api/auth/Login", new { email, password });

        public static Task<HttpResponseMessage> SendWithTokenAsync(
            this HttpClient client, HttpMethod method, string path, string token, object? body = null)
        {
            var request = new HttpRequestMessage(method, path)
            {
                Content = body is null ? null : JsonContent.Create(body)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client.SendAsync(request);
        }

        /// <summary>Una petición cualquiera que exige sesión: 200 si el token vale, 401 si no.</summary>
        public static Task<HttpResponseMessage> ProbeTokenAsync(this HttpClient client, string token)
            => client.SendWithTokenAsync(HttpMethod.Get, "/api/notification/GetUnreadCount", token);

        /// <summary>Exige una respuesta correcta y lee su cuerpo JSON.</summary>
        public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        public static Task<AuthResponseDto> ReadSessionAsync(this HttpResponseMessage response)
            => response.ReadAsync<AuthResponseDto>();

        /// <summary>Ciudadano registrado y con el correo confirmado; devuelve su sesión.</summary>
        public static async Task<AuthResponseDto> ConfirmedCitizenAsync(this RecyRouteApiFactory api, HttpClient client, string email)
        {
            (await client.RegisterAsync(email)).EnsureSuccessStatusCode();

            var code = api.Emails.LastCode(email, EmailKind.EmailConfirmation);
            var confirmation = await client.PostAsJsonAsync("/api/auth/verify-email", new { email, code });

            return await confirmation.ReadSessionAsync();
        }
    }
}
