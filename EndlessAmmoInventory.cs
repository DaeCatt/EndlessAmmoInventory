using EndlessAmmoInventory.Data;
using EndlessAmmoInventory.Helpers;
using EndlessAmmoInventory.Hooks;
using EndlessAmmoInventory.UI;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace EndlessAmmoInventory {
	class EndlessAmmoInventory : Mod {
		public static ScalableTexture2D SmallItemSlotTexture;

		public UserInterface EndlessAmmoUserInterface;
		public EndlessAmmoUI EndlessAmmoUIInstance;

		public override void Load() {
			IL.Terraria.Player.HasAmmo += Hook.HasAmmo;
			IL.Terraria.Player.PickAmmo += Hook.PickAmmo;
			IL.Terraria.UI.ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += Hook.ItemSlotDraw;

			if (Main.dedServ)
				return;

			if (SmallItemSlotTexture == null)
				SmallItemSlotTexture = new ScalableTexture2D(GetTexture("InventoryBackSmall"), 4);

			// Create Gnome Reforge Interface

			EndlessAmmoUIInstance = new EndlessAmmoUI();
			EndlessAmmoUserInterface = new UserInterface();
			EndlessAmmoUserInterface.SetState(EndlessAmmoUIInstance);
		}

		public override void Unload() {
			EndlessAmmoUIInstance = null;
			EndlessAmmoUserInterface = null;

			DataContainer.Unload();
			base.Unload();
		}

		// Insert our interface layers
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
			if (inventoryIndex == -1)
				throw new Exception("Could not find 'Vanilla: Inventory'");

			layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
				"DaesMod: Endless Ammo UI",
				delegate {
					if (Main.playerInventory) {
						EndlessAmmoUserInterface.Draw(Main.spriteBatch, new GameTime());
					} else {
						EndlessAmmoUI.AmmoPicker = AmmoID.None;
					}

					return true;
				},
				InterfaceScaleType.UI
			));
		}
	}

	internal class EndlessAmmoPlayer : ModPlayer {
		public bool hasEndlessAmmo = false;
		public bool useEndlessAmmoFirst = false;
		public List<Item> UnlockedAmmo = new List<Item>();
		public List<bool> SelectedAmmo = new List<bool>();

		public override TagCompound Save() {
			return new TagCompound {
				[nameof(hasEndlessAmmo)] = hasEndlessAmmo,
				[nameof(useEndlessAmmoFirst)] = useEndlessAmmoFirst,
				[nameof(UnlockedAmmo)] = UnlockedAmmo,
				[nameof(SelectedAmmo)] = SelectedAmmo
			};
		}

		public override void Load(TagCompound tag) {
			hasEndlessAmmo = tag.GetBool(nameof(hasEndlessAmmo));
			useEndlessAmmoFirst = tag.GetBool(nameof(useEndlessAmmoFirst));
			UnlockedAmmo = tag.Get<List<Item>>(nameof(UnlockedAmmo));
			SelectedAmmo = tag.Get<List<bool>>(nameof(SelectedAmmo));
		}

		public override void OnEnterWorld(Player player) {
			DataContainer.LoadItems();
		}

		public void UnlockEndlessAmmo() {
			if (hasEndlessAmmo)
				return;

			hasEndlessAmmo = true;

			Item musketBall = new Item();
			musketBall.SetDefaults(ItemID.MusketBall);
			UnlockedAmmo.Add(musketBall);
			SelectedAmmo.Add(true);

			Item arrow = new Item();
			arrow.SetDefaults(ItemID.WoodenArrow);
			UnlockedAmmo.Add(arrow);
			SelectedAmmo.Add(true);
		}

		public Item GetItemForEndlessAmmoType(int ammoType) {
			if (!hasEndlessAmmo)
				return new Item();

			for (int i = 0; i < SelectedAmmo.Count; i++) {
				if (SelectedAmmo[i]) {
					Item selectedAmmo = UnlockedAmmo[i];
					if (selectedAmmo.ammo == ammoType)
						return selectedAmmo;
				}
			}

			for (int i = SelectedAmmo.Count; i < UnlockedAmmo.Count; i++) {
				SelectedAmmo.Add(false);
			}

			for (int i = 0; i < UnlockedAmmo.Count; i++) {
				Item possibleAmmo = UnlockedAmmo[i];
				if (possibleAmmo.ammo == ammoType) {
					SelectedAmmo[i] = true;
					return possibleAmmo;
				}
			}

			return new Item();
		}

		public bool HasEndlessAmmoItemUnlocked(int type) {
			foreach (Item unlocked in UnlockedAmmo) {
				if (unlocked.type == type)
					return true;
			}

			return false;
		}

		public bool SelectUnlockedAmmo(int type) {
			if (!HasEndlessAmmoItemUnlocked(type))
				return false;

			Item ammo = new Item();
			ammo.SetDefaults(type);

			int ammoType = ammo.ammo;
			for (int i = 0; i < UnlockedAmmo.Count; i++) {
				Item possibleAmmo = UnlockedAmmo[i];
				if (possibleAmmo.ammo == ammoType)
					SelectedAmmo[i] = possibleAmmo.type == type;
			}

			return true;
		}

		public bool UnlockEndlessAmmo(int type) {
			if (SelectUnlockedAmmo(type))
				return true;

			if (!ConsumeItem(type, EndlessAmmoUI.UNLOCK_AMOUNT))
				return false;

			Item ammo = new Item();
			ammo.SetDefaults(type);
			UnlockedAmmo.Add(ammo);
			SelectedAmmo.Add(false);

			return SelectUnlockedAmmo(type);
		}

		public int CountItemsInInventory(int type) {
			int count = 0;
			for (int i = 0; i < 58; i++) {
				if (player.inventory[i].type == type) {
					count += player.inventory[i].stack;
				}
			}

			return count;
		}

		public bool ConsumeItem(int type, int count) {
			if (CountItemsInInventory(type) < count)
				return false;

			int remaining = count;
			for (int i = 0; i < 58 && remaining > 0; i++) {
				if (player.inventory[i].type != type)
					continue;

				if (player.inventory[i].stack > remaining) {
					player.inventory[i].stack -= remaining;
					break;
				}

				remaining -= player.inventory[i].stack;
				player.inventory[i].TurnToAir();
			}

			return true;
		}

		public bool CanUnlockAmmoForType(int type) {
			foreach (Item item in DataContainer.AmmoItems) {
				if (item.ammo != type)
					continue;

				if (HasEndlessAmmoItemUnlocked(item.type))
					continue;

				if (CountItemsInInventory(item.type) >= EndlessAmmoUI.UNLOCK_AMOUNT)
					return true;
			}
			return false;
		}
	}

	class EndlessAmmoInventoryItem : ModItem {
		public override void SetDefaults() {
			item.maxStack = 1;
			item.consumable = true;
			item.value = 0;
			item.width = 14;
			item.height = 14;
			item.useStyle = ItemUseStyleID.HoldingUp; // Like a life crystal
			item.rare = ItemRarityID.LightRed; // LightRed, early hardmode items
		}

		public override bool CanUseItem(Player player) {
			EndlessAmmoPlayer modPlayer = player.GetModPlayer<EndlessAmmoPlayer>();
			return !modPlayer.hasEndlessAmmo;
		}

		public override bool UseItem(Player player) {
			EndlessAmmoPlayer modPlayer = player.GetModPlayer<EndlessAmmoPlayer>();
			if (modPlayer.hasEndlessAmmo)
				return false;

			modPlayer.UnlockEndlessAmmo();
			return true;
		}

		public override void AddRecipes() {
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.EndlessMusketPouch);
			recipe.AddIngredient(ItemID.EndlessQuiver);
			recipe.AddTile(TileID.TinkerersWorkbench);
			recipe.AddTile(TileID.CrystalBall);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}