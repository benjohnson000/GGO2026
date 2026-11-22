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
| **Colour** | What the tile matches with. A tile has one colour, or is colourless and never matches. Decided: colourless tiles are allowed in decks (but not as fillers). |
| **Effects** | What happens when the tile is matched (damage, money, explode, etc.). Decided: a tile can have any number of effects, which fire in the order listed. |
| **Shape** | Visual identity of the effect. |
| **Pattern** | Optional fill pattern drawn inside the shape, under the icon. |
| **Icon** | Optional symbol drawn in front of the shape. Its role versus shape is still open. |

### Colours and matching

- Colour is the only thing that determines whether a match forms. Effects never affect matching.
- Tiles match only with tiles of the same colour.
- Decided: **no multi-colour tiles.** They were rejected as too complex and overpowered.
- Possible later: **wild (rainbow) tiles** that match with any colour, kept extremely rare. Not designed or built yet.
- Keep the total colour count low (4 to 5) so matches stay easy to find.

### Effects

- **Every tile in a match triggers its effect**, so mixed matches reward deliberate placement.
- Effect order should be simple and deterministic. Exact rules are still to be defined.
- Scope for the jam: roughly 12 to 15 well-made effects rather than a large catalogue.

### Match and passive effects

A tile can have two kinds of effect:

| Kind | When it fires |
|---|---|
| **Match effect** | When the tile is part of a match. |
| **Passive effect** | While the tile is on the board: once per swap by default, or every N swaps for tiles with an interval. |

- A tile may have a match effect, a passive effect, or both.
- **Turn order:** the player swaps, matches and cascades resolve fully, then passives fire once, then the enemy countdown ticks. Cascades never trigger extra passive ticks.
- A passive tile only does something when matched if it also has a match effect. Otherwise matching it just removes it.
- Passives give the player a reason to keep some tiles on the board longer, so each swap is also a choice about what to leave alone.
- Tiles with an interval show a countdown pip, the same way enemy intents show theirs.
- **Charge-up tiles** combine both: the passive builds up while the tile is on the board, and the match effect cashes it out. Matching the tile is the intended payoff, not a loss.
- Keep passive values small (about 1 per tick). A deck smaller than the grid keeps every passive tile on the board all the time, which makes passives much stronger.
- Passives need a clear tell (counter, pulse when firing, highlight on affected tiles) so the player can see why something happened.
- Enemy tiles that are "active while on board" use the same passive system.

### Example effects

Match:
- **Strike:** deal 3 damage.
- **Guard:** gain 3 block.
- **Mend:** heal 2.
- **Coin:** gain gold.
- **Bomb:** destroy the 3x3 area around it (those tiles trigger their effects).
- **Line:** destroy its whole row or column.
- **Paint:** recolour adjacent tiles to its colour.
- **Echo:** repeat the effect of the strongest other tile in the match.

Passive:
- **Ember:** deal 1 damage to the enemy each swap.
- **Ward:** gain 1 block each swap.
- **Banner:** adjacent tiles deal +1 when matched.
- **Lodestone:** tiles of its colour are more likely to be drawn on refills.
- **Timekeeper:** every 4 swaps, the enemy's countdown ticks 1 slower.
- **Magpie:** gain 1 gold every 3 swaps.

Charge-up:
- **Battery:** gains +1 charge each swap. When matched, deals damage equal to its charge.
- **Seed:** grows each swap (Seed, Sprout, Bloom). Heals more when matched at a later stage.
- **Fuse:** counts down from 5 and explodes by itself at 0. Matching it early deals less damage but lets the player choose the timing.

### Visual language

- Colour sets the palette: all tiles of one colour share a palette.
- Decided: each palette has three colours, plus one outline colour and one highlight colour shared by all tiles. Each tile picks which palette colour (colour 1–3, the highlight, or a custom colour) its shape, pattern and icon use. By default the shape uses colour 2, the pattern colour 1 and the icon colour 3.
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
- Fillers are tile definitions listed on the board config (ideally one per colour). When one is needed, it is picked at random from that list.
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
- **Invalid swaps:** a swap that makes no match animates back and does not count as a turn. Exception: swapping a colour bomb is always valid.
- **Matches:** 3 or more tiles of the same colour in a row or column, or a 2x2 square of one colour. Runs and squares that share a tile (L, T and block shapes) count as one match group.
- **Resolving:** matched tiles are destroyed and return to the pool, tiles fall down, and empty cells are refilled from the pool. This repeats until no matches remain. Each repeat is a cascade.

### Combo tiles

