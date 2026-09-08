using proyectosena.Models;

namespace proyectosena.Interfaces.Repositories
{
    public interface IManagerApplicationRepository
    {
        // La solicitud más reciente de un ciudadano, con su usuario y su revisor
        // cargados. Null si nunca ha solicitado.
        Task<ManagerApplication?> GetLatestByUser(Guid idUser);

        // Una solicitud concreta, con el solicitante cargado. Null si no existe.
        Task<ManagerApplication?> GetById(Guid idApplication);

        // Página de solicitudes esperando revisión, la más antigua primero:
        // quien lleva más tiempo esperando se atiende antes.
        Task<(List<ManagerApplication> Items, int Total)> GetPending(int page, int pageSize);

        // True si ese ciudadano ya tiene una esperando revisión
        Task<bool> HasPending(Guid idUser);

        Task<ManagerApplication> Create(ManagerApplication application);

        // Guarda la decisión sobre una solicitud. Un rechazo solo toca esta tabla.
        Task<ManagerApplication> SaveDecision(ManagerApplication application);

        /// <summary>
        /// Guarda la aprobación y le cambia el rol al solicitante, todo o nada.
        /// </summary>
        /// <remarks>
        /// Van juntas a propósito: si se guardara la aprobación y fallara el
        /// cambio de rol, la solicitud diría «aprobada» y la persona seguiría
        /// siendo ciudadana. Los dos cambios viajan en un solo guardado, y EF los
        /// mete en una sola transacción.
        ///
        /// <para>
        /// Qué rol conceder lo decide el servicio y llega como parámetro: el
        /// repositorio solo persiste.
        /// </para>
        /// </remarks>
        Task<ManagerApplication> ApproveAndGrantRole(ManagerApplication application, Guid idRoleToGrant);
    }
}
