# oauth-cli

Command line interface for retreiving OAuth tokens.

The following flows or grant types are supported
- Authorization Code Flow, with and without PKCE (grant_type=authorization_code)
- Client Credential (grant_type=client_credentials)
- Resource Owner Password Flow (grant_type=password)


When using Azure Active Directory, also a client certificate can be used instead of a client secret. The client certificate should be stored in the local user store.


## Configuration

In order to get the access token for a client, the client first needs to be configured in the clients section of the appsettings.json file. The following is an example for a client using password credential flow and a client using authorization code flow.

``` json
"clients": {
    "opendict-cc": {
      "wellknownEndpoint": "https://localhost:44375/.well-known/openid-configuration",
      "clientId": "postman",
      "grantType": "Password",
      "scopes": [
        "api"
      ]
    },
    "opendict-auth-code": {
      "wellknownEndpoint": "https://localhost:44375/.well-known/openid-configuration",
      "clientId": "postman",
      "grantType": "AuthorizationCode",
      "usePKCE": true,
      "redirectUri": "http://localhost/myapp/",
      "scopes": [
        "offline_access"
      ]
    }
}
```

The configuration should match the configuration on the token provider.

## Usage

To get the acces token for a a client simply use the name of the client as specified in the configuration as the first argument. For example, to get the access token for the client named `opendict-auth-code` use:

``` shell
auth opendict-auth-code
```

When the access token is succesfully retrieved it is writen to the console output.


## References

https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-net-client-assertions

https://learn.microsoft.com/en-us/azure/active-directory/develop/howto-create-self-signed-certificate

https://learn.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow