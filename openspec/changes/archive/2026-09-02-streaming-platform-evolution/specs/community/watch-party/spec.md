## Purpose

Enables synchronized realtime group video watching with shared playback controls, participant presence, and room chat via SignalR.

## ADDED Requirements

### Requirement: Watch Party Room Lifecycle
The system SHALL allow authenticated users to create a watch party room for any playable movie or episode, invite guests via shareable link, and manage participant access.

#### Scenario: Host creates a watch party room
- **WHEN** an authenticated user clicks "Start Watch Party" on a movie page
- **THEN** the system SHALL generate a unique room code, assign the creator as Host, and open the party theater interface

#### Scenario: Guest joins room via invite link
- **WHEN** a user visits a valid watch party URL
- **THEN** the system SHALL add the user to the room presence list and announce their arrival to existing participants

### Requirement: Realtime Playback Synchronization
The system SHALL synchronize play, pause, and seek events between the Host and all room members, with automatic drift correction for client playback divergence.

#### Scenario: Host pauses or resumes playback
- **WHEN** the host triggers play or pause on their player
- **THEN** all connected guests' players SHALL immediately pause or play at the host's synchronized timestamp

#### Scenario: Guest drift correction
- **WHEN** a guest player's current timestamp differs from the host by more than 2 seconds
- **THEN** the guest player SHALL automatically seek to the host's current playback position to maintain synchronization

### Requirement: Room Live Chat
The watch party interface SHALL provide an integrated realtime chat channel for room participants.

#### Scenario: Participant sends a chat message
- **WHEN** a connected participant types and sends a text message
- **THEN** the message SHALL appear instantly in the chat panel of all active members in the room
