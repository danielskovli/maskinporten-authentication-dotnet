# Maskinporten Authentication Library
A C# library that simplifies authentication and authorization with Maskinporten via pre-registered keys.

More information about Maskinporten can be found here: https://samarbeid.digdir.no/maskinporten/maskinporten/25.

## Requirements
* [.NET version 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or newer
* An app consumer that is set up for [dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
* A Maskinporten client registered in your target environment
* The `client-id` and `jwk` private key corresponding with your client registration

The Maskinporten client must be entitled to grant type `urn:ietf:params:oauth:grant-type:jwt-bearer`, with integration type `maskinporten` and authorization method `private_key_jwt`.

Further reading: [creating clients](https://docs.digdir.no/docs/Maskinporten/maskinporten_sjolvbetjening_web#opprette-klient-for-%C3%A5-konsumere-api), [registering keys](https://docs.digdir.no/docs/Maskinporten/maskinporten_sjolvbetjening_web#registrere-n%C3%B8kkel-p%C3%A5-klient).

## Usage
Please refer to [samples/README.md](samples/README.md) for a small selection of examples.

In the most general terms possible, do the following:
1. Clone this repo
2. Set up a reference from your project to the `MaskinportenAuthentication` project
3. Save a [maskinporten-settings.json](samples/secrets/maskinporten-settings.sample.json) file somewhere accessible to your running container
4. Tell your application where to find the settings file
5. Add the [MaskinportenClient](src/MaskinportenAuthentication/MaskinportenClient.cs) service and invoke it where required

### AspNetCore pseudo code
```csharp
using MaskinportenAuthentication.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddMaskinportenClient();
builder.Services.AddHttpClient<IFancyClient, FancyClient>().UseMaskinportenAuthorization(["the:scope"]);

var app = builder.Build();

app.MapGet(
   "/your-endpoint",
   async (IFancyClient client) =>
   {
       var apiData = await client.GetApiData();
       // TODO: Do something with `apiData`
   }
);

app.Run();
```
