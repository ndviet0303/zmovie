## ADDED Requirements

### Requirement: Player Experience Toolbar and State Overlays
The watch page SHALL render an integrated quick-control toolbar directly below the video player providing toggles for autoplay next (`Chuyển tập`), skip intro (`Bỏ qua giới thiệu`), theater mode (`Rạp phim`), social share, watch party (`Xem chung`), issue reporting (`Báo lỗi`), and chapter preview thumbnails, and MUST render a themed error overlay ("CÓ BIẾN RỒI") when stream loading encounters a fatal error.

#### Scenario: Toggling theater mode
- **WHEN** a viewer activates the "Rạp phim" toggle in the under-player toolbar
- **THEN** the player container SHALL expand to fill the full viewport width while dimming adjacent sidebar elements

#### Scenario: Rendering player error fallback state
- **WHEN** an episode video stream fails to load or encounters a network disruption
- **THEN** the player container SHALL display the themed fallback message "CÓ BIẾN RỒI - Hãy thử refresh lại!" with an action to reload playback

#### Scenario: Selecting episode from quick grid
- **WHEN** a viewer clicks an episode button (e.g. `▶ Tập 2`) in the watch page episode grid
- **THEN** the player SHALL switch immediately to the selected episode and persist user progress
