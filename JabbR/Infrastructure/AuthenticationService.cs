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

            ConfigureProvider("facebook", appSettings.FacebookAppId, appSettings.FacebookAppSecret);
            ConfigureProvider("twitter", appSettings.TwitterConsumerKey, appSettings.TwitterConsumerSecret);
            ConfigureProvider("google", appSettings.GoogleClientID, appSettings.GoogleClientSecret);
        }

        private void ConfigureProvider(string providerName, string publicKey, string secretKey)
        {
            if (!string.IsNullOrWhiteSpace(publicKey) && !string.IsNullOrWhiteSpace(secretKey))
            {
                var providerParams = new ProviderParams
                {
                    PublicApiKey = publicKey,
                    SecretApiKey = secretKey
                };

                switch (providerName.ToLowerInvariant())
                {
                    case "facebook":
                        _factory.AddProvider(new FacebookProvider(providerParams));
                        break;
                    case "twitter":
                        _factory.AddProvider(new TwitterProvider(providerParams));
                        break;
                    case "google":
                        _factory.AddProvider(new GoogleProvider(providerParams));
                        break;
                }
            }
            else
            {
                switch (providerName.ToLowerInvariant())
                {
                    case "facebook":
                        _factory.RemoveProvider<FacebookProvider>();
                        break;
                    case "twitter":
                        _factory.RemoveProvider<TwitterProvider>();
                        break;
                    case "google":
                        _factory.RemoveProvider<GoogleProvider>();
                        break;
                }
            }
        }

        public IEnumerable<IAuthenticationProvider> GetProviders()
        {
            return _factory.AuthenticationProviders?.Values ?? Enumerable.Empty<IAuthenticationProvider>();
        }
    }
}