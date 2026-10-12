namespace Shared.Network
{
    public interface IDataReceiver
    {
        Stream Stream { get; }

        Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken = default);
    }
}
