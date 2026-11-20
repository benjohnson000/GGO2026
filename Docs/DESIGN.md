# GGO2026 Design Notes

GitHub Game Off 2026. Theme: **Shift**.

A match-3 roguelike with a deckbuilding layer. The player swaps ("shifts") tiles on a grid to trigger matches and combos. Between encounters the player builds a deck of tiles that determines what appears on the grid.

This document records decisions made so far. Anything under **Open Questions** is undecided.

## Core Loop

1. Enter an encounter with a grid filled from the player's deck.
2. Swap adjacent tiles to make matches. Matched tiles trigger their effects.
3. Win the encounter, then choose a reward that changes the deck.
4. Move on to the next encounter.

## Tiles

A **tile** is a piece that appears on the grid and can be matched. The player's deck is a collection of tiles.

Each tile has:

| Property | Meaning |
|---|---|
| **Colour(s)** | What the tile matches with. A tile may have more than one colour. |
| **Effect** | What happens when the tile is matched (damage, money, explode, etc.). |
| **Shape** | Visual identity of the effect. |

### Colours and matching

- Colour is the only thing that determines whether a match forms. Effects never affect matching.
- A multi-colour tile matches as **any one of its colours** (wildcard-style).
- Keep the total colour count low (4 to 5) so matches stay easy to find.

### Effects

- **Every tile in a match triggers its effect**, so mixed matches reward deliberate placement.
- Effect order should be simple and deterministic. Exact rules are still to be defined.
- Scope for the jam: roughly 12 to 15 well-made effects rather than a large catalogue.

### Visual language

- Colour sets the palette: all tiles of one colour share a palette.
- Shape identifies the effect. Keep the shape set small (about 8 to 10) so every effect is readable at a glance.
- No complex designs. Shape plus colour also keeps the game readable for colour-blind players.

### Starting tiles

The starting deck contains many tiles of each colour. These **plain tiles** have no effect or a very weak one. Better tiles are earned during the run.

## Deck

- The deck is a **pool**. When the grid needs a tile (initial fill, refill after matches, cascades), one is picked at random from the pool and moved onto the board.
- A tile on the board is out of the pool. It **returns to the pool only when destroyed**, either by being matched or by an effect. There is no separate discard pile or reshuffle.
- A destroyed tile can be redrawn on the same refill, so a small pool can bring effect tiles straight back.
- If the pool is empty when the grid needs a tile, the cell is filled with a **filler tile** (see below). This means the deck has no minimum size.

### Filler tiles

- Fillers are not part of the deck. They exist only to fill the board when the pool is empty.
- Decided: fillers have a random colour and no effect, so the board is always matchable. Uncoloured duds were rejected because they cannot be matched and would clog the grid.
- Intended consequence: a deck smaller than the grid keeps every tile on the board at all times, which removes draw luck. This is considered a feature.
- If it proves too strong, counter-measures to consider later: a small filler downside, a minimum deck size, or relying on thin decks simply having fewer tiles to work with. Not needed yet.
- Open: whether fillers look distinct from plain deck tiles.

### Building the deck

- Adding tiles dilutes the chance of drawing every other tile, and removing weak tiles or swapping them for stronger ones is the main way to improve a deck.
- Once the deck is smaller than the grid, removing more tiles stops improving the odds of the rest, since all remaining tiles are already on the board.

## Grid

- Grid size is **configurable** and may **change between encounters**.
- Treat grid dimensions as encounter data, not a hard-coded constant.
- Initial playtest size to try: 6x6.

### Swapping and resolving

- **Input:** drag a tile toward a neighbour to swap them (mouse or touch). One swap per drag. No tap-tap selection.
- **Invalid swaps:** a swap that makes no match animates back and does not count as a turn.
- **Matches:** 3 or more tiles in a row or column that share at least one colour. Runs that share a tile (L and T shapes, or runs of different colours crossing on a multi-colour tile) count as one match group.
- **Resolving:** matched tiles are destroyed and return to the pool, tiles fall down, and empty cells are refilled from the pool. This repeats until no matches remain. Each repeat is a cascade.
- **Initial fill:** avoids creating matches where the pool allows it.
- **No valid moves:** the tiles already on the board are reshuffled. No tiles are drawn or returned.

