## MODIFIED Requirements

### Requirement: Enriched Title Metadata Representation
The system SHALL store and expose comprehensive movie metadata including actor lists with real profile photos and character names, director biographies, country tags, official YouTube trailer embeds, and official 4K backdrops and posters enriched from The Movie Database (TMDB) API and Cloudflare R2 demo titles.

#### Scenario: Displaying movie detail with NguonC metadata
- **WHEN** a user visits a movie detail page populated from NguonC or R2 demo data
- **THEN** the system SHALL display the list of starring actors, director names, country of production, and an interactive button to watch the trailer

#### Scenario: Playing trailer in a preview modal
- **WHEN** a user clicks the "Watch Trailer" button on a title card or detail page
- **THEN** the system SHALL open a modal dialog embedding the responsive trailer video player without navigating away from the page

#### Scenario: Displaying movie detail with TMDB 4K enrichment
- **WHEN** a user visits a movie detail page enriched by TMDB
- **THEN** the system SHALL display high-resolution 4K backdrops, official YouTube trailer embeds, actor portrait avatars with character names, and verified release information

#### Scenario: TMDB fallback to catalog crawler metadata
- **WHEN** a title cannot be matched against TMDB API
- **THEN** the system SHALL seamlessly fall back to default metadata imported from NguonC or OPhim without broken image links or missing fields

## ADDED Requirements

### Requirement: Automated Rich SEO and Structured Data Generation
The system SHALL automatically generate search engine optimized metadata, OpenGraph tags, dynamic social sharing preview cards, and Schema.org JSON-LD structured data for every movie and series in the catalog.

#### Scenario: Search engine crawler reads title page
- **WHEN** a web crawler (e.g. Googlebot) requests a title detail or watch page
- **THEN** the server-rendered HTML response SHALL include Schema.org `Movie` or `VideoObject` structured JSON-LD data specifying title, duration, rating, upload date, and thumbnail URL

#### Scenario: Social sharing link preview generation
- **WHEN** a user shares a title link on social messaging platforms (Facebook, Zalo, Telegram)
- **THEN** the platform SHALL serve OpenGraph meta tags (`og:image`, `og:title`, `og:description`, `og:video`) presenting an optimized branded preview card
