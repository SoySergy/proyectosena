using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using proyectosena.Tests.Infrastructure;

namespace proyectosena.Tests.Authorization
{
    /// <summary>Quién puede llamar a un endpoint, según su atributo [Authorize].</summary>
    public enum Access
    {
        Anonymous,
        AnySession,
        Citizen,
        ManagerOrAdministrator,
        Administrator
    }

    /// <summary>
    /// Los 55 endpoints de la API contra su permiso esperado: sin sesión, y con la
    /// sesión de cada rol.
    /// </summary>
    /// <remarks>
    /// Aquí solo se mira la puerta —el filtro de permisos—, no lo que hace cada
    /// endpoint después. Las peticiones van sin cuerpo y con los ids vacíos: al
    /// rol que tiene permiso le responden 400, 404 o 415, y ninguna cambia datos.
    /// </remarks>
    [Collection(ApiCollection.Name)]
    public class EndpointAuthorizationTests
    {
        // El contrato de permisos, escrito a mano a propósito. Si alguien cambia un
        // [Authorize] por error, la prueba lo compara con lo que debería ser y no
        // con lo que el código dice ahora.
        private static readonly (string Verb, string Path, Access Access)[] Table =
        {
            ("POST",   "/api/Auth/Register",                              Access.Anonymous),
            ("POST",   "/api/Auth/Login",                                 Access.Anonymous),
            ("POST",   "/api/Auth/forgot-password",                       Access.Anonymous),
            ("POST",   "/api/Auth/verify-reset-code",                     Access.Anonymous),
            ("POST",   "/api/Auth/verify-email",                          Access.Anonymous),
            ("POST",   "/api/Auth/resend-verification",                   Access.Anonymous),
            ("POST",   "/api/Auth/reset-password",                        Access.Anonymous),
            ("POST",   "/api/Auth/Logout",                                Access.AnySession),

            ("GET",    "/api/CollectionRequest/GetCollectionRequests",    Access.ManagerOrAdministrator),
            ("GET",    "/api/CollectionRequest/GetCollectionRequestById", Access.AnySession),
            ("POST",   "/api/CollectionRequest/CreateCollectionRequest",  Access.Citizen),
            ("PUT",    "/api/CollectionRequest/UpdateCollectionRequest",  Access.Citizen),
            ("PATCH",  "/api/CollectionRequest/UpdateStatus",             Access.ManagerOrAdministrator),
            ("GET",    "/api/CollectionRequest/GetPendingRequests",       Access.ManagerOrAdministrator),
            ("POST",   "/api/CollectionRequest/AcceptRequest",            Access.ManagerOrAdministrator),
            ("PATCH",  "/api/CollectionRequest/CancelRequest",            Access.Citizen),
            ("GET",    "/api/CollectionRequest/GetMyAssignments",         Access.ManagerOrAdministrator),
            ("GET",    "/api/CollectionRequest/GetRequestsByUser",        Access.Citizen),

            ("GET",    "/api/User/GetUsers",                              Access.Administrator),
            ("GET",    "/api/User/GetUserById",                           Access.AnySession),
            ("GET",    "/api/User/GetUsersByRole",                        Access.Administrator),
            ("GET",    "/api/User/GetUserByEmail",                        Access.Administrator),
            ("GET",    "/api/User/GetUserByDocument",                     Access.Administrator),
            ("PUT",    "/api/User/UpdateUser",                            Access.AnySession),
            ("DELETE", "/api/User/DeleteUser",                            Access.Administrator),

            ("POST",   "/api/Admin/CreateManager",                        Access.Administrator),
            ("GET",    "/api/Admin/GetDashboardStats",                    Access.Administrator),
            ("PATCH",  "/api/Admin/ReassignRequest",                      Access.Administrator),

            ("POST",   "/api/ManagerApplication/Apply",                   Access.Citizen),
            ("GET",    "/api/ManagerApplication/GetMyApplication",        Access.Citizen),
            ("GET",    "/api/ManagerApplication/GetPending",              Access.Administrator),
            ("PATCH",  "/api/ManagerApplication/Approve",                 Access.Administrator),
            ("PATCH",  "/api/ManagerApplication/Reject",                  Access.Administrator),

            ("PATCH",  "/api/Notification/MarkAsRead",                    Access.AnySession),
            ("GET",    "/api/Notification/GetMyNotifications",            Access.AnySession),
            ("GET",    "/api/Notification/GetUnreadCount",                Access.AnySession),
            ("PATCH",  "/api/Notification/MarkAllAsRead",                 Access.AnySession),

            ("GET",    "/api/ChatHistory/GetMessagesByRequest",           Access.AnySession),
            ("POST",   "/api/ChatHistory/SendMessage",                    Access.AnySession),
            ("PUT",    "/api/ChatHistory/MarkAsRead",                     Access.AnySession),
            ("GET",    "/api/ChatHistory/GetUnreadMessages",              Access.AnySession),

            ("GET",    "/api/History/GetMyHistory",                       Access.AnySession),
            ("GET",    "/api/History/GetByRequest",                       Access.AnySession),
            ("GET",    "/api/History/GetByDateRange",                     Access.Administrator),

            // Abierto: el formulario de registro llena el desplegable antes de que exista ninguna cuenta
            ("GET",    "/api/DocumentType/GetDocumentTypes",              Access.Anonymous),
            ("GET",    "/api/DocumentType/GetDocumentTypeById",           Access.AnySession),
            ("POST",   "/api/DocumentType/CreateDocumentType",            Access.Administrator),
            ("PUT",    "/api/DocumentType/UpdateDocumentType",            Access.Administrator),
            ("DELETE", "/api/DocumentType/DeleteDocumentType",            Access.Administrator),

            ("GET",    "/api/Role/GetRoles",                              Access.Administrator),
            ("GET",    "/api/Role/GetRoleById",                           Access.AnySession),
            ("POST",   "/api/Role/CreateRole",                            Access.Administrator),
            ("PUT",    "/api/Role/UpdateRole",                            Access.Administrator),
            ("DELETE", "/api/Role/DeleteRole",                            Access.Administrator),

            ("GET",    "/api/CollectionManagement/GetByRequest",          Access.AnySession),
        };

