## Purpose

Provides an enhanced video playback experience including high-reliability Cloudflare R2 HLS streaming for benchmark demo movies, dual-engine playback (HLS.js + Embed fallback), keyboard shortcuts (hotkeys), subtitle track selection, and realtime Danmaku comments overlay.

## Requirements

### Requirement: Cloudflare R2 Demo Streaming and Dual Player Engine
The player SHALL provide high-reliability HLS video playback for self-hosted demo movies stored in Cloudflare R2, and MUST support automatic switching between the native HLS.js player and an embedded responsive iframe player when the stream source is an external embed URL.

#### Scenario: Playing Cloudflare R2 demo title via native HLS
- **WHEN** a viewer opens one of the benchmark demo movies hosted on Cloudflare R2
- **THEN** the player SHALL initialize HLS.js with the R2 CDN m3u8 endpoint, buffering smoothly with sub-second time-to-first-frame and full quality level selection

#### Scenario: Playing external title with embed source
- **WHEN** an episode stream source is an external iframe embed URL (e.g. from NguonC streamc.xyz)
- **THEN** the player SHALL render a sanitized responsive iframe container while preserving surrounding movie details, episode selector, and report buttons

### Requirement: Player Hotkeys Control
The video player SHALL support standard keyboard shortcuts to control playback without requiring cursor interaction, and MUST suppress shortcut triggers when the user is focused on form inputs or modal dialogs.

#### Scenario: User toggles playback with spacebar
- **WHEN** the user presses the `Space` key while viewing the video player and focus is not inside an active text input or textarea
- **THEN** the player SHALL toggle between playing and paused states without scrolling the webpage

#### Scenario: User seeks backward or forward
- **WHEN** the user presses the `ArrowLeft` or `ArrowRight` key
- **THEN** the player SHALL seek backward or forward by exactly 10 seconds and display a visual indicator of the time change

#### Scenario: User toggles fullscreen or mute
- **WHEN** the user presses `F` or `M`
- **THEN** the player SHALL toggle fullscreen mode or audio mute state respectively

#### Scenario: User types inside a comment input
- **WHEN** the user presses the `Space`, `F`, or `M` key while an `<input>` or `<textarea>` element has active focus
- **THEN** the player SHALL NOT trigger video controls and SHALL preserve native text entry

### Requirement: Subtitle Track Selection
The player SHALL detect embedded subtitle tracks from the HLS stream or external WebVTT tracks and allow users to select their preferred language or disable captions.

#### Scenario: User selects a subtitle language
- **WHEN** the user opens the player captions menu and chooses an available subtitle language
- **THEN** the player SHALL render text cues synchronized with video currentTime

#### Scenario: User disables subtitles
- **WHEN** the user selects "Off" or presses the `C` shortcut key
- **THEN** the player SHALL immediately hide all caption overlays

### Requirement: Danmaku Comments Display and Submission
The video player SHALL render user comments as animated flying text horizontally traversing the video viewport synchronized to the video playback timestamp.

#### Scenario: Rendering timed danmaku comments
- **WHEN** video playback reaches a timestamp corresponding to existing Danmaku comments
- **THEN** the player SHALL animate the comment text smoothly across the video frame from right to left on a Canvas overlay without blocking primary video controls

#### Scenario: User submits a Danmaku comment
- **WHEN** an authenticated user inputs a message in the Danmaku bar and submits
- **THEN** the system SHALL record the comment at the current playback second and display it immediately on the screen

#### Scenario: User toggles Danmaku visibility
- **WHEN** the user clicks the Danmaku visibility toggle button
- **THEN** all flying Danmaku comments SHALL be hidden or shown according to the user's preference
