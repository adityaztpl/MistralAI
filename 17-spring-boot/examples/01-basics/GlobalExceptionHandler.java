package com.example.demo;

import jakarta.servlet.http.HttpServletRequest;
import java.net.URI;
import java.time.Instant;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.http.HttpStatus;
import org.springframework.http.ProblemDetail;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.validation.FieldError;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.context.request.ServletWebRequest;
import org.springframework.web.context.request.WebRequest;

@RestControllerAdvice
class GlobalExceptionHandler {

    private static final Logger log = LoggerFactory.getLogger(GlobalExceptionHandler.class);

    @ExceptionHandler(ProductNotFoundException.class)
    ProblemDetail productNotFound(ProductNotFoundException ex, HttpServletRequest request) {
        ProblemDetail detail = ProblemDetail.forStatusAndDetail(HttpStatus.NOT_FOUND, ex.getMessage());
        detail.setTitle("Product not found");
        detail.setType(URI.create("https://errors.example.com/product-not-found"));
        addCommonProperties(detail, request);
        return detail;
    }

    @ExceptionHandler(DuplicateSkuException.class)
    ProblemDetail duplicateSku(DuplicateSkuException ex, HttpServletRequest request) {
        ProblemDetail detail = ProblemDetail.forStatusAndDetail(HttpStatus.CONFLICT, ex.getMessage());
        detail.setTitle("Duplicate product SKU");
        detail.setType(URI.create("https://errors.example.com/duplicate-sku"));
        addCommonProperties(detail, request);
        return detail;
    }

    @ExceptionHandler(MethodArgumentNotValidException.class)
    ProblemDetail validation(MethodArgumentNotValidException ex, HttpServletRequest request) {
        ProblemDetail detail = ProblemDetail.forStatus(HttpStatus.BAD_REQUEST);
        detail.setTitle("Validation failed");
        detail.setDetail("One or more request fields are invalid.");
        detail.setType(URI.create("https://errors.example.com/validation"));

        Map<String, String> fields = new LinkedHashMap<>();
        for (FieldError error : ex.getBindingResult().getFieldErrors()) {
            fields.putIfAbsent(error.getField(), error.getDefaultMessage());
        }
        detail.setProperty("fields", fields);
        addCommonProperties(detail, request);
        return detail;
    }

    @ExceptionHandler(HttpMessageNotReadableException.class)
    ProblemDetail malformedJson(HttpMessageNotReadableException ex, HttpServletRequest request) {
        ProblemDetail detail = ProblemDetail.forStatusAndDetail(
            HttpStatus.BAD_REQUEST,
            "Request body could not be parsed. Check JSON syntax and field types."
        );
        detail.setTitle("Malformed request body");
        detail.setType(URI.create("https://errors.example.com/malformed-json"));
        addCommonProperties(detail, request);
        return detail;
    }

    @ExceptionHandler(DataIntegrityViolationException.class)
    ProblemDetail dataIntegrity(DataIntegrityViolationException ex, HttpServletRequest request) {
        ProblemDetail detail = ProblemDetail.forStatusAndDetail(
            HttpStatus.CONFLICT,
            "The request conflicts with a database constraint."
        );
        detail.setTitle("Data integrity violation");
        detail.setType(URI.create("https://errors.example.com/data-integrity"));
        addCommonProperties(detail, request);
        return detail;
    }

    @ExceptionHandler(Exception.class)
    ProblemDetail unexpected(Exception ex, WebRequest webRequest) {
        String errorId = UUID.randomUUID().toString();
        HttpServletRequest request = webRequest instanceof ServletWebRequest servletWebRequest
            ? servletWebRequest.getRequest()
            : null;
        log.error("Unexpected API error id={}", errorId, ex);

        ProblemDetail detail = ProblemDetail.forStatusAndDetail(
            HttpStatus.INTERNAL_SERVER_ERROR,
            "An unexpected error occurred. Reference errorId when contacting support."
        );
        detail.setTitle("Internal server error");
        detail.setType(URI.create("https://errors.example.com/internal"));
        detail.setProperty("errorId", errorId);
        if (request != null) {
            addCommonProperties(detail, request);
        }
        return detail;
    }

    private void addCommonProperties(ProblemDetail detail, HttpServletRequest request) {
        detail.setInstance(URI.create(request.getRequestURI()));
        detail.setProperty("timestamp", Instant.now().toString());
        String requestId = request.getHeader("X-Request-Id");
        if (requestId != null && !requestId.isBlank()) {
            detail.setProperty("requestId", requestId);
        }
    }
}
