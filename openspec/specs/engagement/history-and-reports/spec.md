## Purpose

Allows users to manage and delete their personal watch history, submit structured issue reports for defective video streams, explore TikTok-style vertical movie shorts, and earn EXP with Bilibili-style badges.

## Requirements

### Requirement: User Watch History Deletion
The system SHALL enable authenticated users to delete individual items from their viewing history or purge their entire watch history.

#### Scenario: Deleting a single item from watch history
- **WHEN** an authenticated user clicks the delete button next to an item in their watch history list
- **THEN** the system SHALL remove the corresponding watch progress record and update the history display immediately

#### Scenario: Clearing entire watch history
- **WHEN** an authenticated user confirms the "Clear All History" action
- **THEN** the system SHALL remove all watch progress entries associated with the user account

### Requirement: Episode Issue Reporting
The system SHALL provide a reporting mechanism on the playback interface allowing users to submit broken stream, missing audio, or subtitle desynchronization reports.

#### Scenario: Submitting a playback error report
- **WHEN** a viewer opens the report dialog on a video player, selects an issue type (e.g. "Broken Video Link"), enters optional details, and submits
- **THEN** the system SHALL record the report with title ID, episode number, issue category, and timestamp, and acknowledge submission with a success toast

#### Scenario: Admin viewing reported issues
- **WHEN** an administrator accesses the reports administration page
- **THEN** the system SHALL present pending issue reports ordered by occurrence count and allow changing their status to "Resolved" or "Dismissed"

### Requirement: Vertical Movie Shorts Feed (TikTok Style)
The system SHALL provide a dedicated vertical video feed (`/shorts`) presenting bite-sized movie highlights and viral clips with full-screen snapping, touch/keyboard navigation, and instant navigation to the full movie.

#### Scenario: User navigates movie shorts feed
- **WHEN** a user visits `/shorts` and scrolls vertically or uses Up/Down arrow keys
- **THEN** the feed SHALL smoothly snap to the next short clip, immediately begin muted or active autoplay, and buffer adjacent clips

#### Scenario: User deep-links from short to full movie
- **WHEN** a viewer clicks the "Xem bản đầy đủ (Watch Full Movie)" button on a short clip card
- **THEN** the application SHALL navigate directly to the target movie watch page and position playback at the start of the relevant episode

### Requirement: Gamification and Bilibili-Style User Badge System
The system SHALL award experience points (EXP) for constructive user activities, calculate account level progression (Level 1 to Level 6), and render unlocked dynamic badges across user comments and profile surfaces.

#### Scenario: User earns EXP from watching movies and community interactions
- **WHEN** an authenticated user watches video content for consecutive minutes, publishes an approved review, or submits a Danmaku comment
- **THEN** the system SHALL calculate EXP increments, update the user's progress bar toward the next level, and publish an `ExpAwardedDomainEvent`

#### Scenario: User unlocks achievement badge
- **WHEN** a user reaches qualifying milestones (e.g. 100 hours watched, 50 reviews written, VIP membership held)
- **THEN** the system SHALL unlock the corresponding badge (e.g. "Mọt Phim 4K", "Cú Đêm", "Hội Viên Vàng") and display animated badge iconography beside their username across Danmaku, reviews, and profile
