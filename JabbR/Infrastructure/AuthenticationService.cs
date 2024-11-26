using System;
using System.Collections.Generic;
using System.Linq;
using JabbR.Services;
using SimpleAuthentication;
using SimpleAuthentication.Core;

namespace JabbR.Infrastructure
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly AuthenticationProviderFactory _factory;

        public AuthenticationService(AuthenticationProviderFactory factory, ApplicationSettings appSettings)
        {
            _factory = factory;

            ConfigureProvider<FacebookProvider>("Facebook", appSettings.FacebookAppId, appSettings.FacebookAppSecret);
            ConfigureProvider<TwitterProvider>("Twitter", appSettings.TwitterConsumerKey, appSettings.TwitterConsumerSecret);
            ConfigureProvider<GoogleProvider>("Google", appSettings.GoogleClientID, appSettings.GoogleClientSecret);
        }

        private void ConfigureProvider<T>(string providerName, string publicKey, string secretKey) where T : IAuthenticationProvider, new()
        {
            if (!String.IsNullOrWhiteSpace(publicKey) && !String.IsNullOrWhiteSpace(secretKey))
            {
                var provider = new T();
                provider.AuthenticateRedirectionUrl = new Uri($"https://yourapp.com/auth/{providerName.ToLower()}"); // Replace with your actual URL
                _factory.AddProvider(provider);

                // Set the provider parameters
                if (_factory.AuthenticationProviders.TryGetValue(providerName, out var configuredProvider))
                {
                    configuredProvider.ProviderParams = new ProviderParams
                    {
                        PublicApiKey = publicKey,
                        SecretApiKey = secretKey
                    };
                }
            }
            else
            {
                _factory.RemoveProvider<T>();
            }
        }

        public IEnumerable<IAuthenticationProvider> GetProviders()
        {
            return _factory.AuthenticationProviders?.Values ?? Enumerable.Empty<IAuthenticationProvider>();
        }
    }
}