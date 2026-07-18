package com.example.demo.aws;

import java.io.IOException;
import java.io.InputStream;
import java.net.URI;
import java.time.Duration;
import java.util.Locale;
import java.util.Set;
import java.util.UUID;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.stereotype.Service;
import org.springframework.util.unit.DataSize;
import software.amazon.awssdk.core.ResponseInputStream;
import software.amazon.awssdk.core.sync.RequestBody;
import software.amazon.awssdk.services.s3.S3Client;
import software.amazon.awssdk.services.s3.model.GetObjectRequest;
import software.amazon.awssdk.services.s3.model.GetObjectResponse;
import software.amazon.awssdk.services.s3.model.NoSuchKeyException;
import software.amazon.awssdk.services.s3.model.PutObjectRequest;
import software.amazon.awssdk.services.s3.model.ServerSideEncryption;
import software.amazon.awssdk.services.s3.presigner.S3Presigner;
import software.amazon.awssdk.services.s3.presigner.model.GetObjectPresignRequest;
import software.amazon.awssdk.services.s3.presigner.model.PresignedGetObjectRequest;

@Service
@EnableConfigurationProperties(S3StorageProperties.class)
public class S3StorageService {

    private static final Set<String> ALLOWED_CONTENT_TYPES = Set.of(
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    );

    private final S3Client s3Client;
    private final S3Presigner presigner;
    private final S3StorageProperties properties;

    public S3StorageService(S3Client s3Client, S3Presigner presigner, S3StorageProperties properties) {
        this.s3Client = s3Client;
        this.presigner = presigner;
        this.properties = properties;
    }

    public StoredObject uploadProductAsset(UUID productId, String originalFilename, String contentType, long contentLength, InputStream inputStream) throws IOException {
        validateUpload(contentType, contentLength);
        String key = buildProductAssetKey(productId, originalFilename, contentType);

        PutObjectRequest request = PutObjectRequest.builder()
            .bucket(properties.bucket())
            .key(key)
            .contentType(contentType)
            .contentLength(contentLength)
            .serverSideEncryption(ServerSideEncryption.AES256)
            .metadata(java.util.Map.of(
                "product-id", productId.toString(),
                "original-filename", sanitizeMetadata(originalFilename)
            ))
            .build();

        s3Client.putObject(request, RequestBody.fromInputStream(inputStream, contentLength));
        return new StoredObject(properties.bucket(), key, contentType, contentLength);
    }

    public DownloadedObject download(String key) {
        try {
            ResponseInputStream<GetObjectResponse> response = s3Client.getObject(GetObjectRequest.builder()
                .bucket(properties.bucket())
                .key(key)
                .build());
            return new DownloadedObject(
                key,
                response.response().contentType(),
                response.response().contentLength(),
                response
            );
        } catch (NoSuchKeyException ex) {
            throw new StorageObjectNotFoundException(key, ex);
        }
    }

    public URI createPresignedDownloadUrl(String key) {
        GetObjectRequest getObjectRequest = GetObjectRequest.builder()
            .bucket(properties.bucket())
            .key(key)
            .build();
        GetObjectPresignRequest presignRequest = GetObjectPresignRequest.builder()
            .signatureDuration(properties.presignedUrlTtl())
            .getObjectRequest(getObjectRequest)
            .build();
        PresignedGetObjectRequest presigned = presigner.presignGetObject(presignRequest);
        return presigned.url().toURI();
    }

    private void validateUpload(String contentType, long contentLength) {
        if (!ALLOWED_CONTENT_TYPES.contains(contentType)) {
            throw new IllegalArgumentException("Unsupported content type: " + contentType);
        }
        if (contentLength <= 0 || contentLength > properties.maxUploadSize().toBytes()) {
            throw new IllegalArgumentException("Upload size must be between 1 byte and " + properties.maxUploadSize());
        }
    }

    private String buildProductAssetKey(UUID productId, String originalFilename, String contentType) {
        String extension = extensionFor(contentType, originalFilename);
        return "%s/products/%s/assets/%s%s".formatted(
            trimSlashes(properties.keyPrefix()),
            productId,
            UUID.randomUUID(),
            extension
        );
    }

    private String extensionFor(String contentType, String originalFilename) {
        String lowerName = originalFilename == null ? "" : originalFilename.toLowerCase(Locale.ROOT);
        if (lowerName.endsWith(".jpg") || lowerName.endsWith(".jpeg")) {
            return ".jpg";
        }
        if (lowerName.endsWith(".png")) {
            return ".png";
        }
        if (lowerName.endsWith(".webp")) {
            return ".webp";
        }
        if (lowerName.endsWith(".pdf")) {
            return ".pdf";
        }
        return switch (contentType) {
            case "image/jpeg" -> ".jpg";
            case "image/png" -> ".png";
            case "image/webp" -> ".webp";
            case "application/pdf" -> ".pdf";
            default -> "";
        };
    }

    private String sanitizeMetadata(String value) {
        if (value == null || value.isBlank()) {
            return "unknown";
        }
        return value.replaceAll("[\\r\\n]", " ").trim();
    }

    private String trimSlashes(String prefix) {
        if (prefix == null || prefix.isBlank()) {
            return "app";
        }
        return prefix.replaceAll("^/+|/+$", "");
    }
}

@ConfigurationProperties(prefix = "app.aws.s3")
record S3StorageProperties(
    String bucket,
    String keyPrefix,
    DataSize maxUploadSize,
    Duration presignedUrlTtl
) {
    S3StorageProperties {
        if (keyPrefix == null || keyPrefix.isBlank()) {
            keyPrefix = "inventory";
        }
        if (maxUploadSize == null) {
            maxUploadSize = DataSize.ofMegabytes(10);
        }
        if (presignedUrlTtl == null) {
            presignedUrlTtl = Duration.ofMinutes(10);
        }
    }
}

record StoredObject(String bucket, String key, String contentType, long contentLength) {
}

record DownloadedObject(String key, String contentType, long contentLength, InputStream body) {
}

class StorageObjectNotFoundException extends RuntimeException {
    StorageObjectNotFoundException(String key, Throwable cause) {
        super("S3 object was not found: " + key, cause);
    }
}
