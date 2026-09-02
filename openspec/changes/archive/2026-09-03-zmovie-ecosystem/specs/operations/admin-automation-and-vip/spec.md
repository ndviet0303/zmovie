## MODIFIED Requirements

### Requirement: VIP Membership Tiers and VietQR Payment Verification
The system SHALL offer VIP subscription packages (e.g. 1 month, 3 months, 1 year) that unlock exclusive perks, generate dynamic VietQR payment codes, and MUST automatically verify incoming bank transfers within 3 seconds using authenticated PayOS or SePay webhooks with HMAC-SHA256 signature verification.

#### Scenario: User initiates VIP checkout
- **WHEN** an authenticated user selects a VIP subscription tier
- **THEN** the system SHALL create a pending billing transaction with a unique order code (e.g. `ZM_VIP_1024_ABC12`) and generate a dynamic VietQR image with exact amount and payment memo

#### Scenario: Automatic subscription activation via webhook
- **WHEN** the PayOS or SePay webhook endpoint receives a payment notification with a valid HMAC signature and matching order code
- **THEN** the system SHALL extend the user's `VipExpiresAt` timestamp in persistent database storage, publish a `VipActivatedDomainEvent`, and broadcast an instant unlock signal to the user's active client session via SignalR

#### Scenario: Webhook idempotency and signature validation
- **WHEN** an incoming payment webhook has an invalid cryptographic signature or carries an already processed transaction ID
- **THEN** the system SHALL reject the request with HTTP 401/400 or acknowledge idempotently without duplicate subscription extensions

## ADDED Requirements

### Requirement: Self-Healing Stream Health Monitoring Bot
The system SHALL execute an autonomous background health checker that periodically probes active video streams to identify broken URLs, 403 Forbidden errors, or dead CDN endpoints, and automatically reassigns healthy fallback sources.

#### Scenario: Automated stream health verification
- **WHEN** the scheduled stream health monitor runs across catalog titles
- **THEN** the worker SHALL issue HTTP HEAD requests to primary `.m3u8` manifests and record latency and status codes

#### Scenario: Autonomous healing of defective stream
- **WHEN** a primary stream returns HTTP 404, 403, or connection timeouts for 3 consecutive checks
- **THEN** the system SHALL mark the primary source as degraded, promote the highest-priority healthy backup source to active, and log an ops recovery event

### Requirement: Anti-Leech Streaming Protection and Rate Limiting
The system SHALL protect high-bandwidth video streams and Cloudflare R2 assets from unauthorized hotlinking and bandwidth theft by enforcing short-lived HMAC signed tokens and origin verification.

#### Scenario: Authenticated playback token generation
- **WHEN** a client requests playback details for a protected title
- **THEN** the system SHALL generate a short-lived signed playback token bound to the viewer's session and IP address

#### Scenario: Rejecting unauthorized external leech request
- **WHEN** an external third-party domain attempts to embed or directly fetch stream segments without a valid token or with a disallowed Referer header
- **THEN** the streaming edge proxy SHALL reject the request with HTTP 403 Forbidden

### Requirement: Telegram Operations Alert Bot
The system SHALL integrate with Telegram Bot API to deliver real-time operations alerts to platform administrators for stream failures, crawler exceptions, and user error reports with interactive resolution buttons.

#### Scenario: Alerting on broken stream detection
- **WHEN** the self-healing bot detects an unrecoverable stream error or a title receives more than 3 user broken-link reports in 1 hour
- **THEN** the system SHALL dispatch an urgent alert message to the ops Telegram channel detailing the title name, episode, error code, and an inline button to "Cào lại ngay (Retry Crawl)"

#### Scenario: Admin triggers 1-click crawl retry from Telegram
- **WHEN** an administrator clicks the inline action button in the Telegram alert
- **THEN** the Telegram bot webhook SHALL authenticate the admin ID and trigger a targeted background crawler job for that specific title
