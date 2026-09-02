## Purpose

Automates catalog synchronization via NguonC background scheduling, provisions 3 self-hosted Cloudflare R2 demo benchmark movies, provides viewing analytics, and manages VIP memberships with automated VietQR payment verification.

## Requirements

### Requirement: Cloudflare R2 Benchmark Demo Titles Seeding
The system SHALL provide a reproducible provisioning and seeding command that registers exactly 3 high-reliability demo movies hosted in Cloudflare R2 object storage with guaranteed HLS endpoints and full metadata.

#### Scenario: Running R2 demo seeding
- **WHEN** an administrator or deployment script triggers the R2 demo seed command
- **THEN** the system SHALL upsert the 3 benchmark movies into the catalog with their respective Cloudflare R2 m3u8 stream URLs, posters, and featured flags

### Requirement: Automated NguonC Crawler Scheduler
The system SHALL run a configurable background crawler task that periodically synchronizes new catalog releases and episode updates from NguonC API (`https://phim.nguonc.com/api/films/phim-moi-cap-nhat`).

#### Scenario: Scheduled incremental crawl from NguonC
- **WHEN** the configured crawler schedule timer triggers (e.g. every 4 hours)
- **THEN** the background service SHALL ingest the latest pages from NguonC, map casts/director/category, import newly available episodes, and record execution metrics

#### Scenario: Manual crawl trigger from admin panel
- **WHEN** an administrator clicks the "Run Crawler Now" button in the admin console
- **THEN** the system SHALL immediately start an import job from NguonC in the background and stream live progress logs to the admin UI

### Requirement: In-depth Viewing Analytics Dashboard
The administration panel SHALL provide visual analytics charts tracking key performance indicators including total viewing hours, peak viewing times, top titles, and episode retention rates.

#### Scenario: Admin views streaming metrics
- **WHEN** an administrator visits the analytics dashboard
- **THEN** the system SHALL render interactive charts displaying view trends over time, completion rates per series, and device platform breakdowns

### Requirement: VIP Membership Tiers and VietQR Payment Verification
The system SHALL offer VIP subscription packages (e.g. 1 month, 3 months, 1 year) that unlock exclusive perks, and SHALL verify bank transfers automatically using VietQR dynamic order codes and webhooks.

#### Scenario: User initiates VIP checkout
- **WHEN** an authenticated user selects a VIP subscription tier
- **THEN** the system SHALL generate a dynamic VietQR code encoding the exact payment amount and a unique transaction order code (e.g. `ZM_VIP_1024_ABC12`)

#### Scenario: Automatic subscription activation via webhook
- **WHEN** the banking webhook receives an incoming transfer matching the order code and amount
- **THEN** the system SHALL immediately update the user's account to VIP status, extend `VipExpiresAt`, and notify the user interface in realtime
