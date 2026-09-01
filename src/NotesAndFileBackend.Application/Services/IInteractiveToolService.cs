using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NotesAndFileBackend.Application.Models;

namespace NotesAndFileBackend.Application.Services;

public interface IInteractiveToolService
{
    Task<IEnumerable<InteractiveToolListDto>> ListToolsAsync(Guid noteId, Guid userId);
    Task<InteractiveToolDetailsDto?> GetToolAsync(Guid noteId, Guid toolId, Guid userId);
    Task<InteractiveToolDetailsDto> CreateToolAsync(Guid noteId, CreateInteractiveToolRequest request, Guid userId);
    Task<InteractiveToolDetailsDto> UpdateToolAsync(Guid noteId, Guid toolId, UpdateInteractiveToolRequest request, Guid userId);
    Task<bool> DeleteToolAsync(Guid noteId, Guid toolId, Guid userId);
}
