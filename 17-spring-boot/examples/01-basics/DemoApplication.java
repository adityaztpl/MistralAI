package com.example.demo;

import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.info.Contact;
import io.swagger.v3.oas.models.info.Info;
import io.swagger.v3.oas.models.info.License;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Positive;
import java.time.Clock;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.actuate.autoconfigure.security.servlet.EndpointRequest;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.boot.context.properties.ConfigurationPropertiesScan;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.validation.annotation.Validated;

@SpringBootApplication
@ConfigurationPropertiesScan
public class DemoApplication {

    public static void main(String[] args) {
        SpringApplication.run(DemoApplication.class, args);
    }

    @Bean
    Clock clock() {
        return Clock.systemUTC();
    }

    @Bean
    OpenAPI inventoryOpenApi(ApiDocsProperties docs) {
        return new OpenAPI()
            .info(new Info()
                .title(docs.title())
                .version(docs.version())
                .description(docs.description())
                .contact(new Contact().name("API Support").email(docs.supportEmail()))
                .license(new License().name("Internal study example")));
    }
}

@ConfigurationProperties(prefix = "app.api-docs")
@Validated
record ApiDocsProperties(
    @NotBlank String title,
    @NotBlank String version,
    @NotBlank String description,
    @NotBlank String supportEmail
) {
    ApiDocsProperties {
        if (title == null) {
            title = "Inventory API";
        }
        if (version == null) {
            version = "v1";
        }
        if (description == null) {
            description = "Spring Boot 3 study API";
        }
        if (supportEmail == null) {
            supportEmail = "support@example.com";
        }
    }
}

@ConfigurationProperties(prefix = "app.limits")
@Validated
record ApiLimitProperties(
    @Positive int maxPageSize
) {
    ApiLimitProperties {
        if (maxPageSize == 0) {
            maxPageSize = 100;
        }
    }
}

@Configuration
class ActuatorSecurityExample {

    @Bean
    SecurityFilterChain actuatorSecurity(HttpSecurity http) throws Exception {
        return http
            .securityMatcher(EndpointRequest.toAnyEndpoint())
            .authorizeHttpRequests(auth -> auth
                .requestMatchers(EndpointRequest.to("health", "info")).permitAll()
                .anyRequest().hasRole("OPS"))
            .httpBasic(Customizer.withDefaults())
            .build();
    }
}
