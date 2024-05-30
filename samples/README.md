# Implementation examples

Please refer to the `Program.cs` file in each sample app [[1](SampleCmdApp/Program.cs), [2](SampleWebApp/Program.cs)] for a demonstration of a variety of implementation options.

## Requirements
1. A client registered with Maskinporten. See the docs [here](https://samarbeid.digdir.no/maskinporten/maskinporten/25), [here](https://docs.digdir.no/docs/Maskinporten/maskinporten_overordnet) and [here](https://docs.digdir.no/docs/Maskinporten/maskinporten_guide_apikonsument) for more information on getting started
2. The scopes `idporten:dcr.read` and/or `skatteetaten:testnorge/testdata.read`, depending on which sample app you wish to run
3. A JSON file containing the required authentication details for communication with Maskinporten. Please refer to the [maskinporten-settings-sample.json](secrets/maskinporten-settings.sample.json) for a demonstration of what this file should contain
   * If you place your customized settings file in the same folder and name it `maskinporten-settings.json`, both sample apps will build and run without the need for any additional configuration

## Building and running
### Example 1: Commandline application
```shell
cd samples/SampleCmdApp
dotnet build
dotnet run
```
The app will perform its single task, then terminate.

### Example 2: Web application
```shell
cd SampleWebApp
dotnet build
dotnet run --launch-profile "https"
```
Your web app is now reachable at https://localhost:7258 and will redirect to the Swagger documentation.
