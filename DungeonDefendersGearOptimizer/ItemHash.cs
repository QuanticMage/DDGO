using System;
using System.Collections.Generic;
using System.Text;
namespace DDUP
{
	public static class ItemHash
	{
		// 30 bits total => 6 words * 5 bits (0..31)
		private const uint Mask30 = (1u << 30) - 1u;
		private const uint Mask5 = 0x1Fu;

		// ----------------------------
		// 32-word lists (index 0..31)
		// Grammar: Adj Adj GoodNoun Verb Adj BadNoun
		// ----------------------------

		public static readonly string[] Adjectives32 =
		{
			"Mythic","Ancient","Radiant","Corrupted","Enchanted","Hardened","Fortified","Shattered",
			"Brutal","Swift","Endless","Tactical","Heroic","Grim","Chaotic","Luminous",
			"Beloved","Cracked","Goated","Juicy","Sweaty","Mid","Fire","Locked",
			"Drippy","Buffed","Nerfed","Tilted","Cooked","Wild","Shiny","Fragile"
		};

		public static readonly string[] Verbs32 =
		{
			"Defends","Builds","Repairs","Summons","Channels","Fortifies","Breaches","Repels",
			"Smashes","Freezes","Shocks","Burns","Explodes","Shatters","Launches","Withstands",
			"Yeets","Clutches","Deletes","Farms","Camps","Rushes","Spams","Boosts",
			"Ratios","Snowballs","Throws","Wins","Locks","Pops","Wipes","Carries"
		};

		public static readonly string[] GoodNouns32 =
		{
			"Squire","Monk","Huntress","Apprentice",
			"Defender","Champion","Guardian","Builder",
			"Tower","Aura","Trap","Barricade",
			"Harpoon","Fireball","Lightning","Shiro",
			"Crystal","Core","Beacon","Obelisk",
			"Forge","Armory","Workshop","Stronghold",
			"Banner","Sigil","Totem","Relic",
			"Sentinel","Warden","Protector","Vanguard"
		};

		public static readonly string[] BadNouns32 =
		{
			"Goblin","Ogre","Wyvern","Kobold",
			"Orc","Demon","Dragon","Spider",
			"Minion","Brute","Berserker","Assassin",
			"Shaman","Warlock","Necromancer","Cultist",
			"Raider","Invader","Marauder","Pillager",
			"Swarm","Horde","Brood","Pack",
			"Beast","Fiend","Wretch","Abomination",
			"Corruptor","Ravager","Destroyer","Overlord"
		};

		// ----------------------------
		// Decoding lookup tables
		// (case-insensitive)
		// ----------------------------

		private static readonly Dictionary<string, int> AdjIndex = BuildIndex(Adjectives32);
		private static readonly Dictionary<string, int> VerbIndex = BuildIndex(Verbs32);
		private static readonly Dictionary<string, int> GoodIndex = BuildIndex(GoodNouns32);
		private static readonly Dictionary<string, int> BadIndex = BuildIndex(BadNouns32);

		private static Dictionary<string, int> BuildIndex(string[] words)
		{
			var d = new Dictionary<string, int>(words.Length, StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < words.Length; i++)
			{
				if (d.ContainsKey(words[i]))
					throw new InvalidOperationException($"Duplicate word in list: {words[i]}");
				d[words[i]] = i;
			}
			return d;
		}

		// ============================================================
		// 1) String -> 30-bit int
		// WASM-safe: pure managed FNV-1a over UTF-8 bytes.
		// ============================================================

		public static uint StringToInt30(string input)
			=> StringToInt30(input, seed: 0);

		public static uint StringToInt30(string input, uint seed)
		{
			if (input is null) throw new ArgumentNullException(nameof(input));

			// Pick a normalization policy and stick to it forever.
			// This makes phrases stable across platforms and user input quirks.
			string s = input.Normalize(NormalizationForm.FormC).ToLowerInvariant();

			byte[] bytes = Encoding.UTF8.GetBytes(s);

			uint h32 = Fnv1a32(bytes, seed);
			return h32 & Mask30;
		}

		// FNV-1a 32-bit with an optional seed mixed in (simple + deterministic).
		private static uint Fnv1a32(ReadOnlySpan<byte> data, uint seed)
		{
			const uint offset = 2166136261u;
			const uint prime = 16777619u;

			uint hash = offset;

			// Mix seed deterministically (so you can version/salt without changing input text)
			hash ^= (seed & 0xFF); hash *= prime;
			hash ^= ((seed >> 8) & 0xFF); hash *= prime;
			hash ^= ((seed >> 16) & 0xFF); hash *= prime;
			hash ^= ((seed >> 24) & 0xFF); hash *= prime;

			foreach (byte b in data)
			{
				hash ^= b;
				hash *= prime;
			}
			return hash;
		}

		// ============================================================
		// 2) 30-bit int -> 6-word phrase
		// Grammar: Adj Adj GoodNoun Verb Adj BadNoun
		// ============================================================

		public static string Int30ToPhrase(uint value30)
		{
			value30 &= Mask30;

			int a0 = (int)((value30 >> 0) & Mask5);
			int a1 = (int)((value30 >> 5) & Mask5);
			int gn = (int)((value30 >> 10) & Mask5);
			int vb = (int)((value30 >> 15) & Mask5);
			int a2 = (int)((value30 >> 20) & Mask5);
			int bn = (int)((value30 >> 25) & Mask5);

			return string.Join(' ',
				Adjectives32[a0],
				Adjectives32[a1],
				GoodNouns32[gn],
				Verbs32[vb],
				Adjectives32[a2],
				BadNouns32[bn]
			);
		}

		// ============================================================
		// 3) Phrase -> 30-bit int (reverse of Int30ToPhrase)
		// ============================================================

		public static bool TryPhraseToInt30(string phrase, out uint value30)
		{
			value30 = 0;

			if (string.IsNullOrWhiteSpace(phrase))
				return false;

			// Split on whitespace; TrimEntries handles extra spaces.
			string[] parts = phrase.Split((char[])null!,
				StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			if (parts.Length != 6)
				return false;

			if (!AdjIndex.TryGetValue(parts[0], out int a0)) return false;
			if (!AdjIndex.TryGetValue(parts[1], out int a1)) return false;
			if (!GoodIndex.TryGetValue(parts[2], out int gn)) return false;
			if (!VerbIndex.TryGetValue(parts[3], out int vb)) return false;
			if (!AdjIndex.TryGetValue(parts[4], out int a2)) return false;
			if (!BadIndex.TryGetValue(parts[5], out int bn)) return false;

			value30 =
				((uint)a0 << 0) |
				((uint)a1 << 5) |
				((uint)gn << 10) |
				((uint)vb << 15) |
				((uint)a2 << 20) |
				((uint)bn << 25);

			value30 &= Mask30;
			return true;
		}
	}
}