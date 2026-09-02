## Purpose

Enriches movie metadata with actors, directors, country of origin, and preview trailers from NguonC catalog data and Cloudflare R2 demo titles, while enabling multi-faceted filtering on the browse page.

## Requirements

### Requirement: Enriched Title Metadata Representation
The system SHALL store and expose comprehensive movie metadata including actor lists, director names, country tags, and trailer video links mapped from NguonC API and R2 demo titles.

#### Scenario: Displaying movie detail with NguonC metadata
- **WHEN** a user visits a movie detail page populated from NguonC or R2 demo data
- **THEN** the system SHALL display the list of starring actors, director names, country of production, and an interactive button to watch the trailer

#### Scenario: Playing trailer in a preview modal
- **WHEN** a user clicks the "Watch Trailer" button on a title card or detail page
- **THEN** the system SHALL open a modal dialog embedding the responsive trailer video player without navigating away from the page

### Requirement: Multi-Faceted Catalog Browse Filter
The catalog browse page SHALL allow users to simultaneously filter titles by multiple genres, country of production, release year range, and media format (single movie vs series), combined with sorting criteria.

#### Scenario: Applying combined genre and country filters
- **WHEN** a user selects multiple genres (e.g. Action and Thriller) and filters by Country (e.g. South Korea)
- **THEN** the browse list SHALL display only titles matching all selected criteria and update the browser URL query parameters

#### Scenario: Resetting active filters
- **WHEN** a user clicks the "Clear Filters" action
- **THEN** the browse page SHALL reset all facet selections to default and display the full catalog list
