package com.example.demo.resilience;

import io.github.resilience4j.bulkhead.BulkheadConfig;
import io.github.resilience4j.circuitbreaker.CircuitBreakerConfig;
import io.github.resilience4j.core.IntervalFunction;
import io.github.resilience4j.ratelimiter.RateLimiterConfig;
import io.github.resilience4j.retry.RetryConfig;
import io.github.resilience4j.timelimiter.TimeLimiterConfig;
import java.io.IOException;
import java.time.Duration;
import java.util.concurrent.TimeoutException;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.reactive.function.client.WebClientRequestException;
import org.springframework.web.reactive.function.client.WebClientResponseException;

@Configuration
public class ResilienceConfig {

    @Bean
    CircuitBreakerConfig pricingCircuitBreakerConfig() {
        return CircuitBreakerConfig.custom()
            .failureRateThreshold(50.0f)
            .slowCallRateThreshold(50.0f)
            .slowCallDurationThreshold(Duration.ofSeconds(2))
            .minimumNumberOfCalls(20)
            .slidingWindowSize(50)
            .permittedNumberOfCallsInHalfOpenState(5)
            .waitDurationInOpenState(Duration.ofSeconds(30))
            .recordException(this::isTransientFailure)
            .ignoreExceptions(IllegalArgumentException.class)
            .build();
    }

    @Bean
    RetryConfig pricingRetryConfig() {
        return RetryConfig.custom()
            .maxAttempts(3)
            .intervalFunction(IntervalFunction.ofExponentialBackoff(Duration.ofMillis(100), 2.0))
            .retryOnException(this::isRetryable)
            .build();
    }

    @Bean
    TimeLimiterConfig pricingTimeLimiterConfig() {
        return TimeLimiterConfig.custom()
            .timeoutDuration(Duration.ofSeconds(3))
            .cancelRunningFuture(true)
            .build();
    }

    @Bean
    BulkheadConfig pricingBulkheadConfig() {
        return BulkheadConfig.custom()
            .maxConcurrentCalls(25)
            .maxWaitDuration(Duration.ofMillis(50))
            .build();
    }

    @Bean
    RateLimiterConfig publicApiRateLimiterConfig() {
        return RateLimiterConfig.custom()
            .limitForPeriod(100)
            .limitRefreshPeriod(Duration.ofSeconds(1))
            .timeoutDuration(Duration.ZERO)
            .build();
    }

    private boolean isRetryable(Throwable throwable) {
        if (throwable instanceof WebClientResponseException responseException) {
            int status = responseException.getStatusCode().value();
            return status == 429 || status >= 500;
        }
        return throwable instanceof TimeoutException
            || throwable instanceof IOException
            || throwable instanceof WebClientRequestException;
    }

    private boolean isTransientFailure(Throwable throwable) {
        if (throwable instanceof WebClientResponseException responseException) {
            int status = responseException.getStatusCode().value();
            return status == 429 || status >= 500;
        }
        return throwable instanceof TimeoutException
            || throwable instanceof IOException
            || throwable instanceof WebClientRequestException;
    }
}
