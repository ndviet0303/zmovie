## Purpose

Allows users to manage and delete their personal watch history and submit structured issue reports for defective video streams or episodes.

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