- Special match shapes create a **combo tile** (like striped candy) in place of one matched tile.
- **One combo per match group**, from its highest-priority shape. Priority, highest first: 5+ in a line, T/L (a row and a column of 3+ sharing a tile), 4 in a line, 2x2 square. Plain 3-matches make no combo.
- The combo takes the match's colour and appears on the tile the player moved if it is part of the match; otherwise (cascades) at a fixed cell of the shape.
- **Temporary:** combo tiles never join the deck. When destroyed they are gone for good. The matched tiles still return to the pool as usual.
- **4 in a line** makes a **Striped** tile (Line Clear effect), which destroys its whole row or column when destroyed. The line is chosen when it fires:
  - Matched by the player's swap: as in Candy Crush, a sideways swap clears the column and an up/down swap clears the row.
  - Hit by another line clear: the other direction (a row clear hitting it makes it clear its column), so chains reach new tiles.
  - Otherwise (cascades): random.
  - If the chosen line has no tiles left (e.g. two Striped tiles matched in the same line), it clears the other direction instead of doing nothing.
  - Decided: the line is destroyed from the Striped tile outward, so effects in it fire nearest first. At equal distance, left fires before right and below before above.
- **Colour bomb** (Colour Bomb effect) is **colourless**: it never matches. Swapping it with any neighbour is always a valid move (it counts as a turn) and destroys every tile of that neighbour's colour.
  - Destroyed without being swapped (e.g. hit by a line clear): destroys every tile of the most common colour on the board.
  - Swapped with another colour bomb: destroys the whole board.
  - Which match shape makes it is set in the board config's combo rules (5 in a line is the natural fit).
- What a combo does is the combo tile's own effect, not a board setting.
- Not decided yet: what the square, T/L and 5-line combos do, and whether swapping two combos together does something special.
- **Initial fill:** avoids creating matches where the pool allows it.
- **No valid moves:** the tiles already on the board are reshuffled. No tiles are drawn or returned.

## Encounters

- Each encounter is a fight against **one enemy at a time**.
- Both the player and the enemy have a **health pool**. Tile effects chip away at the enemy's health. Losing all player health ends the run.
- Turn-based: time is measured in player swaps, so cascades never cost time.

### Health, block and statuses

- **Block** is the agreed name (not "shield"). Block absorbs damage before health.
- **Turns:** the enemy only gets a turn when its intent fires (its countdown reaches 0). Swaps that only tick the countdown are not enemy turns. The player's turn starts right after the enemy's turn, so each side gets one turn per enemy action.
- Block is lost at the **start of its owner's turn**, so the player's block lasts until the enemy has acted. Charms may change this (e.g. "only lose 50% of your block at the start of your turn").
- Status effects (poison, stun, etc.) are a major part of the game and can be on either side.
- **Poison:** ticks at the start of its owner's turn (so enemy poison ticks at the start of the enemy turn), dealing damage equal to its stacks, then loses a configurable number of stacks (1 by default).
- **Player health carries over** between encounters. This should be configurable in settings and changeable by a charm.
- Charms and statuses hook into combat events such as damage taken (e.g. "whenever you take damage, apply 5 poison").

### Enemy intents

- The enemy telegraphs its next action and a countdown in swaps (e.g. "Attack 8 in 3 swaps").
- **Each intent has its own countdown.** Every accepted swap ticks it down by 1; at 0 the intent fires and the enemy picks its next one.
- For now enemies pick their next intent **at random**, by weight. Fixed or scripted patterns may come later.
- **Stun adds swaps** to the enemy's countdown, delaying its intent.
- The player chooses whether to race the damage, defend, or deal with the threat. Player health makes defensive effects (block, heal) meaningful.

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
- Filler tiles deal 1 damage when matched (rewards thin decks).
- +1 damage per 10 tiles in your pool (rewards large decks).
- Destroying a black tile grants 1 block.
- When an enemy attack countdown reaches 1, gain 3 block.
- Every 3rd cascade in a turn heals 1.

### Implementation notes

- Each charm reacts to game events such as match, cascade, tile destroyed, encounter start, and damage taken. The board should raise these as clean events so charms are mostly data plus a small script.
- Show active charms in a bar and flash a charm's icon when it triggers, so the player can see why something happened.
- Jam scope: roughly 15 to 20 charms.

## Naming

- **Tile** is the agreed name for deck items.
- **Charm** is the agreed name for passive run items.

## Open Questions

- Does poison damage ignore block? (The poison asset has a toggle, on by default.)
- Enemy tiles: does black match with other black tiles? Are enemy tiles removed from the pool when the encounter ends?
- What boss phases change, given that board-shifting enemy actions are out of scope.
- Enemy roster and affix list for the jam.
- How charms are earned (encounter rewards, shop, elites, bosses) and whether there is a charm limit.
- What effects does the reward screen offer, and how does the player remove tiles?
- Effect trigger order within a match and how cascades interact with match effects. (The order along a line clear is decided; see Combo tiles.) (Passive timing is decided; see Match and passive effects.)
- Player-facing name for match effects: "Activated" or "Match". "Match effect" is used in this doc for now.
- Shift mechanics beyond the basic swap (the theme): special tiles, abilities, or other ways to rearrange the board.
- Combo tiles: effects for the square, T/L and 5-line combos; combo + combo swaps; whether tiles destroyed by a combo trigger their match effects.
- Run structure: map or linear progression, number of encounters, boss.
- Platform (WebGL?). Input is settled as drag-to-swap; see Swapping and resolving.
- Art direction. Decided: placeholder coloured shapes are used for now.
- Tile visuals: does the icon or the shape identify the effect, and what do the others (icon, pattern) communicate?
