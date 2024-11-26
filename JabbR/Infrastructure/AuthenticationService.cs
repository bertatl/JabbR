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
            if (!String.IsNullOrWhiteSpace(publicKey) && !String.IsNullOrWhiteSpace(secretKey))
            {
                var providerParams = new ProviderParams
                {
                    PublicApiKey = publicKey,
                    SecretApiKey = secretKey
                };

                // Use reflection to create the provider instance
                var providerType = Type.GetType($"SimpleAuthentication.Providers.{providerName}Provider, SimpleAuthentication");
                if (providerType != null)
                {
                    var provider = Activator.CreateInstance(providerType, providerParams) as IAuthenticationProvider;
                    if (provider != null)
                    {
                        _factory.AddProvider(provider);
                    }
                }
            }
            else
            {
                // Use generic method to remove provider
                var method = typeof(AuthenticationProviderFactory).GetMethod("RemoveProvider");
                var genericMethod = method.MakeGenericMethod(Type.GetType($"SimpleAuthentication.Providers.{providerName}Provider, SimpleAuthentication"));
                genericMethod.Invoke(_factory, null);
            }
        }

        public IEnumerable<IAuthenticationProvider> GetProviders()
        {
            return _factory.AuthenticationProviders?.Values ?? Enumerable.Empty<IAuthenticationProvider>();
        }
    }
}