## Encounters

- Each encounter is a fight against **one enemy at a time**.
- Both the player and the enemy have a **health pool**. Tile effects chip away at the enemy's health. Losing all player health ends the run.
- Turn-based: time is measured in player swaps, so cascades never cost time.

### Enemy intents

- The enemy telegraphs its next action and a countdown in swaps (e.g. "Attack 8 in 3 swaps").
- The player chooses whether to race the damage, defend, or deal with the threat. Player health makes defensive effects (shield, heal) meaningful.

### Enemy tiles

- Enemies can **add their own tiles to the player's pool** to clog it.
- Enemy tiles are **black** and may carry their own effects, e.g. harmful when matched or active while on the board.
- Enemy tiles follow the pool rules: they appear on the board when drawn and return to the pool when destroyed.

### Colour rules

- Each enemy can have colour **weaknesses and resistances** (e.g. double damage from red, half from blue).
- A weakness may rotate during the fight to force the player to change which colour they match.

### Variety

- Grid size and shape can change per encounter, including blocked cells for unusual shapes.
- **Affixes** on standard enemies (e.g. Armoured, Regenerating, Hexer) create many fights from a small set of enemies.
- **Bosses** have phases that change the rules partway through the fight.

### Out of scope (for now)

- Enemies that shift the board (rotating rows, reversing gravity, shuffling). Considered too complex for gameplay and the jam's scope.

## Charms

**Charms** are passive items the player collects during a run, similar to Slay the Spire relics. Tiles decide what can happen on the board, and charms change the rules the player plays under.

### Design goals

- **Lower power, still satisfying.** Charms should drive decisions without breaking the game, closer to Slay the Spire relics than Balatro jokers.
- **Mostly conditional.** Prefer charms tied to a play pattern ("your first 5-match each fight deals double") over flat bonuses ("+1 damage to all matches").
- **Additive, not multiplicative.** Avoid charms that multiply each other so stacking stays balanced.
- **Few per run.** Hand them out slowly so each one is noticed (roughly 10 to 15 by the end of a run).
- **Trade-offs for strong charms.** Boss charms may be powerful with a cost attached.
- **Give the deck direction.** A charm should push the player toward collecting certain tiles or colours.

### Example charms

- Matching your least common colour deals +2 damage.
- Multi-colour tiles trigger their effect twice.
- Filler tiles deal 1 damage when matched (rewards thin decks).
- +1 damage per 10 tiles in your pool (rewards large decks).
- Destroying a black tile grants 1 shield.
- When an enemy attack countdown reaches 1, gain 3 shield.
- Every 3rd cascade in a turn heals 1.

### Implementation notes

- Each charm reacts to game events such as match, cascade, tile destroyed, encounter start, and damage taken. The board should raise these as clean events so charms are mostly data plus a small script.
- Show active charms in a bar and flash a charm's icon when it triggers, so the player can see why something happened.
- Jam scope: roughly 15 to 20 charms.

## Naming

- **Tile** is the agreed name for deck items.
- **Charm** is the agreed name for passive run items.

## Open Questions

- Enemy tiles: does black match with other black tiles? Are enemy tiles removed from the pool when the encounter ends?
- What boss phases change, given that board-shifting enemy actions are out of scope.
- Enemy roster and affix list for the jam.
- How charms are earned (encounter rewards, shop, elites, bosses) and whether there is a charm limit.
- What effects does the reward screen offer, and how does the player remove tiles?
- Effect trigger order and how cascades interact with effects.
- Shift mechanics beyond the basic swap (the theme): special tiles, abilities, or other ways to rearrange the board.
- Run structure: map or linear progression, number of encounters, boss.
- Platform (WebGL?). Input is settled as drag-to-swap; see Swapping and resolving.
- Art direction. Decided: placeholder coloured shapes are used for now.
