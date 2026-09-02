## Purpose

Provides an enhanced video playback experience including high-reliability Cloudflare R2 HLS streaming for benchmark demo movies, multi-source fallback, dual-engine playback (HLS.js + Embed fallback), keyboard shortcuts (hotkeys), multi-audio and dual-subtitle language learning, Ambilight glow, storyboard scrubbing, mini-player, and realtime Danmaku comments.

## Requirements

### Requirement: Multi-Source Streaming Engine and Dual Player Failover
The player SHALL provide high-reliability video playback supporting multiple stream sources per episode (including Cloudflare R2 HLS, OPhim HLS, NguonC HLS, and external embed iframes) and MUST automatically fail over to the next priority source if the current source encounters a network or playback error.

#### Scenario: Playing primary HLS stream
- **WHEN** a viewer opens an episode with multiple configured stream sources
- **THEN** the player SHALL attempt playback on the highest-priority source (Priority 1) using native HLS.js with adaptive quality switching

#### Scenario: Automatic fallback on broken stream
- **WHEN** the active HLS stream returns HTTP 403, 404, or fails to parse manifests after 2 retry attempts
- **THEN** the player SHALL silently fail over to the next priority backup stream source without reloading the entire web page

#### Scenario: Fallback to sanitized responsive iframe embed
- **WHEN** all direct HLS streams fail or the selected source is an embed provider URL
- **THEN** the player SHALL render a sanitized responsive iframe container while preserving player hotkeys, episode selectors, and reporting tools

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

### Requirement: Multi-Audio and Subtitle Track Selection
The player SHALL detect and expose all available audio tracks (such as Vietsub, Thuyết minh, Lồng tiếng, and Original Audio) and subtitle tracks from HLS manifests or external WebVTT tracks, allowing viewers to switch audio and subtitle language dynamically during playback.

#### Scenario: User switches audio track
- **WHEN** the user selects a different audio track (e.g. switching from "Vietsub" to "Lồng tiếng")
- **THEN** the player SHALL switch audio output seamlessly at the current playback second without resetting buffer position

#### Scenario: User selects a subtitle language
- **WHEN** the user opens the player captions menu and chooses an available subtitle language
- **THEN** the player SHALL render text cues synchronized with video currentTime

#### Scenario: User disables subtitles
- **WHEN** the user selects "Off" or presses the `C` shortcut key
- **THEN** the player SHALL immediately hide all caption overlays

### Requirement: Precise Intro and Outro Timestamp Skipping
The player SHALL inspect episode intro and outro timestamp metadata and provide automatic or manual single-click skipping of opening and closing sequences.

#### Scenario: Displaying skip intro button during opening sequence
- **WHEN** video playback enters the range `[IntroStart, IntroEnd]`
- **THEN** the player SHALL display an interactive "Bỏ qua giới thiệu (Skip Intro)" overlay button

#### Scenario: User triggers skip intro
- **WHEN** the user clicks the "Skip Intro" button or presses the `S` shortcut key while inside the intro window
- **THEN** the player SHALL seek immediately to `IntroEnd` and dismiss the prompt button

#### Scenario: Auto skip intro enabled in settings
- **WHEN** the user has enabled "Tự động bỏ qua Intro" in player settings and playback enters the intro range
- **THEN** the player SHALL automatically seek to `IntroEnd` and display a transient notification banner

### Requirement: Ambilight Atmospheric Glow Effect
The player SHALL capture live video frame edge colors in real time and project a synchronized, blurred atmospheric ambient glow behind the video container on dark theater backgrounds.

#### Scenario: Rendering synchronized ambilight glow
- **WHEN** the video is playing in theater or default mode and ambilight is enabled
- **THEN** the player SHALL sample edge pixels via an offscreen downscaled canvas and update CSS background glow filters at up to 30fps

#### Scenario: Low power and mobile fallback
- **WHEN** the user enables battery saver or accesses the site from a mobile device
- **THEN** the player SHALL automatically disable real-time canvas sampling to conserve battery and GPU resources

### Requirement: Storyboard Thumbnail Seekbar Scrubbing
The player SHALL display visual frame preview thumbnails in a floating tooltip when the cursor hovers or scrubs along the video progress seekbar.

#### Scenario: User hovers over progress seekbar
- **WHEN** the user hovers the cursor over a specific point along the playback progress bar
- **THEN** the player SHALL calculate the target timestamp and render the matching storyboard preview thumbnail image with formatted time text

### Requirement: Responsive Mini-Player and Native Picture-in-Picture
The player SHALL support native Picture-in-Picture (PiP) and an in-page floating Mini-Player docked to the bottom corner when the user scrolls down the page past the primary player viewport.

#### Scenario: User scrolls past video viewport
- **WHEN** the video is actively playing and the user scrolls down past the video container boundary
- **THEN** the player SHALL smoothly dock into a persistent mini-player overlay in the lower-right corner with basic play, pause, and close controls

#### Scenario: User toggles native Picture-in-Picture
- **WHEN** the user clicks the PiP toggle button or presses the `P` key
- **THEN** the browser SHALL detach the video into the OS native floating Picture-in-Picture window

### Requirement: Dual-Subtitle Language Learning Mode
The player SHALL provide a dual-subtitle mode displaying two concurrent subtitle tracks with clickable words for instant translation lookup and sentence repeating hotkeys.

#### Scenario: Displaying synchronized dual subtitles
- **WHEN** the user enables "Học ngoại ngữ (Dual Sub)" mode and selects primary (e.g. English) and secondary (e.g. Vietnamese) tracks
- **THEN** the player SHALL render both subtitle lines stacked above the control bar with distinct typographic styling

#### Scenario: User clicks a word for dictionary definition
- **WHEN** the user hovers over or clicks an individual word in the foreign subtitle cue
- **THEN** the player SHALL pause playback and display a popover containing word pronunciation, part of speech, and Vietnamese definition

#### Scenario: User repeats current subtitle sentence
- **WHEN** the user presses the `R` key while in dual-subtitle learning mode
- **THEN** the player SHALL seek backward to the start timestamp of the currently active subtitle cue and replay it

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
- **THEN** the player SHALL toggle the Danmaku canvas display without affecting primary video playback
