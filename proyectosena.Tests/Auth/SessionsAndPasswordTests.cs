using System.Net;
using System.Net.Http.Json;
using proyectosena.Tests.Infrastructure;

namespace proyectosena.Tests.Auth
{
    /// <summary>
    /// Cerrar sesión, cambiar la contraseña y recuperarla: que un token deje de
    /// valer cuando tiene que dejar de valer.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class SessionsAndPasswordTests
    {
        private const string NewPassword = "NewPassword456*";

        private readonly RecyRouteApiFactory _api;

        public SessionsAndPasswordTests(RecyRouteApiFactory api) => _api = api;

        [Fact]
        public async Task Logout_RevokesThatToken_NotTheOthers()
        {
            var client = _api.NewClient();
            var email = Api.NewEmail();
            var first = await _api.ConfirmedCitizenAsync(client, email);
            var second = await (await client.LoginAsync(email)).ReadSessionAsync();

            var logout = await client.SendWithTokenAsync(HttpMethod.Post, "/api/auth/Logout", first.Token);

            Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.ProbeTokenAsync(first.Token)).StatusCode);

            // Se anula ese token por su jti; la sesión de otro navegador sigue abierta
            Assert.Equal(HttpStatusCode.OK, (await client.ProbeTokenAsync(second.Token)).StatusCode);
        }

        // BL-07: la contraseña nueva no sirve de nada si una sesión robada con la
        // vieja sigue funcionando
        [Fact]
        public async Task ChangePassword_ClosesAllSessions()
        {
            var client = _api.NewClient();
            var email = Api.NewEmail();
            var first = await _api.ConfirmedCitizenAsync(client, email);
            var second = await (await client.LoginAsync(email)).ReadSessionAsync();

            var change = await client.SendWithTokenAsync(HttpMethod.Put, "/api/user/UpdateUser", first.Token,
                new { currentPassword = Api.Password, newPassword = NewPassword });

            Assert.Equal(HttpStatusCode.OK, change.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.ProbeTokenAsync(first.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.ProbeTokenAsync(second.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.LoginAsync(email)).StatusCode);

            await LoginWithNewPasswordAsync(client, email);
        }

        [Fact]
        public async Task ChangePassword_WithoutTheRightCurrentPassword_KeepsTheOldOne()
        {
            var client = _api.NewClient();
            var email = Api.NewEmail();
            var session = await _api.ConfirmedCitizenAsync(client, email);

            var change = await client.SendWithTokenAsync(HttpMethod.Put, "/api/user/UpdateUser", session.Token,
                new { currentPassword = "NotTheCurrent123*", newPassword = NewPassword });

            Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.ProbeTokenAsync(session.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.LoginAsync(email)).StatusCode);
        }

        [Fact]
        public async Task ResetPassword_WithTheEmailedCode()
        {
            var client = _api.NewClient();
            var email = Api.NewEmail();
            await _api.ConfirmedCitizenAsync(client, email);
            var code = await RequestResetCodeAsync(client, email);

            var verification = await client.PostAsJsonAsync("/api/auth/verify-reset-code", new { email, code });
            var reset = await client.PostAsJsonAsync("/api/auth/reset-password",
                new { email, code, newPassword = NewPassword });

            Assert.Equal(HttpStatusCode.OK, verification.StatusCode);
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.LoginAsync(email)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.LoginAsync(email, NewPassword)).StatusCode);

            // El código se quema al usarlo: no sirve para cambiarla otra vez
            var reuse = await client.PostAsJsonAsync("/api/auth/reset-password",
                new { email, code, newPassword = "YetAnother789*" });
            Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
        }

        // Quien recupera la contraseña muchas veces lo hace porque alguien más la
        // conoce: una sesión abierta con la vieja no puede sobrevivir al cambio
        [Fact]
        public async Task ResetPassword_ClosesAllSessions()
        {
            var client = _api.NewClient();
            var email = Api.NewEmail();
            var first = await _api.ConfirmedCitizenAsync(client, email);
            var second = await (await client.LoginAsync(email)).ReadSessionAsync();
            var code = await RequestResetCodeAsync(client, email);

            var reset = await client.PostAsJsonAsync("/api/auth/reset-password",
                new { email, code, newPassword = NewPassword });

            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.ProbeTokenAsync(first.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.ProbeTokenAsync(second.Token)).StatusCode);

            await LoginWithNewPasswordAsync(client, email);
        }

        [Fact]
        public async Task ForgotPassword_RespondsTheSame_WhetherTheAccountExistsOrNot()
        {
            var client = _api.NewClient();
            var registered = Api.NewEmail();
            var unknown = Api.NewEmail();
            await _api.ConfirmedCitizenAsync(client, registered);

            var exists = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = registered });
            var doesNotExist = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = unknown });

            Assert.Equal(HttpStatusCode.OK, exists.StatusCode);
            Assert.Equal(exists.StatusCode, doesNotExist.StatusCode);
            Assert.Equal(await exists.Content.ReadAsStringAsync(), await doesNotExist.Content.ReadAsStringAsync());
            Assert.Empty(_api.Emails.SentTo(unknown));
        }

        // ── Ayudas ──────────────────────────────────────────────────────

        private async Task<string> RequestResetCodeAsync(HttpClient client, string email)
        {
            (await client.PostAsJsonAsync("/api/auth/forgot-password", new { email })).EnsureSuccessStatusCode();
            return _api.Emails.LastCode(email, EmailKind.PasswordReset);
        }

        // Tras anular las sesiones, la contraseña nueva abre una que sí vale. Se
        // espera al segundo siguiente: el token guarda su hora de emisión en
        // segundos enteros, y uno emitido en el mismo segundo del cambio nacería
        // ya anulado.
        private static async Task LoginWithNewPasswordAsync(HttpClient client, string email)
        {
            await Task.Delay(TimeSpan.FromSeconds(1.1));

            var session = await (await client.LoginAsync(email, NewPassword)).ReadSessionAsync();
            Assert.Equal(HttpStatusCode.OK, (await client.ProbeTokenAsync(session.Token)).StatusCode);
        }
    }
}
