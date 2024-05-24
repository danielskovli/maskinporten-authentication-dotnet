namespace SampleWebApp.HttpClients;

public interface IGetApiDataHandler<T>
{
    public Task<T> GetApiData();
}
