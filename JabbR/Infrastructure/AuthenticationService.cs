using System;
using System.Collections.Generic;
using System.Linq;
using JabbR.Services;
using SimpleAuthentication;
using SimpleAuthentication.Core;
using System;
using System.Collections.Generic;
using System.Linq;

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
            if (!String.IsNullOrWhiteSpace(publicKey) && !String.IsNullOrWhiteSpace(secretKey))
            {
                var provider = _factory.CreateProvider(providerName, new ProviderParams
                {
                    PublicApiKey = publicKey,
                    SecretApiKey = secretKey
                });

                if (provider != null)
                {
                    _factory.AddProvider(provider);
                }
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
}