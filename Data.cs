using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EndlessAmmoInventory.Data {
	public struct EndlessAmmoType {
		public int Type;
		public int PreviewItemType;
		public string Title;
		public string NoSelected;
		public EndlessAmmoType(string title, string noSelected, int ammoType, int previewItemType) {
			Title = title;
			NoSelected = noSelected;
			Type = ammoType;
			PreviewItemType = previewItemType;
		}
	}

	class DataContainer {
		static public List<Item> AmmoItems = new List<Item>();
		static public EndlessAmmoType[] EndlessAmmoTypes = {
			new EndlessAmmoType("Select Bullet", "No bullets unlocked.", AmmoID.Bullet, ItemID.MusketBall),
			new EndlessAmmoType("Select Arrow", "No arrows unlocked.", AmmoID.Arrow, ItemID.WoodenArrow),
			new EndlessAmmoType("Select Rocket", "No rockets unlocked.", AmmoID.Rocket, ItemID.RocketI),
			new EndlessAmmoType("Select Solution", "No solutions unlocked.",AmmoID.Solution, ItemID.GreenSolution),
		};

		public static void LoadItems() {
			if (AmmoItems.Count != 0)
				return;

			List<int> AmmoIds = new List<int>();
			foreach (EndlessAmmoType EndlessAmmoType in EndlessAmmoTypes)
				AmmoIds.Add(EndlessAmmoType.Type);

			for (int type = 0; type < ItemLoader.ItemCount; type++) {
				Item item = new Item();
				item.SetDefaults(type);
				if (item.consumable && AmmoIds.Contains(item.ammo))
					AmmoItems.Add(item.Clone());
			}
		}

		public static void Unload() {
			AmmoItems.Clear();
		}
	}
}
