## MODIFIED Requirements

### Requirement: Multi-Source Streaming Engine and Dual Player Failover
The player SHALL provide high-reliability video playback supporting multiple stream sources per episode (including Cloudflare R2 HLS, OPhim HLS, NguonC HLS, native direct video such as MP4, and external embed iframes) and MUST automatically fail over to the next priority source if the current source encounters a network or playback error.

#### Scenario: Playing primary HLS stream
- **WHEN** a viewer opens an episode with multiple configured stream sources
- **THEN** the player SHALL attempt playback on the highest-priority source (Priority 1) using native HLS.js with adaptive quality switching

#### Scenario: Automatic fallback on broken stream
- **WHEN** the active HLS stream returns HTTP 403, 404, or fails to parse manifests after 2 retry attempts
- **THEN** the player SHALL silently fail over to the next priority backup stream source without reloading the entire web page

#### Scenario: Fallback to sanitized responsive iframe embed
- **WHEN** all direct HLS streams fail or the selected source is an embed provider URL
- **THEN** the player SHALL render a sanitized responsive iframe container while preserving player hotkeys, episode selectors, and reporting tools

Direct-source recovery SHALL preserve the current episode, latest requested playback position, and play/pause intent. Recovery MUST be bounded, and errors from obsolete sources MUST NOT advance the current source. Position control within external embeds is not guaranteed.

#### Scenario: Native MP4 failure
- **WHEN** the active MP4 source encounters a terminal media error and a backup exists
- **THEN** the player SHALL select the next priority source without a page reload

#### Scenario: Recovery between direct sources
- **WHEN** a direct source fails during playback or a pending seek and another direct source is available
- **THEN** the replacement SHALL resume at the latest intended position clamped to its valid duration and retain play/pause intent subject to browser playback permission

#### Scenario: Exhausted sources
- **WHEN** all eligible sources have failed within the bounded recovery policy
- **THEN** the player SHALL stop automatic retries and expose the existing error and explicit retry controls

