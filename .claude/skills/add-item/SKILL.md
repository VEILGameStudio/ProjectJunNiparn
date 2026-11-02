---
name: add-item
description: Use when a teammate wants a new item (key, gear, food, quest item, secret collectible) or wants to change an existing item's name, icon, description, stack or effect.
---

# Add or change an item

Spec: CLAUDE.md §7 "Items and inventory", "Holding an item", "The inventory screen".

## Steps (Intern B, no code)
1. Create the `ItemData` asset in the right folder: `Data/Items/Puzzle/`, `Consumable/`, `Quest/` or `Secret/`.
2. Set `category` to match the folder.
3. Set `id` — lowercase, snake_case, final. Name and description are localized and can change any time.
4. Icon from `Art/Sprites/Shared/`.
5. **Register it in the one `ItemDatabase`.** Forgetting this is the most common mistake.
6. Consumables: `healAmount` / `staminaAmount` (0 = no effect).

## Must hold
- **Never change an existing `id`** once saves exist. Rename the display name instead.
- No capacity, no "inventory full" path, ever.
- Secret items are gallery entries: picking one up unlocks the entry and does not go in the bag; found
  secrets live in the global save.
- A third kind of effect that is not a number on a stat (light a torch, reveal the map) is a design
  change → `design-change-proposal` for an `ItemEffect` ScriptableObject. Not a third `if`.
- UI never edits inventory data; it calls `Hold` / `StopHolding` and redraws from events.

## Check
The two tests: every `.asset` under `Data/Items/` is in `ItemDatabase`; folder matches `category`.
Hand back with CLAUDE.md §10, in Thai.