        private enum TestRole { Citizen, Manager, Administrator }

        // Una sesión por rol para toda la tabla: abrir tres por fila serían más de
        // cien inicios de sesión con BCrypt. Estático porque xUnit crea una
        // instancia de la clase por cada fila.
        private static readonly object SharedSessionsLock = new();
        private static Task<(TestRole Role, Actor Actor)[]>? _sharedSessions;

        private readonly RecyRouteApiFactory _api;

        public EndpointAuthorizationTests(RecyRouteApiFactory api) => _api = api;

        public static TheoryData<string, string, Access> AllEndpoints => Rows(_ => true);

        public static TheoryData<string, string, Access> EndpointsRequiringSession => Rows(access => access != Access.Anonymous);

        // Un endpoint nuevo sin clasificar, o uno borrado que sigue en la tabla,
        // hace fallar esta prueba: obliga a decidir su permiso a conciencia.
        [Fact]
        public void Table_CoversExactlyTheApiEndpoints()
        {
            var actual = _api.Services.GetRequiredService<EndpointDataSource>().Endpoints
                .OfType<RouteEndpoint>()
                .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
                .SelectMany(e => e.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods
                    .Select(verb => $"{verb} /{e.RoutePattern.RawText}"))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var inTable = Table.Select(e => $"{e.Verb} {e.Path}").ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.Equal(Table.Length, inTable.Count);
            Assert.Empty(actual.Except(inTable, StringComparer.OrdinalIgnoreCase));
            Assert.Empty(inTable.Except(actual, StringComparer.OrdinalIgnoreCase));
        }

        [Theory]
        [MemberData(nameof(AllEndpoints))]
        public async Task WithoutSession(string verb, string path, Access access)
        {
            var response = await _api.NewClient().SendAsync(new HttpRequestMessage(new HttpMethod(verb), path));

            if (access == Access.Anonymous)
                Assert.False(await IsAuthorizationRejectionAsync(response),
                    $"{verb} {path} should be anonymous but returned {(int)response.StatusCode}");
            else
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(EndpointsRequiringSession))]
        public async Task WithEachRole(string verb, string path, Access access)
        {
            // Logout anula el token con el que se le llama: lleva sesiones propias
            // para no dejar sin sesión al resto de la tabla
            var sessions = path.EndsWith("/Logout", StringComparison.OrdinalIgnoreCase)
                ? await NewSessionsAsync()
                : await SharedSessionsAsync();

            foreach (var (role, actor) in sessions)
            {
                var response = await actor.SendAsync(new HttpMethod(verb), path);

                if (Allows(access, role))
                    Assert.False(await IsAuthorizationRejectionAsync(response),
                        $"{role} should get through {verb} {path} but got {(int)response.StatusCode}");
                else
                    Assert.True(await IsForbiddenByAuthorizationAsync(response),
                        $"{role} should not get through {verb} {path} but got {(int)response.StatusCode}");
            }
        }

        // ── Ayudas ──────────────────────────────────────────────────────

        private static TheoryData<string, string, Access> Rows(Func<Access, bool> include)
        {
            var rows = new TheoryData<string, string, Access>();
            foreach (var (verb, path, access) in Table.Where(e => include(e.Access)))
                rows.Add(verb, path, access);
            return rows;
        }

        private static bool Allows(Access access, TestRole role) => access switch
        {
            Access.AnySession => true,
            Access.Citizen => role == TestRole.Citizen,
            Access.ManagerOrAdministrator => role != TestRole.Citizen,
            Access.Administrator => role == TestRole.Administrator,
            _ => throw new ArgumentOutOfRangeException(nameof(access), access, null)
        };

        // Lo que responde el filtro de permisos: 401 sin sesión válida, o 403 sin
        // cuerpo. Los 403 que decide el negocio («no es tu solicitud») siempre
        // traen el motivo escrito, y ningún controlador llama a Forbid().
        private static async Task<bool> IsAuthorizationRejectionAsync(HttpResponseMessage response)
            => response.StatusCode == HttpStatusCode.Unauthorized || await IsForbiddenByAuthorizationAsync(response);

        private static async Task<bool> IsForbiddenByAuthorizationAsync(HttpResponseMessage response)
            => response.StatusCode == HttpStatusCode.Forbidden
               && (await response.Content.ReadAsStringAsync()).Length == 0;

        private Task<(TestRole Role, Actor Actor)[]> SharedSessionsAsync()
        {
            lock (SharedSessionsLock)
                return _sharedSessions ??= NewSessionsAsync();
        }

        private async Task<(TestRole Role, Actor Actor)[]> NewSessionsAsync() => new[]
        {
            (TestRole.Citizen, await _api.CitizenAsync()),
            (TestRole.Manager, await _api.ManagerAsync()),
            (TestRole.Administrator, await _api.AdministratorAsync())
        };
    }
}
