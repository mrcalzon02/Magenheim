using UnityEngine;
namespace Magenheim.Runtime;

// Underworld fauna roster. Entries intentionally reuse proven donor chassis; Magenheim replaces surface identity, temperament, physiology and biome effects at registration.
internal static class UnderworldCreaturePrototypes
{
    internal sealed record Entry(string Name, string Biome, string Donor, float Scale, Color Color, string Limit)
    {
        internal string Prefab => "Magenheim_Underworld_Prototype_" + Name.Replace(" ", "");

        internal string? AuthoredModelId => Name switch
        {
            "Lantern Moth" => "underworld-creature-lantern-moth",
            "Sporeling" => "underworld-creature-sporeling",
            "Capcrawler" => "underworld-creature-capcrawler",
            "Mycelial Stalker" => "underworld-creature-mycelial-stalker",
            "Puffback" => "underworld-creature-puffback",
            "Shelf Lurker" => "underworld-creature-shelf-lurker",
            "Crowncap Brute" => "underworld-creature-crowncap-brute",
            "Cave Ray" => "underworld-creature-cave-ray",
            "Gloomfin" => "underworld-creature-gloomfin",
            "Blackwater Lamprey" => "underworld-creature-blackwater-lamprey",
            "Shoreclaw" => "underworld-creature-shoreclaw",
            "Lantern Angler" => "underworld-creature-lantern-angler",
            "Abyss Shellback" => "underworld-creature-abyss-shellback",
            "Deep Hunter" => "underworld-creature-deep-hunter",
            "Ashmite" => "underworld-creature-ashmite",
            "Cinder Hound" => "underworld-creature-cinder-hound",
            "Basalt Crawler" => "underworld-creature-basalt-crawler",
            "Vent Spitter" => "underworld-creature-vent-spitter",
            "Rime Moth" => "underworld-creature-rime-moth",
            "Frost Tick" => "underworld-creature-frost-tick",
            "Iceblind" => "underworld-creature-iceblind",
            "Pale Burrower" => "underworld-creature-pale-burrower",
            "Rimewing" => "underworld-creature-rimewing",
            "Glacier Stalker" => "underworld-creature-glacier-stalker",
            "Cryolith Guardian" => "underworld-creature-cryolith-guardian",
            _ => null,
        };
    }
    internal static readonly Entry[] All =
    {
        new("Lantern Moth", "Fungal Forest", "Bat", 0.65f, new Color(0.596f, 0.824f, 0.698f), "Four-wing production author/animation pipeline ready; Blender source/runtime payload regeneration pending; donor fallback remains until payload exists"),
        new("Sporeling", "Fungal Forest", "Tick", 0.6f, new Color(0.596f, 0.824f, 0.698f), "Production creature source/rig authored; runtime payload export pending; donor fallback remains until payload exists"),
        new("Capcrawler", "Fungal Forest", "Seeker", 0.65f, new Color(0.596f, 0.824f, 0.698f), "Production creature source/rig authored; runtime payload export pending; donor fallback remains until payload exists"),
        new("Mycelial Stalker", "Fungal Forest", "Wolf", 1.05f, new Color(0.596f, 0.824f, 0.698f), "Production Stalker author/animation pipeline ready with conceal/pounce actions; Blender source/runtime payload regeneration pending"),
        new("Puffback", "Fungal Forest", "Lox", 0.8f, new Color(0.596f, 0.824f, 0.698f), "Reactive spore burst implemented; production Puffback author/animation pipeline ready with inflation/spore actions; Blender source/runtime payload regeneration pending"),
        new("Shelf Lurker", "Fungal Forest", "Seeker", 1f, new Color(0.596f, 0.824f, 0.698f), "Production Shelf Lurker author/animation pipeline ready; donor fallback remains until payload exists; wall-clinging locomotion still requires gameplay integration"),
        new("Crowncap Brute", "Fungal Forest", "Troll", 0.9f, new Color(0.596f, 0.824f, 0.698f), "Production Crowncap author/animation pipeline ready with crown/gill anatomy; Blender source/runtime payload regeneration pending"),
        new("Cave Ray", "Blackwater Deep", "Serpent", 0.45f, new Color(0.459f, 0.710f, 0.769f), "Production Cave Ray body/actions wired for runtime export; hit-triggered flee temperament implemented"),
        new("Gloomfin", "Blackwater Deep", "Serpent", 0.55f, new Color(0.459f, 0.710f, 0.769f), "Production Gloomfin body/actions wired for runtime export; donor gameplay chassis retained"),
        new("Blackwater Lamprey", "Blackwater Deep", "Leech", 1f, new Color(0.459f, 0.710f, 0.769f), "Production Lamprey body/actions wired for runtime export; timed attach-and-feed latch implemented"),
        new("Shoreclaw", "Blackwater Deep", "Neck", 1.5f, new Color(0.459f, 0.710f, 0.769f), "Production Shoreclaw shell/claw body and actions wired for runtime export"),
        new("Lantern Angler", "Blackwater Deep", "Serpent", 0.7f, new Color(0.459f, 0.710f, 0.769f), "Production Lantern Angler body/actions wired for runtime export; radial pressure-release gameplay implemented; lure behavior still pending"),
        new("Abyss Shellback", "Blackwater Deep", "Neck", 2f, new Color(0.459f, 0.710f, 0.769f), "Shell armor with pickaxe counter implemented; production Shellback body/guard actions wired for runtime export"),
        new("Deep Hunter", "Blackwater Deep", "Serpent", 1.2f, new Color(0.459f, 0.710f, 0.769f), "Production Deep Hunter hero body/actions wired for runtime export; owner-authoritative apex ram implemented; additional hero combat still pending"),
        new("Ashmite", "Sulfurous Wastes", "Tick", 0.45f, new Color(0.859f, 0.608f, 0.369f), "Production Ashmite heat-shell body/actions wired for runtime export; donor gameplay chassis retained"),
        new("Cinder Hound", "Sulfurous Wastes", "Wolf", 1f, new Color(0.859f, 0.608f, 0.369f), "Production Cinder Hound heat-adapted body/actions wired for runtime export; pack signaling gameplay still open"),
        new("Basalt Crawler", "Sulfurous Wastes", "SeekerBrute", 0.65f, new Color(0.859f, 0.608f, 0.369f), "Production Basalt Crawler body/actions wired for runtime export; donor armored-strike gameplay retained"),
        new("Vent Spitter", "Sulfurous Wastes", "Neck", 1.25f, new Color(0.859f, 0.608f, 0.369f), "Line-of-sight thermal spit retaliation implemented; production pressure-sac body and physical charge/spit/recoil actions wired for runtime export"),
        new("Fume Wraith", "Sulfurous Wastes", "Wraith", 1f, new Color(0.859f, 0.608f, 0.369f), "Native spectral flight/melee; no new smoke rig"),
        new("Magma Leaper", "Sulfurous Wastes", "Fenring", 0.75f, new Color(0.859f, 0.608f, 0.369f), "Native leap/attack; biped donor silhouette retained for review"),
        new("Furnace Golem", "Sulfurous Wastes", "StoneGolem", 1.1f, new Color(0.859f, 0.608f, 0.369f), "Native heavy construct attacks; no armor-break mechanic"),
        new("Rime Moth", "Frozen Caverns", "Bat", 0.7f, new Color(0.655f, 0.835f, 0.925f), "Four-wing crystalline Rime Moth body/actions authored for runtime export"),
        new("Frost Tick", "Frozen Caverns", "Tick", 0.7f, new Color(0.655f, 0.835f, 0.925f), "Eight-legged ice-carapace Frost Tick body/actions authored for runtime export; donor latch gameplay retained"),
        new("Iceblind", "Frozen Caverns", "Wolf", 1.1f, new Color(0.655f, 0.835f, 0.925f), "Eyeless sensory-crown Iceblind body/actions authored for runtime export; donor quadruped gameplay retained"),
        new("Pale Burrower", "Frozen Caverns", "Seeker", 0.85f, new Color(0.655f, 0.835f, 0.925f), "Shovel-limbed Pale Burrower body/actions authored for runtime export; burrowing gameplay integration pending"),
        new("Rimewing", "Frozen Caverns", "Hatchling", 1f, new Color(0.655f, 0.835f, 0.925f), "Crystal-wing Rimewing body/actions authored for runtime export; perch lifecycle gameplay pending"),
        new("Glacier Stalker", "Frozen Caverns", "Wolf", 1.2f, new Color(0.655f, 0.835f, 0.925f), "Low ice-armored Glacier Stalker body/actions authored for runtime export; donor pursuit/bite gameplay retained"),
        new("Cryolith Guardian", "Frozen Caverns", "StoneGolem", 1f, new Color(0.655f, 0.835f, 0.925f), "Cryolith Guardian construct body/actions authored for runtime export; donor heavy construct gameplay retained"),
        new("Fracture Wisp", "Fracture Zones", "FrostWisp", 0.8f, new Color(0.741f, 0.624f, 0.863f), "Native flying wisp; no orbiting-piece system"),
        new("Rift Skitter", "Fracture Zones", "Seeker", 0.65f, new Color(0.741f, 0.624f, 0.863f), "Native insect locomotion; no wall adhesion"),
        new("Shardwing", "Fracture Zones", "Hatchling", 0.9f, new Color(0.741f, 0.624f, 0.863f), "Native flight/projectile; mineral wing art pending"),
        new("Gravity Leech", "Fracture Zones", "Leech", 1.1f, new Color(0.741f, 0.624f, 0.863f), "Aquatic motion proxy only; no land crawl or pull field"),
        new("Chasm Stalker", "Fracture Zones", "Seeker", 1.15f, new Color(0.741f, 0.624f, 0.863f), "Native insect locomotion; long-limb art pending"),
        new("Stonebound", "Fracture Zones", "StoneGolem", 0.7f, new Color(0.741f, 0.624f, 0.863f), "Native grounded construct; independent floating masses deferred"),
        new("Rift Colossus", "Fracture Zones", "StoneGolem", 1.3f, new Color(0.741f, 0.624f, 0.863f), "Native heavy construct; no custom displacement system"),
        new("Rotling", "Great Decay", "Tick", 0.8f, new Color(0.690f, 0.702f, 0.416f), "Native scuttle/latch; biomass anatomy pending"),
        new("Carrion Bloom", "Great Decay", "Greydwarf_Shaman", 0.85f, new Color(0.690f, 0.702f, 0.416f), "Rooted caster behavior implemented; rooted bloom anatomy pending"),
        new("Spore Husk", "Great Decay", "Draugr", 1f, new Color(0.690f, 0.702f, 0.416f), "Native humanoid equipment/attacks; grafted silhouette pending"),
        new("Marrow Creeper", "Great Decay", "Seeker", 0.7f, new Color(0.690f, 0.702f, 0.416f), "Native insect locomotion; bone-supported anatomy pending"),
        new("Decay Hound", "Great Decay", "Wolf", 1.1f, new Color(0.690f, 0.702f, 0.416f), "Native quadruped attacks; contamination art pending"),
        new("Graft Warden", "Great Decay", "Troll", 1f, new Color(0.690f, 0.702f, 0.416f), "Native heavy biped attacks; asymmetric graft art pending"),
        new("Corpse Orchard", "Great Decay", "Greydwarf_Shaman", 1.5f, new Color(0.690f, 0.702f, 0.416f), "Rooted colony and Rotling propagation implemented; final colony anatomy pending"),
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
