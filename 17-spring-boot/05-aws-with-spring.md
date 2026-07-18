# 05 - AWS with Spring Boot

This guide covers practical AWS integration for Spring Boot 3 applications using AWS SDK for Java v2 and modern Spring patterns. The recurring theme is: use IAM roles, keep credentials out of code, configure clients as beans, add timeouts/retries deliberately, and understand each AWS service's semantics.

## Outcomes

- [ ] Configure AWS SDK v2 clients in Spring.
- [ ] Use S3 safely for object storage.
- [ ] Connect Spring Boot to RDS with operational awareness.
- [ ] Load secrets and parameters without hard-coded credentials.
- [ ] Publish and consume SQS messages idempotently.
- [ ] Validate Cognito JWTs with Spring Security.
- [ ] Understand IAM roles for ECS, EKS, and Lambda.
- [ ] Use Spring Cloud AWS and LocalStack appropriately.

## AWS SDK v2 with Spring

AWS SDK for Java v2 is non-blocking-capable, immutable-client-oriented, and region/credentials-provider based. In Spring, define SDK clients as beans, configure region from properties/environment, and let IAM roles provide credentials in AWS runtimes.

**Interview focus**

- DefaultCredentialsProvider chain.
- Region provider chain.
- Sync versus async clients.

**Production checklist**

- [ ] Do not hard-code access keys.
- [ ] Create one client bean per service/region need.
- [ ] Set timeouts/retries intentionally for production.

**Code example**

```java
@Bean
S3Client s3Client(AwsProperties props) {
    return S3Client.builder()
        .region(Region.of(props.region()))
        .credentialsProvider(DefaultCredentialsProvider.create())
        .build();
}
```

**Common pitfalls**

- Checking credentials into config.
- Creating SDK clients per request.

## S3

S3 is object storage for files, images, exports, logs, and static assets. Common Spring patterns include upload/download services, pre-signed URLs, metadata validation, event notifications, and lifecycle policies.

**Interview focus**

- Bucket/key model, not folders.
- Pre-signed URLs.
- Server-side encryption and least privilege.

**Production checklist**

- [ ] Validate content type and size before upload.
- [ ] Generate safe keys; do not trust client filenames.
- [ ] Use IAM policies scoped to required bucket prefixes.

**Code example**

```java
PutObjectRequest request = PutObjectRequest.builder()
    .bucket(bucket)
    .key(key)
    .contentType(contentType)
    .serverSideEncryption(ServerSideEncryption.AES256)
    .build();
s3.putObject(request, RequestBody.fromBytes(bytes));
```

**Common pitfalls**

- Serving private objects with public buckets.
- Using original filenames as object keys.

## RDS

RDS provides managed relational databases including PostgreSQL and SQL Server. From Spring, it is still JDBC/JPA; the main differences are networking, credentials, SSL, backups, replicas, failover, parameter groups, and connection limits.

**Interview focus**

- RDS is not a different JDBC API.
- Multi-AZ failover and connection recovery.
- IAM database authentication at a high level.

**Production checklist**

- [ ] Use Secrets Manager or IAM auth for credentials.
- [ ] Set Hikari maxLifetime below infrastructure idle timeouts.
- [ ] Test failover behavior for critical systems.

**Code example**

```yaml
spring:
  datasource:
    url: jdbc:postgresql://${DB_HOST}:${DB_PORT:5432}/${DB_NAME}
    username: ${DB_USERNAME}
    password: ${DB_PASSWORD}
```

**Common pitfalls**

- Treating RDS max connections as unlimited.
- Running migrations from every app instance without coordination.

## Secrets Manager

Secrets Manager stores and rotates secrets such as database credentials and API keys. Spring apps can fetch secrets at startup or through Spring Cloud AWS/property import mechanisms. Prefer rotation-aware designs for long-running apps.

**Interview focus**

- Secret value versus secret metadata.
- Rotation and caching.
- IAM permission `secretsmanager:GetSecretValue`.

**Production checklist**

- [ ] Cache secrets but plan refresh for rotation.
- [ ] Never log secret strings.
- [ ] Scope IAM to named secrets.

**Code example**

```java
GetSecretValueResponse response = secrets.getSecretValue(GetSecretValueRequest.builder()
    .secretId(secretId)
    .build());
```

**Common pitfalls**

- Fetching secrets on every request.
- Granting wildcard secret access.

## Parameter Store

