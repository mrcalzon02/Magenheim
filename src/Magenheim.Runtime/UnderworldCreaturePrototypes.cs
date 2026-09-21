using UnityEngine;
namespace Magenheim.Runtime;

// Content-only rough roster. These are intact donors, not accepted replacement meshes.
internal static class UnderworldCreaturePrototypes
{
    internal sealed record Entry(string Name, string Biome, string Donor, float Scale, Color Color, string Limit)
    {
        internal string Prefab => "Magenheim_Underworld_Prototype_" + Name.Replace(" ", "");
    }
    internal static readonly Entry[] All =
    {
        new("Lantern Moth", "Fungal Forest", "Bat", 0.65f, new Color(0.596f, 0.824f, 0.698f), "Winged hover/bite stand-in; ambient temperament and four-wing art pending"),
        new("Sporeling", "Fungal Forest", "Tick", 0.6f, new Color(0.596f, 0.824f, 0.698f), "Native scuttle/latch; fungal cap art pending"),
        new("Capcrawler", "Fungal Forest", "Seeker", 0.65f, new Color(0.596f, 0.824f, 0.698f), "Native insect walk/bite/flight; ground-only behavior not claimed"),
        new("Mycelial Stalker", "Fungal Forest", "Wolf", 1.05f, new Color(0.596f, 0.824f, 0.698f), "Native quadruped run/bite; no custom concealment or pounce"),
        new("Puffback", "Fungal Forest", "Lox", 0.8f, new Color(0.596f, 0.824f, 0.698f), "Native graze/walk/stomp; no inflation or spore burst"),
        new("Shelf Lurker", "Fungal Forest", "Seeker", 1f, new Color(0.596f, 0.824f, 0.698f), "Native insect movement; no wall climbing"),
        new("Crowncap Brute", "Fungal Forest", "Troll", 0.9f, new Color(0.596f, 0.824f, 0.698f), "Native heavy biped swings; crown/gill art pending"),
        new("Cave Ray", "Blackwater Deep", "Serpent", 0.45f, new Color(0.459f, 0.710f, 0.769f), "Swim-chain motion proxy only; no fin-wing deformation or ambient temperament"),
        new("Gloomfin", "Blackwater Deep", "Serpent", 0.55f, new Color(0.459f, 0.710f, 0.769f), "Native swimming/bite; fish body art pending"),
        new("Blackwater Lamprey", "Blackwater Deep", "Leech", 1f, new Color(0.459f, 0.710f, 0.769f), "Native swimming/bite; no attached-player latch"),
        new("Shoreclaw", "Blackwater Deep", "Neck", 1.5f, new Color(0.459f, 0.710f, 0.769f), "Native amphibious walk/swim/bite; shell and claws pending"),
        new("Lantern Angler", "Blackwater Deep", "Serpent", 0.7f, new Color(0.459f, 0.710f, 0.769f), "Native swim/bite; no lure or ranged pressure attack"),
        new("Abyss Shellback", "Blackwater Deep", "Neck", 2f, new Color(0.459f, 0.710f, 0.769f), "Amphibious motion proxy; shell armor and guard pending"),
        new("Deep Hunter", "Blackwater Deep", "Serpent", 1.2f, new Color(0.459f, 0.710f, 0.769f), "Native large aquatic predator; hero anatomy pending"),
        new("Ashmite", "Sulfurous Wastes", "Tick", 0.45f, new Color(0.859f, 0.608f, 0.369f), "Native scuttle/latch; heat-shell art pending"),
        new("Cinder Hound", "Sulfurous Wastes", "Wolf", 1f, new Color(0.859f, 0.608f, 0.369f), "Native run/bite; heat-adapted art pending"),
        new("Basalt Crawler", "Sulfurous Wastes", "SeekerBrute", 0.65f, new Color(0.859f, 0.608f, 0.369f), "Native armored insect locomotion/strikes"),
        new("Vent Spitter", "Sulfurous Wastes", "Neck", 1.25f, new Color(0.859f, 0.608f, 0.369f), "Quadruped motion proxy; no pressure sac or ranged attack"),
        new("Fume Wraith", "Sulfurous Wastes", "Wraith", 1f, new Color(0.859f, 0.608f, 0.369f), "Native spectral flight/melee; no new smoke rig"),
        new("Magma Leaper", "Sulfurous Wastes", "Fenring", 0.75f, new Color(0.859f, 0.608f, 0.369f), "Native leap/attack; biped donor silhouette retained for review"),
        new("Furnace Golem", "Sulfurous Wastes", "StoneGolem", 1.1f, new Color(0.859f, 0.608f, 0.369f), "Native heavy construct attacks; no armor-break mechanic"),
        new("Rime Moth", "Frozen Caverns", "Bat", 0.7f, new Color(0.655f, 0.835f, 0.925f), "Winged motion proxy; crystalline wing anatomy pending"),
        new("Frost Tick", "Frozen Caverns", "Tick", 0.7f, new Color(0.655f, 0.835f, 0.925f), "Native tick locomotion/latch"),
        new("Iceblind", "Frozen Caverns", "Wolf", 1.1f, new Color(0.655f, 0.835f, 0.925f), "Native quadruped locomotion; sensory head art pending"),
        new("Pale Burrower", "Frozen Caverns", "Seeker", 0.85f, new Color(0.655f, 0.835f, 0.925f), "Native insect locomotion; burrowing deferred"),
        new("Rimewing", "Frozen Caverns", "Hatchling", 1f, new Color(0.655f, 0.835f, 0.925f), "Native flight/projectile tells; no perch lifecycle"),
        new("Glacier Stalker", "Frozen Caverns", "Wolf", 1.2f, new Color(0.655f, 0.835f, 0.925f), "Native run/bite; low ice-armored anatomy pending"),
        new("Cryolith Guardian", "Frozen Caverns", "StoneGolem", 1f, new Color(0.655f, 0.835f, 0.925f), "Native heavy construct locomotion/attack"),
        new("Fracture Wisp", "Fracture Zones", "FrostWisp", 0.8f, new Color(0.741f, 0.624f, 0.863f), "Native flying wisp; no orbiting-piece system"),
        new("Rift Skitter", "Fracture Zones", "Seeker", 0.65f, new Color(0.741f, 0.624f, 0.863f), "Native insect locomotion; no wall adhesion"),
        new("Shardwing", "Fracture Zones", "Hatchling", 0.9f, new Color(0.741f, 0.624f, 0.863f), "Native flight/projectile; mineral wing art pending"),
        new("Gravity Leech", "Fracture Zones", "Leech", 1.1f, new Color(0.741f, 0.624f, 0.863f), "Aquatic motion proxy only; no land crawl or pull field"),
        new("Chasm Stalker", "Fracture Zones", "Seeker", 1.15f, new Color(0.741f, 0.624f, 0.863f), "Native insect locomotion; long-limb art pending"),
        new("Stonebound", "Fracture Zones", "StoneGolem", 0.7f, new Color(0.741f, 0.624f, 0.863f), "Native grounded construct; independent floating masses deferred"),
        new("Rift Colossus", "Fracture Zones", "StoneGolem", 1.3f, new Color(0.741f, 0.624f, 0.863f), "Native heavy construct; no custom displacement system"),
        new("Rotling", "Great Decay", "Tick", 0.8f, new Color(0.690f, 0.702f, 0.416f), "Native scuttle/latch; biomass anatomy pending"),
        new("Carrion Bloom", "Great Decay", "Greydwarf_Shaman", 0.85f, new Color(0.690f, 0.702f, 0.416f), "Native poison-cast proxy; mobile donor retained, rooted art pending"),
        new("Spore Husk", "Great Decay", "Draugr", 1f, new Color(0.690f, 0.702f, 0.416f), "Native humanoid equipment/attacks; grafted silhouette pending"),
        new("Marrow Creeper", "Great Decay", "Seeker", 0.7f, new Color(0.690f, 0.702f, 0.416f), "Native insect locomotion; bone-supported anatomy pending"),
        new("Decay Hound", "Great Decay", "Wolf", 1.1f, new Color(0.690f, 0.702f, 0.416f), "Native quadruped attacks; contamination art pending"),
        new("Graft Warden", "Great Decay", "Troll", 1f, new Color(0.690f, 0.702f, 0.416f), "Native heavy biped attacks; asymmetric graft art pending"),
        new("Corpse Orchard", "Great Decay", "Greydwarf_Shaman", 1.5f, new Color(0.690f, 0.702f, 0.416f), "Poison-cast motion proxy; mobile donor, no rooted colony or spawning"),
    };
    internal static readonly Entry[] Infrastructure =
    {
        new("Mycelial Walkway", "Fungal Forest", "wood_floor", 1f, new Color(0.596f, 0.824f, 0.698f), "Native building-piece prototype"),
        new("Mycelial Support", "Fungal Forest", "wood_pole2", 1f, new Color(0.596f, 0.824f, 0.698f), "Native building-piece prototype"),
        new("Mycelial Footing", "Fungal Forest", "stone_floor_2x2", 1f, new Color(0.596f, 0.824f, 0.698f), "Native building-piece prototype"),
        new("Blackwater Walkway", "Blackwater Deep", "wood_floor", 1f, new Color(0.459f, 0.710f, 0.769f), "Native building-piece prototype"),
        new("Blackwater Support", "Blackwater Deep", "wood_pole2", 1f, new Color(0.459f, 0.710f, 0.769f), "Native building-piece prototype"),
        new("Blackwater Footing", "Blackwater Deep", "stone_floor_2x2", 1f, new Color(0.459f, 0.710f, 0.769f), "Native building-piece prototype"),
        new("Cinder Walkway", "Sulfurous Wastes", "wood_floor", 1f, new Color(0.859f, 0.608f, 0.369f), "Native building-piece prototype"),
        new("Cinder Support", "Sulfurous Wastes", "wood_pole2", 1f, new Color(0.859f, 0.608f, 0.369f), "Native building-piece prototype"),
        new("Cinder Footing", "Sulfurous Wastes", "stone_floor_2x2", 1f, new Color(0.859f, 0.608f, 0.369f), "Native building-piece prototype"),
        new("Rime Walkway", "Frozen Caverns", "wood_floor", 1f, new Color(0.655f, 0.835f, 0.925f), "Native building-piece prototype"),
        new("Rime Support", "Frozen Caverns", "wood_pole2", 1f, new Color(0.655f, 0.835f, 0.925f), "Native building-piece prototype"),
        new("Rime Footing", "Frozen Caverns", "stone_floor_2x2", 1f, new Color(0.655f, 0.835f, 0.925f), "Native building-piece prototype"),
        new("Rift Walkway", "Fracture Zones", "wood_floor", 1f, new Color(0.741f, 0.624f, 0.863f), "Native building-piece prototype"),
        new("Rift Support", "Fracture Zones", "wood_pole2", 1f, new Color(0.741f, 0.624f, 0.863f), "Native building-piece prototype"),
        new("Rift Footing", "Fracture Zones", "stone_floor_2x2", 1f, new Color(0.741f, 0.624f, 0.863f), "Native building-piece prototype"),
        new("Rotroot Walkway", "Great Decay", "wood_floor", 1f, new Color(0.690f, 0.702f, 0.416f), "Native building-piece prototype"),
        new("Rotroot Support", "Great Decay", "wood_pole2", 1f, new Color(0.690f, 0.702f, 0.416f), "Native building-piece prototype"),
        new("Rotroot Footing", "Great Decay", "stone_floor_2x2", 1f, new Color(0.690f, 0.702f, 0.416f), "Native building-piece prototype"),
    };

}
