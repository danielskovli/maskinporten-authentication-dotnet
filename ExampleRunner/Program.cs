using ExampleRunner;
using MaskinportenAuthentication;

var creds = await CredentialsLoader.Load(
    Path.Combine(Environment.CurrentDirectory, "Secrets", "credentials.json")
);

var auth = await Maskinporten.Authenticate(
    authority: "https://maskinporten.dev/",
    appId: creds.AppId,
    jwk: creds.Keys.First(),
    scopes: "idporten:operationalstatus.admin"
);

Console.WriteLine($"Maskinporten responded: {auth}");
