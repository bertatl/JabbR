using System;
using System.Collections.Generic;
using System.Linq;
using JabbR.Services;
using SimpleAuthentication;
using SimpleAuthentication.Core;
using System.Security.Claims;

namespace JabbR.Infrastructure
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly AuthenticationProviderFactory _factory;

        public AuthenticationService(AuthenticationProviderFactory factory, ApplicationSettings appSettings)
        {
            _factory = factory;

            AddProviderIfConfigured("facebook", appSettings.FacebookAppId, appSettings.FacebookAppSecret);
            AddProviderIfConfigured("twitter", appSettings.TwitterConsumerKey, appSettings.TwitterConsumerSecret);
            AddProviderIfConfigured("google", appSettings.GoogleClientID, appSettings.GoogleClientSecret);
        }

        private void AddProviderIfConfigured(string providerName, string publicKey, string secretKey)
        {
            if (!string.IsNullOrWhiteSpace(publicKey) && !string.IsNullOrWhiteSpace(secretKey))
            {
                _factory.AddProvider(() => new GenericProvider(providerName, new ProviderParams
                {
                    PublicApiKey = publicKey,
                    SecretApiKey = secretKey
                }));
            }
            else
            {
                _factory.RemoveProvider(providerName);
            }
        }

        public IEnumerable<IAuthenticationProvider> GetProviders()
        {
            if (_factory.AuthenticationProviders == null)
            {
                return Enumerable.Empty<IAuthenticationProvider>();
            }

            return _factory.AuthenticationProviders.Values;
        }
    }

    // Generic provider class to replace specific providers
    public class GenericProvider : IAuthenticationProvider
    {
        private readonly string _name;
        private readonly ProviderParams _providerParams;

        public GenericProvider(string name, ProviderParams providerParams)
        {
            _name = name;
            _providerParams = providerParams;
        }

        public string Name => _name;

        public ProviderParams ProviderParams => _providerParams;

        public AuthenticatedClient AuthenticateClient(ProviderParams providerParams, System.Collections.Specialized.NameValueCollection parameters)
        {
            throw new NotImplementedException("This method needs to be implemented based on the specific provider requirements.");
        }

        public System.Threading.Tasks.Task<AuthenticatedClient> AuthenticateClientAsync(ProviderParams providerParams, System.Collections.Specialized.NameValueCollection parameters)
        {
            throw new NotImplementedException("This method needs to be implemented based on the specific provider requirements.");
        }

        public ClaimsIdentity AuthenticateUser(AuthenticatedClient authenticatedClient)
        {
            throw new NotImplementedException("This method needs to be implemented based on the specific provider requirements.");
        }

        public System.Threading.Tasks.Task<ClaimsIdentity> AuthenticateUserAsync(AuthenticatedClient authenticatedClient)
        {
            throw new NotImplementedException("This method needs to be implemented based on the specific provider requirements.");
        }

        public System.Uri GetRedirectUri(System.Uri currentUri, System.Uri callbackUri)
        {
            throw new NotImplementedException("This method needs to be implemented based on the specific provider requirements.");
        }
    }
}