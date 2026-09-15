using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using proyectosena.Models;
using proyectosena.Tests.Infrastructure;

namespace proyectosena.Tests.Authorization
{
    /// <summary>
    /// Tokens que no emitió el servidor, todos con rol de administrador: lo primero
    /// que prueba un atacante contra una API con JWT.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class ForgedTokenTests
    {
        private const string AdministratorPath = "/api/admin/GetDashboardStats";

        private readonly RecyRouteApiFactory _api;

        public ForgedTokenTests(RecyRouteApiFactory api) => _api = api;

        // Control: fabricado igual pero con la clave buena y vigente, sí entra. Sin
        // esto, un 401 en las demás podría deberse a un token mal armado y no al
        // defecto que cada una prueba.
        [Fact]
        public async Task ProperlySignedUnexpiredToken_GetsIn()
        {
            var token = Forge(RecyRouteApiFactory.JwtKey, expires: DateTime.UtcNow.AddMinutes(30));

            Assert.Equal(HttpStatusCode.OK, await RequestWithAsync(token));
        }

        [Fact]
        public async Task SignedWithAnotherKey_Returns401()
        {
            var token = Forge("another-key-the-server-does-not-know-0123456789-abcdefghij", expires: DateTime.UtcNow.AddMinutes(30));

            Assert.Equal(HttpStatusCode.Unauthorized, await RequestWithAsync(token));
        }

        // Program.cs valida la caducidad sin margen (ClockSkew = 0)
        [Fact]
        public async Task Expired_Returns401()
        {
            var token = Forge(RecyRouteApiFactory.JwtKey, expires: DateTime.UtcNow.AddMinutes(-1));

            Assert.Equal(HttpStatusCode.Unauthorized, await RequestWithAsync(token));
        }

        // "alg": "none": un token sin firma que cualquiera puede escribir a mano
        [Fact]
        public async Task Unsigned_Returns401()
        {
            var token = Forge(key: null, expires: DateTime.UtcNow.AddMinutes(30));

            Assert.Equal(HttpStatusCode.Unauthorized, await RequestWithAsync(token));
        }

        private async Task<HttpStatusCode> RequestWithAsync(string token)
            => (await _api.NewClient().SendWithTokenAsync(HttpMethod.Get, AdministratorPath, token)).StatusCode;

        // Los mismos claims que emite AuthService, con la clave y la fecha que se pidan
        private static string Forge(string? key, DateTime expires)
        {
            var issuedAt = expires.AddHours(-1);

            var signing = key is null
                ? null
                : new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: RecyRouteApiFactory.JwtIssuer,
                audience: RecyRouteApiFactory.JwtAudience,
                claims: new[]
                {
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(issuedAt).ToString(), ClaimValueTypes.Integer64),
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, RoleNames.Administrator)
                },
                notBefore: issuedAt,
                expires: expires,
                signingCredentials: signing);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
