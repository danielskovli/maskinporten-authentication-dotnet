using Microsoft.Extensions.Logging;

namespace SampleCmdApp;

public class ExampleRunner(ExampleHttpClient client, ILogger<ExampleRunner> logger)
{
    public async Task Run()
    {
        var apiData = await client.GetApiData();
        logger.LogDebug("Fetched API data was: {ApiData}", apiData);
    }
}
