## Purpose

Provide predictable movie startup, saved-position restoration, and seeking, with frame-based evidence of the waiting time experienced by viewers.

## ADDED Requirements

### Requirement: Latest playback selection owns the session
The player SHALL activate only the latest selected episode and source, without redundant reloads of an unchanged selection. Switching selection or leaving the page MUST prevent obsolete work from taking over playback or showing stale errors.

#### Scenario: Opening an episode
- **WHEN** the initial source and video viewport become available
- **THEN** the player SHALL load that selection once without application-triggered duplicate source resets

#### Scenario: Rapid source changes
- **WHEN** a viewer selects another source while the previous selection is initializing
- **THEN** only the newest selection SHALL attach media, update playback state, or display errors

### Requirement: Initial resume is bounded and applied once
The player SHALL choose an initial episode and valid saved position before beginning content playback. Remote history lookup MUST have a finite deadline; failure or late completion MUST NOT block or interrupt playback. Explicit viewer selection MUST override automatic resume.

#### Scenario: Saved position is available
- **WHEN** a viewer opens a title with a valid saved position for the chosen episode
- **THEN** playback SHALL begin at that position without first playing from zero or reapplying resume after later seeks

#### Scenario: Remote history is slow or unavailable
- **WHEN** remote history does not resolve before the startup deadline
- **THEN** playback SHALL proceed using available local progress or the default episode and SHALL ignore late remote results for the active session

#### Scenario: Viewer selects an episode during startup
- **WHEN** the viewer selects an episode before automatic resume completes
- **THEN** that episode SHALL remain selected regardless of a later history response

### Requirement: Seeking preserves viewer intent
Seeking SHALL retain the current source and play/pause intent, move to the latest requested timestamp, and accurately represent loading for that seek. A stale event MUST NOT mark a newer pending seek complete.

#### Scenario: Seeking during playback
- **WHEN** a viewer seeks to an unbuffered timestamp while playing
- **THEN** the player SHALL load the target media and continue playing from the requested position without resetting the source

#### Scenario: Seeking while paused
- **WHEN** a viewer seeks while paused
- **THEN** the player SHALL display the target frame when available and remain paused

#### Scenario: Consecutive seeks
- **WHEN** a second seek is requested before the first finishes
- **THEN** the player SHALL settle at the latest target and SHALL NOT restore an older saved position

### Requirement: Frame-based latency verification
The change SHALL include reproducible before/after measurements of startup and seek latency. Measurements MUST distinguish actual displayed frames from loading-indicator events, exclude time waiting for user play permission, and identify cold versus warm cache conditions.

#### Scenario: Pilot performance evaluation
- **WHEN** the pilot is evaluated for promotion
- **THEN** the report SHALL include baseline and candidate startup from zero, startup from saved progress, and unbuffered seeks to minutes 30, 60, and 120 on the same recorded device, browser, and network profile
- **AND** the report SHALL include individual samples, medians, p95 values, failure counts, and cache conditions without presenting exploratory targets as production guarantees
