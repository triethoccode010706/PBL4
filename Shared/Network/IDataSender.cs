namespace Shared.Network
{
    public interface IDataSender
    {
        Task SendAsync(byte[] data, CancellationToken cancellationToken = default);
    }
}
