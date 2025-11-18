using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BooksCatalog.Infrastructure.Configuration
{
    public sealed class SecretsManagerConnectionStringProvider : IDbConnectionStringProvider
    {
        private readonly string _connectionString;

        public SecretsManagerConnectionStringProvider(
            IConfiguration configuration,
            IAmazonSecretsManager secretsManager,
            ILogger<SecretsManagerConnectionStringProvider> logger)
        {
            // 1) Intento leer nombre del secret
            var secretName = configuration["BooksDb:SecretName"]
                             ?? configuration["BooksDb__SecretName"]; // por si viene de env var

            // 2) Connection string directa (para local)
            var directConnectionString = configuration.GetConnectionString("BooksDb");

            if (string.IsNullOrWhiteSpace(secretName))
            {
                if (string.IsNullOrWhiteSpace(directConnectionString))
                {
                    throw new InvalidOperationException(
                        "Neither BooksDb:SecretName nor ConnectionStrings:BooksDb are configured.");
                }

                _connectionString = directConnectionString;
                return;
            }

            try
            {
                var request = new GetSecretValueRequest
                {
                    SecretId = secretName
                };

                var response = secretsManager.GetSecretValueAsync(request)
                                             .GetAwaiter()
                                             .GetResult();

                if (!string.IsNullOrEmpty(response.SecretString))
                {
                    // Soporte para dos formas:
                    // 1) Secret es la connection string pura.
                    // 2) Secret es un JSON con la key ConnectionStrings__BooksDb.
                    try
                    {
                        var doc = JsonDocument.Parse(response.SecretString);
                        if (doc.RootElement.TryGetProperty("ConnectionStrings__BooksDb", out var csProp))
                        {
                            _connectionString = csProp.GetString()
                                ?? throw new InvalidOperationException("ConnectionStrings__BooksDb is null in secret.");
                        }
                        else
                        {
                            // No hay JSON con esa key, asumimos que el secret es la connection string pura
                            _connectionString = response.SecretString;
                        }
                    }
                    catch (JsonException)
                    {
                        // No es JSON, asumimos connection string pura
                        _connectionString = response.SecretString;
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Secret '{secretName}' does not contain a SecretString value.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving DB connection from Secrets Manager. Falling back to ConnectionStrings:BooksDb.");

                if (string.IsNullOrWhiteSpace(directConnectionString))
                {
                    throw;
                }

                _connectionString = directConnectionString;
            }
        }

        public string GetConnectionString() => _connectionString;
    }
}
