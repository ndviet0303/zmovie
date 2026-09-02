## Purpose

Notifies users when new episodes or titles are released for content saved in their personal library or watchlist.

## Requirements

### Requirement: New Episode Release Notification
The system SHALL generate notifications for users who have saved a title to their library whenever new episodes become available for that title.

#### Scenario: User receives alert for new episode release
- **WHEN** a new episode is ingested or published for a series in the user's saved library
- **THEN** the system SHALL create an unread notification record linking to the specific episode playback URL

### Requirement: Notification Bell UI and Realtime Dispatch
The navigation header SHALL display a notification bell with an unread badge counter and provide a dropdown to view, mark as read, and navigate to alerted content.

#### Scenario: User views unread notifications
- **WHEN** an authenticated user clicks the notification bell icon in the navbar
- **THEN** the system SHALL display a list of recent alerts with thumbnail, title name, new episode label, relative timestamp, and mark-as-read controls

#### Scenario: Realtime notification push
- **WHEN** a new episode notification is generated while the user is actively browsing the application
- **THEN** the system SHALL increment the unread badge counter in realtime via SignalR without requiring a page reload
