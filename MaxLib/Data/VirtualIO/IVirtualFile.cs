using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Data.VirtualIO
{
    public interface IVirtualFile : IVirtualEntry
    {
        Stream GetStream(bool readOnly);

        Task<Stream> GetStreamAsync(bool readOnly);

        Task<Stream> GetStreamAsync(bool readOnly, CancellationToken cancellationToken);
    }
}