SSM Parameter Store is useful for non-secret and simple secret configuration. It integrates well with hierarchical names such as `/prod/inventory/tax-api-url`. For high-change dynamic config, consider refresh strategy and operational ownership.

**Interview focus**

- String, StringList, SecureString.
- Hierarchical parameters.
- Parameter Store versus Secrets Manager.

**Production checklist**

- [ ] Use Parameter Store for config and Secrets Manager for rotating secrets.
- [ ] Version and document parameter names.
- [ ] Avoid excessive startup API calls.

**Code example**

```text
/prod/inventory/server-port
/prod/inventory/tax-api/base-url
/prod/inventory/features/enable-new-pricing
```

**Common pitfalls**

- Using Parameter Store as a high-throughput runtime database.
- Mixing secret and non-secret ownership without clarity.

## SQS

SQS is a managed queue with at-least-once delivery. Standard queues provide high throughput and best-effort ordering. FIFO queues provide ordering and deduplication within message groups with lower throughput. Consumers must be idempotent.

**Interview focus**

- Visibility timeout.
- DLQ redrive policy.
- Standard versus FIFO.

**Production checklist**

- [ ] Set visibility timeout longer than normal processing time.
- [ ] Use DLQs and alarms.
- [ ] Store processed message/business IDs for idempotency.

**Code example**

```java
sqs.sendMessage(SendMessageRequest.builder()
    .queueUrl(queueUrl)
    .messageBody(objectMapper.writeValueAsString(event))
    .build());
```

**Common pitfalls**

- Assuming a message is delivered exactly once.
- Deleting messages before processing succeeds.

## Cognito overview

Amazon Cognito can act as a user pool identity provider that issues JWTs. Spring Security can validate Cognito JWTs as an OAuth2 resource server using issuer URI/JWK discovery. Application authorization still needs server-side mapping from claims/scopes/groups to permissions.

**Interview focus**

- User pools versus identity pools.
- JWT issuer and JWK set.
- Groups/scopes/claims mapping.

**Production checklist**

- [ ] Validate issuer, audience/client ID when required, expiration, and signature.
- [ ] Map Cognito groups/scopes to Spring authorities deliberately.
- [ ] Do not put sensitive authorization rules only in the frontend.

**Code example**

```yaml
spring:
  security:
    oauth2:
      resourceserver:
        jwt:
          issuer-uri: https://cognito-idp.us-east-1.amazonaws.com/us-east-1_example
```

**Common pitfalls**

- Trusting decoded JWT payloads without signature validation.
- Confusing authentication with application authorization.

## IAM roles for ECS, EKS, and Lambda

In AWS runtimes, applications should receive credentials from execution roles rather than static keys. ECS task roles, EKS IRSA/pod identity, and Lambda execution roles all allow the SDK default provider chain to obtain short-lived credentials.

**Interview focus**

- ECS task role versus task execution role.
- EKS IRSA/pod identity.
- Lambda execution role.

**Production checklist**

- [ ] Grant least privilege per workload.
- [ ] Use resource ARNs and condition keys where practical.
- [ ] Let `DefaultCredentialsProvider` resolve credentials.

**Code example**

```text
ECS task role -> app permissions such as s3:PutObject
ECS execution role -> pull image, write logs
Lambda execution role -> function AWS API permissions
EKS IRSA -> service account mapped to IAM role
```

**Common pitfalls**

- Putting AWS keys in Kubernetes secrets when role-based auth is available.
- Using broad `AdministratorAccess` for applications.

## Spring Cloud AWS notes

Spring Cloud AWS can reduce integration boilerplate for SQS listeners, S3 resource access, Secrets Manager/Parameter Store config imports, and auto-configured clients. It is useful, but you still need to understand the underlying AWS semantics.

**Interview focus**

- What Spring Cloud AWS abstracts.
- Version compatibility with Boot 3.
- When direct AWS SDK use is clearer.

**Production checklist**

- [ ] Check dependency compatibility with Boot version.
- [ ] Keep AWS semantics visible in naming and error handling.
- [ ] Test local behavior with LocalStack where appropriate.

**Code example**

```java
@SqsListener("inventory-events")
void receive(InventoryEvent event) {
    handler.handle(event);
}
```

**Common pitfalls**

- Assuming framework annotations change SQS delivery semantics.
- Adding a large abstraction for one simple SDK call.

## LocalStack tip

LocalStack emulates many AWS APIs locally for development and integration tests. It is valuable for S3/SQS/Secrets workflows but does not perfectly model IAM, performance, regional failures, or every edge case.

**Interview focus**

- Endpoint override.
- Testcontainers LocalStack module.
- What local emulation cannot prove.

**Production checklist**

- [ ] Use LocalStack for fast integration feedback.
- [ ] Still run critical tests against real AWS staging where risk requires it.
- [ ] Keep local credentials fake and harmless.

**Code example**

```java
S3Client.builder()
    .endpointOverride(localstack.getEndpointOverride(S3))
    .credentialsProvider(StaticCredentialsProvider.create(AwsBasicCredentials.create("test", "test")))
    .region(Region.of(localstack.getRegion()))
    .build();
```

**Common pitfalls**

- Believing LocalStack validates IAM policies completely.
- Pointing tests at real AWS by accident.

## AWS deployment readiness checklist

- [ ] Application runs with no static AWS keys.
- [ ] IAM policy grants only required actions and resources.
- [ ] All SDK clients have a region and production-aware retry/timeout settings.
- [ ] S3 buckets block public access unless there is an explicit public hosting requirement.
- [ ] RDS credentials come from Secrets Manager or an equivalent secret mechanism.
- [ ] SQS consumers are idempotent and use DLQs.
- [ ] Cognito issuer/JWK validation is configured for API security.
- [ ] CloudWatch logs/metrics/traces are correlated with application observability.
- [ ] LocalStack is used for local tests without replacing staging validation.
- [ ] Runbooks document credential rotation, queue backlog, bucket permission issues, and database failover.

## AWS client configuration pattern

A production Spring app often centralizes AWS client creation so every client uses the same region, credentials provider, endpoint override for local tests, retry mode, and API call timeouts.

```java
@Configuration
class AwsClientConfig {
    @Bean
    SqsClient sqsClient(AwsProperties props) {
        return SqsClient.builder()
            .region(Region.of(props.region()))
            .credentialsProvider(DefaultCredentialsProvider.create())
            .overrideConfiguration(ClientOverrideConfiguration.builder()
                .apiCallTimeout(Duration.ofSeconds(5))
                .apiCallAttemptTimeout(Duration.ofSeconds(2))
                .build())
            .build();
    }
}
```

### Client lifecycle rules

- SDK clients are thread-safe and should be reused as singleton beans.
- Do not instantiate clients per request.
- Keep endpoint overrides limited to local/test profiles.
- Use fake local credentials only for LocalStack.
- Close clients gracefully when the application context shuts down.

## IAM policy examples

### S3 prefix-limited write policy

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:PutObject", "s3:GetObject", "s3:DeleteObject"],
      "Resource": "arn:aws:s3:::example-inventory-bucket/prod/inventory/*"
    }
  ]
}
```

### SQS producer/consumer policy

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["sqs:SendMessage"],
      "Resource": "arn:aws:sqs:us-east-1:123456789012:inventory-events"
    },
    {
      "Effect": "Allow",
      "Action": ["sqs:ReceiveMessage", "sqs:DeleteMessage", "sqs:ChangeMessageVisibility", "sqs:GetQueueAttributes"],
      "Resource": "arn:aws:sqs:us-east-1:123456789012:inventory-work"
    }
  ]
}
```

