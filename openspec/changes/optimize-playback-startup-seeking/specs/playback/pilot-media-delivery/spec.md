## Purpose

Deliver a reversible adaptive streaming pilot for Natra that reduces startup and seek waiting through independently decodable media segments and verified edge caching.

## ADDED Requirements

### Requirement: Adaptive segmented pilot source
The Natra pilot SHALL offer an adaptive HLS source with at least two compatible bitrate renditions, aligned independently decodable segment boundaries, and matching content timelines. Existing audio and subtitle availability MUST be preserved before promotion.

#### Scenario: Opening the pilot stream
- **WHEN** a supported browser opens the Natra pilot source
- **THEN** it SHALL be able to start playback and select among the available renditions without downloading the entire movie

#### Scenario: Seeking within the pilot
- **WHEN** a viewer seeks to an unbuffered point
- **THEN** the stream SHALL permit decoding from a nearby segment boundary and presenting the requested point without downloading all preceding segments

### Requirement: Cacheable delivery through a production domain
The pilot SHALL deliver manifests and media through a production custom domain with browser-compatible CORS and media types. Versioned media objects MUST fit the configured cache size limits and expose verifiable cache behavior; r2.dev MUST NOT be the primary pilot delivery endpoint.

#### Scenario: Warm media request
- **WHEN** an eligible segment is requested repeatedly from the same delivery location after cache population
- **THEN** delivery evidence SHALL demonstrate an edge cache hit and correct playable content

#### Scenario: Cross-origin playback
- **WHEN** a supported browser requests the pilot from the watch-page origin
- **THEN** manifests, media, and retained subtitle resources SHALL be accessible without CORS failures

### Requirement: Reversible Natra-only rollout
The pilot SHALL preserve the original MP4 source as a lower-priority fallback, change only the Natra source configuration, and support restoring its previous priority without re-encoding or deleting media.

#### Scenario: Pilot promotion
- **WHEN** compatibility, cache, and measured performance checks pass
- **THEN** the pilot SHALL become the preferred Natra source while other titles keep their existing source ordering

#### Scenario: Pilot rollback
- **WHEN** the pilot fails validation or causes a playback regression
- **THEN** the previous MP4 priority SHALL be retained or restored and the original media SHALL remain available
