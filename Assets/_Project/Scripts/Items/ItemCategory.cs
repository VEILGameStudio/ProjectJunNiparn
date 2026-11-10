// ItemCategory
// The kind of an item. Every item asset also sits in the folder of its category:
//   Data/Items/Puzzle, Data/Items/Consumable, Data/Items/Quest, Data/Items/Secret
// The folder is for people browsing the project. This value is what the game reads,
// because a script cannot look at a folder while the game is running. The two must match.
//
// Put this on: nothing. ItemData uses it.

public enum ItemCategory
{
    Puzzle,       // Opens something: keys, gears, fuses.
    Consumable,   // Used up: food, tonics.
    Quest,        // Carried to the end of the story.
    Secret        // A collectible shown in the gallery. It does not go in the bag.
}
