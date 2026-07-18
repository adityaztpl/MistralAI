package com.example.demo.aws;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.net.URI;
import java.time.Duration;
import java.util.Map;
import javax.sql.DataSource;
import org.springframework.boot.autoconfigure.jdbc.DataSourceProperties;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Profile;
import software.amazon.awssdk.auth.credentials.DefaultCredentialsProvider;
import software.amazon.awssdk.core.client.config.ClientOverrideConfiguration;
import software.amazon.awssdk.regions.Region;
import software.amazon.awssdk.services.secretsmanager.SecretsManagerClient;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueRequest;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueResponse;

@Configuration
@EnableConfigurationProperties({AwsClientProperties.class, DatabaseSecretProperties.class})
public class SecretsConfig {

    @Bean
    SecretsManagerClient secretsManagerClient(AwsClientProperties properties) {
        var builder = SecretsManagerClient.builder()
            .region(Region.of(properties.region()))
            .credentialsProvider(DefaultCredentialsProvider.create())
            .overrideConfiguration(ClientOverrideConfiguration.builder()
                .apiCallTimeout(properties.apiCallTimeout())
                .apiCallAttemptTimeout(properties.apiCallAttemptTimeout())
                .build());

        if (properties.endpointOverride() != null && !properties.endpointOverride().isBlank()) {
            builder.endpointOverride(URI.create(properties.endpointOverride()));
        }
        return builder.build();
    }

    @Bean
    @Profile("aws-secrets")
    DataSource dataSourceFromSecret(
        SecretsManagerClient secretsManagerClient,
        DatabaseSecretProperties secretProperties,
        ObjectMapper objectMapper
    ) {
        GetSecretValueResponse response = secretsManagerClient.getSecretValue(GetSecretValueRequest.builder()
            .secretId(secretProperties.secretId())
            .build());

        DatabaseCredentials credentials = parseCredentials(objectMapper, response.secretString());

        DataSourceProperties dataSourceProperties = new DataSourceProperties();
        dataSourceProperties.setUrl(credentials.jdbcUrl());
        dataSourceProperties.setUsername(credentials.username());
        dataSourceProperties.setPassword(credentials.password());
        return dataSourceProperties.initializeDataSourceBuilder().build();
    }

    private DatabaseCredentials parseCredentials(ObjectMapper objectMapper, String secretString) {
        if (secretString == null || secretString.isBlank()) {
            throw new IllegalStateException("Database secret value is empty");
        }
        try {
            DatabaseCredentials credentials = objectMapper.readValue(secretString, DatabaseCredentials.class);
            credentials.validate();
            return credentials;
        } catch (JsonProcessingException ex) {
            throw new IllegalStateException("Database secret is not valid JSON", ex);
        }
    }
}

@ConfigurationProperties(prefix = "app.aws")
record AwsClientProperties(
    String region,
    String endpointOverride,
    Duration apiCallTimeout,
    Duration apiCallAttemptTimeout
) {
    AwsClientProperties {
        if (region == null || region.isBlank()) {
            region = "us-east-1";
        }
        if (apiCallTimeout == null) {
            apiCallTimeout = Duration.ofSeconds(5);
        }
        if (apiCallAttemptTimeout == null) {
            apiCallAttemptTimeout = Duration.ofSeconds(2);
        }
    }
}

@ConfigurationProperties(prefix = "app.aws.secrets.database")
record DatabaseSecretProperties(String secretId) {
}

record DatabaseCredentials(
    String jdbcUrl,
    String username,
    String password,
    Map<String, String> metadata
) {
    void validate() {
        if (jdbcUrl == null || jdbcUrl.isBlank()) {
            throw new IllegalStateException("Database secret is missing jdbcUrl");
        }
        if (username == null || username.isBlank()) {
            throw new IllegalStateException("Database secret is missing username");
        }
        if (password == null || password.isBlank()) {
            throw new IllegalStateException("Database secret is missing password");
        }
    }
}
