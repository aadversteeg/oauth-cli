# oauth-cli

[![validate](https://github.com/aadversteeg/oauth-cli/actions/workflows/validate.yml/badge.svg)](https://github.com/aadversteeg/oauth-cli/actions/workflows/validate.yml)

Command line interface for retreiving OAuth tokens.

The following flows or grant types are supported
- Authorization Code Flow, with and without PKCE (grant_type=authorization_code)
- Client Credential (grant_type=client_credentials)
- Resource Owner Password Flow (grant_type=password)


When using Azure Active Directory, also a client certificate can be used instead of a client secret. The client certificate should be stored in the local user store.


## Configuration

### Clients
In order to get the access token for a client, the client first needs to be configured in the clients section of the appsettings.json file. The following is an example for a client using password credential flow and a client using authorization code flow.

``` json
{
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
}
```


The configuration should match the configuration on the token provider.

### Certificate Stores

The tool can use certificates for client assertions. Currently two certificate stores/locations are supported:

- Windows Certificate Store: type = `windows`
- Local File System: type = `localfile`

In order to use these, the certificate stores need to be defined in the appsettings:

``` json
{
  "certificateStores": {
    "windows": {
      "type": "windows",
      "location":  "CurrentUser"
    },
    "certificates-on-d": {
      "type": "localfile",
      "folder": "D:\\Certificates"
    }
  }
}

```

The certificate store can be used in a client configuration when the grant type is ClientCredentials:

``` json
{
  "clients": {
      "some-api-client": {
        "wellknownEndpoint": "https://login.microsoftonline.com/2fb8c929-8306-4a3b-8c12-a201b46ce22a/v2.0/.well-known/openid-configuration",
        "clientId": "8583c1bb-e870-4a3d-98c6-bab6fc10cbf0",
        "clientCertificateName": "private-certificate.pfx",
        "clientCertificateStore": "certificates-on-d",
        "grantType": "ClientCredentials",
        "scopes": [
          "api://mytenant.onmicrosoft.com/some-api/.default"
        ]
      }
  }
}
```

For the local file certificate store, the certificate name is the file name. For the Windows certificate store, the certificate name is the subject name of the certificate.


## Usage

To get a list of all configured clients use:
``` shell
auth client list
```

To get the acces token for a a client simply use the name of the client as specified in the configuration as the first argument. For example, to get the access token for the client named `opendict-auth-code` use:

``` shell
auth client get-access-token opendict-auth-code
```

Some commands also have a shorter alias.  For getting the access token you can also use:

``` shell
auth client gat opendict-auth-code
```

When the access token is succesfully retrieved, it is writen to the console output.

To see more commands, arguments and options, just use:
``` shell
auth --help
```






## References

https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-net-client-assertions

https://learn.microsoft.com/en-us/azure/active-directory/develop/howto-create-self-signed-certificate

https://learn.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow