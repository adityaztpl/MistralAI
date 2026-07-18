# REST vs gRPC vs SignalR

## The decision

Pick the communication style between clients and services. The important dimensions are client type, latency, streaming direction, protocol support, schema evolution, observability, load balancing, and developer ergonomics.

## Quick default

- Use **REST** for public HTTP APIs, browser/mobile clients, CRUD resources, broad compatibility, and easy debugging.
- Use **gRPC** for service-to-service calls with strong contracts, low latency, binary payloads, streaming, and polyglot internal systems.
- Use **SignalR** for real-time bidirectional browser/app experiences such as notifications, presence, collaborative updates, and chat.

## Comparison

| Force | REST | gRPC | SignalR |
|---|---|---|---|
| Transport | HTTP/JSON | HTTP/2 + Protobuf | WebSockets/SSE/long polling abstraction |
| Best client | Browser/mobile/public clients | Internal services | Browser/mobile real-time clients |
| Contract | OpenAPI/JSON schema | `.proto` IDL | Hub methods/messages |
| Debugging | Easy with curl/devtools | Needs grpcurl/tools | Needs connection/event tooling |
| Streaming | SSE/chunked possible | First-class unary/server/client/bidi | First-class push/bidirectional app messages |
| Payload | Human-readable JSON | Compact binary | Usually JSON or MessagePack |
| Public API fit | Excellent | Sometimes, but harder for browsers | For realtime features, not general CRUD |
| Browser support | Native fetch | Requires gRPC-Web for browsers | Strong through client library |

## Use REST when

- You expose an API to browsers, mobile apps, partners, or unknown clients.
- CRUD/resource semantics are natural.
- You want simple caching, documentation, and inspection.
- You need broad infrastructure compatibility.
- Human-debuggability matters.

Good REST practices:

- Resource-oriented URLs.
- Correct status codes.
- Idempotency keys for retries on creates/actions.
- Pagination, filtering, sorting.
- Consistent error body.
- OpenAPI documentation.
- ETags or cache headers when useful.

## Use gRPC when

- Services call services inside controlled infrastructure.
- Performance and payload size matter.
- Strong typed contracts across languages matter.
- Streaming is part of the core interaction.
- You own both client and server deployment.

Good gRPC practices:

- Design proto contracts for evolution.
- Add deadlines/cancellation.
- Map errors consistently.
- Use interceptors for auth, telemetry, retries where appropriate.
- Be careful exposing gRPC directly to browsers; consider gRPC-Web or a BFF.

## Use SignalR when

- The product needs server push or bidirectional interaction.
- Examples: chat, streaming assistant tokens, live notifications, dashboards, collaborative editing hints, presence.
- You need fallback transports and connection management above raw WebSocket.
- Clients subscribe to groups/users/tenants.

Good SignalR practices:

- Authenticate connections.
- Authorize group membership server-side.
- Handle reconnect and missed messages.
- Persist important events; do not rely on transient socket delivery for critical state.
- Backpressure high-volume messages.

## Trade-offs

REST:

- Simple and universal, but less efficient for high-throughput internal calls.
- Streaming is possible but not as strongly modeled as gRPC.

gRPC:

- Efficient and strongly typed, but harder for browser/public API consumers.
- Binary protocol makes ad hoc debugging less convenient.

SignalR:

- Excellent for real-time UX, but not a replacement for durable API commands/queries.
- Connection state, scaling, and missed events must be designed.

## Interview answer script

```text
For a public browser-facing API I would default to REST because it is compatible, easy to document with OpenAPI, and easy to debug. For internal service-to-service communication where both sides are controlled and low latency or streaming matters, I would consider gRPC. For real-time browser features like chat, presence, notifications, or token streaming, I would use SignalR, usually alongside REST for durable commands and queries.

The trade-off is compatibility versus efficiency versus real-time connection management. I would not make one protocol carry every workload if a combination gives a cleaner architecture.
```

## Example architecture

```text
React/Angular SPA
  -> REST: login, list documents, create chat session
  -> SignalR: receive streamed tokens and notifications
ASP.NET API
  -> gRPC: call internal retrieval/reranking service
  -> SQL/vector store
```
