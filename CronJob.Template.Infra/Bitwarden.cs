#nullable enable

using System;
using System.Collections.Generic;
using Bitwarden.Sdk;

public class Bw
{
    private const string ACCESS_TOKEN_ENV_KEY = "BWS_ACCESS_TOKEN";
    private const string DEFAULT_ORG = "f329a0e7-4bfd-4a0b-8e6c-b2400076524b";

    private readonly string orgId;
    private readonly string accessToken;

    private Dictionary<string, string>? secrets;

    public Bw()
    {
        this.orgId = DEFAULT_ORG;
        this.accessToken = Environment.GetEnvironmentVariable(ACCESS_TOKEN_ENV_KEY) 
            ?? throw new ArgumentNullException(ACCESS_TOKEN_ENV_KEY, "the bitwarden access token environment variable was not provided");
    }

    public Bw(string orgId, string accessToken)
    {
        this.orgId = orgId;
        this.accessToken = accessToken;
    }

    public string GetSecret(string key)
    {
        LoadSecrets();

        if(secrets is null) 
            throw new InvalidOperationException("Internal error: secrets are not loaded properly");

        if(!secrets.ContainsKey(key))
            throw new InvalidOperationException($"No secret with key {key} was found in bitwarden");

        return secrets[key];
    }

    private void LoadSecrets()
    {
        if (secrets is not null)
            return;

        secrets = new Dictionary<string, string>();

        using BitwardenClient client = new();
        client.Auth.LoginAccessToken(accessToken);

        SecretIdentifiersResponse secretIds = client.Secrets.List(Guid.Parse(orgId));

        foreach(SecretIdentifierResponse? secret in secretIds.Data) 
        {
            if(secret is null) continue;

            secrets[secret.Key] = client.Secrets.Get(secret.Id).Value;
        }
    }
}
