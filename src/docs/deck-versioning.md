# Deck Versioning

Decks use mutable authoring tables and immutable study snapshots.

## Purpose

When a learner starts a study session from a deck, the cards they see must not change if the deck owner edits, reorders, or deletes cards before the learner submits results. Study plans therefore read from a specific `DeckVersion`, and submitted responses are validated against that same version.

## Data Model

- `Deck` remains the editable authoring aggregate.
- `Deck.CurrentVersionNumber` points to the version number currently used for new study plans.
- `DeckVersion` stores immutable deck metadata for a version.
- `DeckVersionCard` stores immutable card content for a version.
- `DeckVersionCardChoice` stores immutable multi-choice options for a version.
- `StudySession.DeckVersionId` records which version was studied.
- `StudySessionResponse.DeckVersionCardId` records which version card was answered.
- `StudySessionResponse.CardId` is nullable and points to the current mutable card only when that original card still exists.

The version-card tables intentionally store `OriginalCardId` and `OriginalChoiceId` as nullable values without depending on those mutable rows staying alive. This keeps historical study sessions readable after an owner deletes a card.

## Write Flow

Creating a deck creates version `1`.

Updating a deck:

1. Mutates the editable `Deck` and `Card` rows.
2. Increments `Deck.CurrentVersionNumber`.
3. Appends a new `DeckVersion` snapshot with copied cards and choices.

AI-generated decks also create an initial version. If audio is attached after generation, the processor appends another version so new study plans can see the attached audio asset ids.

## Study Flow

`GET /api/decks/{id}/study-plan` returns cards from the deck's current version. Each `StudyPlanCardDto` includes:

- `deckVersionId`
- `deckVersionNumber`
- `cardId`

For study-plan responses, `cardId` is the `DeckVersionCard` id. `POST /api/decks/{id}/study-sessions` accepts `deckVersionId` and validates each submitted card against that version.

For backward compatibility, submit validation also accepts original mutable card ids when they map to cards in the selected deck version. New clients should submit the version card ids returned by the study plan.

## Review State

Spaced-repetition state still uses the mutable original card id when that card still exists. This preserves user progress across small edits. If a user submits against an old version after the owner deleted the original card, the response is saved, but no review state is updated for that deleted card.

## Tradeoffs

This design duplicates deck text per version. The storage overhead should be modest because deck content is mostly small text rows, while audio assets remain shared by id.

The main complexity is semantic: edits that substantially change a card still preserve review state because the original card id remains the same. If that becomes undesirable, a future change can compare content hashes and fork review state only for materially changed cards.

## Future Improvements

- Add content hashes to version cards to distinguish typo fixes from meaning changes.
- Expose version history to deck owners.
- Add a rollback or "restore version" command.
- Consider pruning old versions only after confirming they have no study sessions.
