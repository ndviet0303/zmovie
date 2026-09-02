## Purpose

Enables synchronized realtime group video watching with shared playback controls, participant presence, room chat, and realtime Danmaku flying comments via SignalR.

## Requirements

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

### Requirement: Realtime Danmaku Broadcasting and Synchronized Persistence
The system SHALL provide high-throughput, low-latency Danmaku comment broadcasting using SignalR and persist comments indexed by video playback seconds for on-demand playback synchronization.

#### Scenario: User submits Danmaku during active playback
- **WHEN** an authenticated user enters a Danmaku comment and submits
- **THEN** the system SHALL validate the text against content filters, persist the comment with video second offset, and immediately broadcast the message over SignalR to all active viewers watching the same title

#### Scenario: Fetching timed Danmaku comments during video buffering
- **WHEN** a viewer loads an episode or seeks to a new video timestamp
- **THEN** the client SHALL fetch time-bucketed Danmaku comment segments corresponding to the active playback window to ensure zero UI stutter
