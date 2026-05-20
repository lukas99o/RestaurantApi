namespace ResturangDB_API.Services.IServices
{
    public interface IMenuImageBootstrapper
    {
        Task EnsureSeedImagesDownloadedAsync(CancellationToken cancellationToken = default);
    }
}
