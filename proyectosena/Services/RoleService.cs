using proyectosena.DTOs.User;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<List<RoleDto>> GetAll()
        {
            var roles = await _roleRepository.GetRoles();
            return roles.Select(MapToDto).ToList();
        }

        public async Task<RoleDto?> GetById(Guid idRole)
        {
            var role = await _roleRepository.GetRole(idRole);
            return role == null ? null : MapToDto(role);
        }

        public async Task<RoleDto> Create(RoleDto dto)
        {
            var created = await _roleRepository.CreateRole(MapToEntity(dto));
            return MapToDto(created);
        }

        public async Task<RoleDto> Update(RoleDto dto)
        {
            var updated = await _roleRepository.UpdateRole(MapToEntity(dto));
            return MapToDto(updated);
        }

        public Task<bool> Delete(Guid idRole)
            => _roleRepository.DeleteRole(idRole);

        // ── Mapeo ───────────────────────────────────────────────────────
        // Deja fuera la colección de usuarios: no sale de esta capa

        private static RoleDto MapToDto(Role r) => new()
        {
            IdRole = r.IdRole,
            RoleName = r.RoleName,
            RoleDescription = r.RoleDescription
        };

        private static Role MapToEntity(RoleDto d) => new()
        {
            IdRole = d.IdRole,
            RoleName = d.RoleName,
            RoleDescription = d.RoleDescription
        };
    }
}
