using proyectosena.DTOs.Common;
using proyectosena.DTOs.Requests;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Services
{
    public class HistoryService : IHistoryService
    {
        private readonly IHistoryRepository _historyRepository;

        public HistoryService(IHistoryRepository historyRepository)
        {
            _historyRepository = historyRepository;
        }

        public async Task<PagedResult<HistoryResponseDto>> GetMyHistory(Guid idUser, int page, int pageSize)
        {
            (page, pageSize) = PagedResult<HistoryResponseDto>.Normalize(page, pageSize);

            var (items, total) = await _historyRepository.GetByRequestOwner(idUser, page, pageSize);

            return PagedResult<HistoryResponseDto>.Create(
                items.Select(MapToDto).ToList(), page, pageSize, total);
        }

        public async Task<List<HistoryResponseDto>> GetByRequest(Guid idRequest)
        {
            var histories = await _historyRepository.GetByRequest(idRequest);
            return histories.Select(MapToDto).ToList();
        }

        public async Task<PagedResult<HistoryResponseDto>> GetByDateRange(
            DateTime startDate, DateTime endDate, int page, int pageSize)
        {
            (page, pageSize) = PagedResult<HistoryResponseDto>.Normalize(page, pageSize);

            var (items, total) = await _historyRepository.GetByDateRange(startDate, endDate, page, pageSize);

            return PagedResult<HistoryResponseDto>.Create(
                items.Select(MapToDto).ToList(), page, pageSize, total);
        }

        // ── Mapeo ───────────────────────────────────────────────────────
        // UserName sale del usuario que el repositorio trae con Include. Si no
        // viniera cargado queda vacío en vez de reventar: el historial se sigue
        // viendo aunque falte el nombre.
        private static HistoryResponseDto MapToDto(History h) => new()
        {
            IdHistory = h.IdHistory,
            IdRequest = h.IdRequest,
            IdUser = h.IdUser,
            UserName = h.User != null ? $"{h.User.Name} {h.User.LastName}" : string.Empty,
            PreviousStatus = h.PreviousStatus,
            NewStatus = h.NewStatus,
            ChangeDate = h.ChangeDate,
            Comment = h.Comment
        };
    }
}
