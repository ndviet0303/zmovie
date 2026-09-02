## ADDED Requirements

### Requirement: Realtime Danmaku Broadcasting and Synchronized Persistence
The system SHALL provide high-throughput, low-latency Danmaku comment broadcasting using SignalR and persist comments indexed by video playback seconds for on-demand playback synchronization.

#### Scenario: User submits Danmaku during active playback
- **WHEN** an authenticated user enters a Danmaku comment and submits
- **THEN** the system SHALL validate the text against content filters, persist the comment with video second offset, and immediately broadcast the message over SignalR to all active viewers watching the same title

#### Scenario: Fetching timed Danmaku comments during video buffering
- **WHEN** a viewer loads an episode or seeks to a new video timestamp
- **THEN** the client SHALL fetch time-bucketed Danmaku comment segments corresponding to the active playback window to ensure zero UI stutter
