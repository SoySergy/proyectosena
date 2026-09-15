using System.Net;
using System.Net.Http.Json;
using proyectosena.Models;
using proyectosena.Tests.Infrastructure;

namespace proyectosena.Tests.Auth
{
    /// <summary>
    /// Registrarse, confirmar el correo y entrar, por HTTP como el navegador.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class RegistrationAndLoginTests
    {
        private readonly RecyRouteApiFactory _api;

        public RegistrationAndLoginTests(RecyRouteApiFactory api) => _api = api;

        [Fact]
        public async Task Register_GivesNoSession_UntilEmailIsConfirmed()
        {
            var client = _api.NewClient();
            var email = Api.NewEmail();

            var registration = await client.RegisterAsync(email);

            Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
            Assert.DoesNotContain("token", await registration.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

            // Contraseña correcta pero correo sin confirmar: 403, no 401, para que
            // la persona sepa qué le falta
            Assert.Equal(HttpStatusCode.Forbidden, (await client.LoginAsync(email)).StatusCode);

            var code = _api.Emails.LastCode(email, EmailKind.EmailConfirmation);
            var session = await (await client.PostAsJsonAsync("/api/auth/verify-email", new { email, code }))
                .ReadSessionAsync();

            Assert.Equal(RoleNames.Citizen, session.User.RoleName);
            Assert.Equal(HttpStatusCode.OK, (await client.ProbeTokenAsync(session.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.LoginAsync(email)).StatusCode);
        }

        [Fact]
        public async Task WrongConfirmationCode_Returns400()
        {
            var client = _api.NewClient();
            var email = Api.NewEmail();
            (await client.RegisterAsync(email)).EnsureSuccessStatusCode();

            var code = _api.Emails.LastCode(email, EmailKind.EmailConfirmation);
            var wrongCode = code == "000000" ? "111111" : "000000";

            var response = await client.PostAsJsonAsync("/api/auth/verify-email", new { email, code = wrongCode });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // WA-03: registrarse con un correo ajeno no puede servir para saber si
        // esa persona tiene cuenta
        [Fact]
        public async Task Register_WithTakenEmail_RespondsLikeANewOne()
        {
            var client = _api.NewClient();
            var email = Api.NewEmail();

            var fresh = await client.RegisterAsync(email);
            var repeated = await client.RegisterAsync(email);

            Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
            Assert.Equal(fresh.StatusCode, repeated.StatusCode);
            Assert.Equal(await fresh.Content.ReadAsStringAsync(), await repeated.Content.ReadAsStringAsync());

            // Por dentro sí cambia: a la dueña se le avisa y no se emite otro código
            var received = _api.Emails.SentTo(email);
            Assert.Single(received, sent => sent.Kind == EmailKind.EmailConfirmation);
            Assert.Single(received, sent => sent.Kind == EmailKind.AlreadyRegistered);
        }

        [Fact]
        public async Task Register_WithTakenDocument_NotifiesItsOwner()
        {
            var client = _api.NewClient();
            var documentNumber = Api.NewDocumentNumber();
            var owner = Api.NewEmail();
            var other = Api.NewEmail();
            (await client.RegisterAsync(owner, documentNumber)).EnsureSuccessStatusCode();

            var repeated = await client.RegisterAsync(other, documentNumber);

            Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);

            // El aviso va al correo real de quien tiene el documento, no al que
            // escribió quien llama, que puede ser el de cualquiera
            Assert.Contains(_api.Emails.SentTo(owner), sent => sent.Kind == EmailKind.AlreadyRegistered);
            Assert.Empty(_api.Emails.SentTo(other));
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.LoginAsync(other)).StatusCode);
        }

        // Distinguirlos le confirmaría a un atacante qué correos están registrados
        [Fact]
        public async Task Login_UnknownEmailAndWrongPassword_RespondTheSame()
        {
            var client = _api.NewClient();
            var email = Api.NewEmail();
            await _api.ConfirmedCitizenAsync(client, email);

            var unknownEmail = await client.LoginAsync(Api.NewEmail());
            var wrongPassword = await client.LoginAsync(email, "AnotherPassword123*");

            Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
            Assert.Equal(unknownEmail.StatusCode, wrongPassword.StatusCode);
            Assert.Equal(await unknownEmail.Content.ReadAsStringAsync(), await wrongPassword.Content.ReadAsStringAsync());
        }

        // Política Auth de Program.cs: diez intentos por minuto por IP
        [Fact]
        public async Task Login_EleventhAttemptFromSameIp_Returns429()
        {
            var client = _api.NewClient();

            // Un correo que no existe: la respuesta no pasa por BCrypt y la prueba va rápida
            var email = Api.NewEmail();

            for (var i = 0; i < 10; i++)
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.LoginAsync(email)).StatusCode);

            Assert.Equal(HttpStatusCode.TooManyRequests, (await client.LoginAsync(email)).StatusCode);

            // El cupo agotado es el de esa IP, no el de todo el mundo
            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.NewClient().LoginAsync(email)).StatusCode);
        }
    }
}
