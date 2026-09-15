using Microsoft.Extensions.DependencyInjection;
using proyectosena.Interfaces.Services;
using proyectosena.Models;
using proyectosena.Tests.Infrastructure;

namespace proyectosena.Tests.Auth
{
    /// <summary>
    /// VerificationCodeService contra la base real: los códigos de 6 cifras de
    /// confirmar el correo y de recuperar la contraseña.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class VerificationCodeTests
    {
        private readonly RecyRouteApiFactory _api;

        public VerificationCodeTests(RecyRouteApiFactory api) => _api = api;

        [Fact]
        public async Task CorrectCode_IsValid()
        {
            var email = Api.NewEmail();

            var code = await IssueAsync(email);

            Assert.Matches("^[0-9]{6}$", code);
            Assert.True(await ValidateAsync(email, code));
        }

        [Fact]
        public async Task ExpiredCode_IsRejected()
        {
            var email = Api.NewEmail();

            var code = await IssueAsync(email, minutes: -1);

            Assert.False(await ValidateAsync(email, code));
        }

        [Fact]
        public async Task FourFailures_CodeStillWorks()
        {
            var email = Api.NewEmail();
            var code = await IssueAsync(email);

            for (var i = 0; i < 4; i++)
                Assert.False(await ValidateAsync(email, WrongCode(code)));

            Assert.True(await ValidateAsync(email, code));
        }

        [Fact]
        public async Task FifthFailure_KillsTheCode()
        {
            var email = Api.NewEmail();
            var code = await IssueAsync(email);

            for (var i = 0; i < 5; i++)
                Assert.False(await ValidateAsync(email, WrongCode(code)));

            Assert.False(await ValidateAsync(email, code));
        }

        // Cuatro fallos a la vez tienen que contar cuatro, de modo que un quinto
        // mate el código. Si cada petición leyera el contador, le sumara uno y lo
        // guardara, las cuatro leerían el mismo valor y se pisarían: el contador
        // quedaría corto y el código sobreviviría al quinto. La suma la hace la
        // base (B-6 · WA-12).
        [Fact]
        public async Task SimultaneousFailures_AllCount()
        {
            var email = Api.NewEmail();
            var code = await IssueAsync(email);

            var results = await _api.RunSimultaneouslyAsync<IVerificationCodeService, bool>(
                4, (service, _) => service.ValidateCode(email, WrongCode(code), CodePurpose.AccountAccess));

            Assert.DoesNotContain(true, results);
            Assert.False(await ValidateAsync(email, WrongCode(code)));
            Assert.False(await ValidateAsync(email, code));
        }

        [Fact]
        public async Task CodeForAnotherPurpose_IsRejected()
        {
            var email = Api.NewEmail();
            var code = await IssueAsync(email, CodePurpose.EmailConfirmation);

            Assert.False(await ValidateAsync(email, code, CodePurpose.AccountAccess));
            Assert.True(await ValidateAsync(email, code, CodePurpose.EmailConfirmation));
        }

        [Fact]
        public async Task RequestingANewCode_VoidsThePreviousOne()
        {
            var email = Api.NewEmail();
            var first = await IssueAsync(email);

            // Pueden salir iguales una vez entre un millón; entonces no probaría nada
            string second;
            do { second = await IssueAsync(email); } while (second == first);

            Assert.False(await ValidateAsync(email, first));
            Assert.True(await ValidateAsync(email, second));
        }

        [Fact]
        public async Task Email_IsCaseInsensitive()
        {
            var email = Api.NewEmail();

            var code = await IssueAsync(email.ToUpperInvariant());

            Assert.True(await ValidateAsync(email, code));
        }

        [Fact]
        public async Task InvalidatedCode_NoLongerWorks()
        {
            var email = Api.NewEmail();
            var code = await IssueAsync(email);

            await UseAsync(service => service.InvalidateCode(email, CodePurpose.AccountAccess));

            Assert.False(await ValidateAsync(email, code));
        }

        // ── Ayudas ──────────────────────────────────────────────────────

        // Un scope por llamada, como una petición HTTP: el servicio y su DbContext
        // son Scoped, y el DbContext no admite dos consultas a la vez.
        private async Task<T> UseAsync<T>(Func<IVerificationCodeService, Task<T>> use)
        {
            using var scope = _api.Services.CreateScope();
            return await use(scope.ServiceProvider.GetRequiredService<IVerificationCodeService>());
        }

        private async Task UseAsync(Func<IVerificationCodeService, Task> use)
        {
            using var scope = _api.Services.CreateScope();
            await use(scope.ServiceProvider.GetRequiredService<IVerificationCodeService>());
        }

        private Task<string> IssueAsync(string email, CodePurpose purpose = CodePurpose.AccountAccess, int minutes = 15)
            => UseAsync(service => service.GenerateAndStoreCode(email, purpose, minutes));

        private Task<bool> ValidateAsync(string email, string code, CodePurpose purpose = CodePurpose.AccountAccess)
            => UseAsync(service => service.ValidateCode(email, code, purpose));

        private static string WrongCode(string code) => code == "000000" ? "111111" : "000000";
    }
}
