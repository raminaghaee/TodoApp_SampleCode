namespace TodoApp.Business.Subtasks;

public interface ISubtaskService
{
	/// <summary>
	/// مرتب بر اساس position.
	/// </summary>
	Task<IReadOnlyList<SubtaskDto>> GetByTaskAsync(int userId, int taskId, CancellationToken ct = default);
	Task<SubtaskDto> CreateAsync(int userId, int taskId, CreateSubtaskRequest request, CancellationToken ct = default);
	Task<SubtaskDto> UpdateAsync(int userId, int subtaskId, UpdateSubtaskRequest request, CancellationToken ct = default);
	Task DeleteAsync(int userId, int subtaskId, CancellationToken ct = default);
}