### Secrets Manager read policy

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "secretsmanager:GetSecretValue",
      "Resource": "arn:aws:secretsmanager:us-east-1:123456789012:secret:prod/inventory/db-*"
    }
  ]
}
```

## RDS operational checklist

- Use private subnets and security groups that allow only required app traffic.
- Enforce TLS where required by policy.
- Keep app connection pools below database capacity across all replicas/tasks/pods.
- Configure backups, retention, maintenance windows, and monitoring.
- Know failover behavior: DNS changes, broken connections, retry windows, and app recovery.
- Use migrations carefully; avoid every app instance running destructive migrations concurrently.
- Monitor CPU, memory, storage, IOPS, locks, deadlocks, connections, replication lag, and slow queries.
- Test restore, not just backup creation.

## SQS consumer design in Spring

### Handler shape

```java
@Component
class InventoryMessageHandler {
    @Transactional
    public void handle(InventoryReservedEvent event) {
        if (processedMessageStore.alreadyProcessed(event.messageId())) {
            return;
        }
        inventoryService.applyReservation(event);
        processedMessageStore.markProcessed(event.messageId());
    }
}
```

### Visibility timeout rules

- Visibility timeout should exceed normal processing time.
- Extend visibility for rare long-running work or break work into smaller messages.
- If processing fails, do not delete the message; let it retry or move to DLQ.
- Redrive DLQ messages only after the root cause is fixed.

### Message payload rules

- Include event type, version, message ID, aggregate ID, occurredAt, and trace/correlation ID.
- Keep payloads small; store large files in S3 and send references.
- Avoid putting secrets or sensitive personal data in messages.

## Cognito + Spring Security claim mapping

Cognito access tokens commonly include scopes and client information. ID tokens include user identity claims. APIs should usually authorize with access tokens, not ID tokens.

```java
@Bean
JwtAuthenticationConverter jwtAuthenticationConverter() {
    JwtGrantedAuthoritiesConverter scopes = new JwtGrantedAuthoritiesConverter();
    scopes.setAuthorityPrefix("SCOPE_");
    scopes.setAuthoritiesClaimName("scope");

    JwtAuthenticationConverter converter = new JwtAuthenticationConverter();
    converter.setJwtGrantedAuthoritiesConverter(scopes);
    return converter;
}
```

### Authorization examples

- `@PreAuthorize("hasAuthority('SCOPE_product:write')")`
- `@PreAuthorize("hasRole('ADMIN')")`
- `@PreAuthorize("@tenantSecurity.canAccess(authentication, #tenantId)")`

## Secrets rotation strategy

Secrets Manager can rotate database credentials, but application behavior depends on how the datasource uses credentials.

### Startup-only load

- Simple and common.
- App must restart to pick up rotated credentials unless the database keeps old credentials active during rollout.
- Works well with deployment automation that restarts services after rotation.

### Runtime refresh

- More complex.
- Requires recreating datasource/pool or using a library that refreshes credentials.
- Must avoid interrupting in-flight transactions.

### Interview answer

"I would avoid hard-coded credentials and use IAM to read the secret. For rotation, I would first understand whether restart-on-rotation is acceptable. If not, I would design datasource refresh carefully and test it under load because connection pools hold credentials in existing connections."

## LocalStack + Testcontainers pattern

```java
@Testcontainers
@SpringBootTest
class S3StorageServiceTest {
    @Container
    static LocalStackContainer localstack = new LocalStackContainer(DockerImageName.parse("localstack/localstack:latest"))
        .withServices(LocalStackContainer.Service.S3, LocalStackContainer.Service.SQS);

    @DynamicPropertySource
    static void aws(DynamicPropertyRegistry registry) {
        registry.add("app.aws.region", localstack::getRegion);
        registry.add("app.aws.endpoint-override", () -> localstack.getEndpointOverride(LocalStackContainer.Service.S3).toString());
    }
}
```

### Local testing cautions

- Never point local tests at production AWS accounts.
- Use account/region names that are visibly fake.
- Create buckets/queues/secrets during test setup.
- Do not assume LocalStack perfectly models IAM or service quotas.

## Cloud deployment interview map

| Runtime | Credential mechanism | Spring concern | Common pitfall |
| --- | --- | --- | --- |
| ECS | Task role | No static keys; task role policy | Confusing task execution role with task role |
| EKS | IRSA or Pod Identity | Service account annotation/association | Falling back to node role with excessive permissions |
| Lambda | Execution role | Cold start, client reuse, timeout alignment | Creating SDK clients on every invocation |
| EC2 | Instance profile | Metadata service access | Sharing one broad role across unrelated apps |

## AWS incident runbooks

### S3 access denied

1. Check bucket policy, IAM role policy, object key prefix, KMS key policy if used, and block-public-access settings.
2. Verify the running workload assumed the expected role.
3. Inspect CloudTrail for denied action/resource.
4. Confirm object ownership and ACL assumptions, especially across accounts.

### SQS backlog growing

1. Check consumer error logs and DLQ count.
2. Compare incoming rate with processing throughput.
3. Inspect visibility timeout and message age.
4. Scale consumers only if the downstream database/API can handle the added load.
5. Redrive DLQ after fixing poison messages or code defects.

### RDS connection exhaustion

1. Count app replicas times Hikari maximum pool size.
2. Check active versus idle connections and waiting threads.
3. Look for slow queries, locks, long transactions, or leaked connections.
4. Reduce pool sizes or scale database only after understanding the bottleneck.
