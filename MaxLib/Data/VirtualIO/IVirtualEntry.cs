using System;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Data.VirtualIO
{
    public interface IVirtualEntry
    {
        string Name { get; }

        long? Length { get; }

        VirtualPath Path { get; }

        Task<long?> GetLengthAsync();

        Task<long?> GetLengthAsync(CancellationToken cancellationToken);

        DateTime? Created { get; }

        Task<DateTime?> GetCreatedAsync();

        Task<DateTime?> GetCreatedAsync(CancellationToken cancellationToken);

        DateTime? Modified { get; }

        Task<DateTime?> GetModifiedAsync();

        Task<DateTime?> GetModifiedAsync(CancellationToken cancellationToken);

        IIOHost Host { get; }
    }
}
