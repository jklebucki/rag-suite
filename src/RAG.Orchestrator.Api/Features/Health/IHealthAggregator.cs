using System.Threading;
using System.Threading.Tasks;

namespace RAG.Orchestrator.Api.Features.Health;

public interface IHealthAggregator
{
    Task<SystemHealthResponse> GetSystemHealthAsync(CancellationToken cancellationToken = default);
}