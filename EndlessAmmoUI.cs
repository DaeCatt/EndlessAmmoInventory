using EndlessAmmoInventory.Helpers;
using EndlessAmmoInventory.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace EndlessAmmoInventory.UI {
	struct AmmoData {
		public Item Item;
		public bool Unlocked;
		public int Count;
		public int UnlockAmount;
		public string UnlockString;
		public bool CanUnlock;

		public AmmoData(Item item, bool unlocked, int count, int unlockAmount) {
			Item = item;
			Unlocked = unlocked;
			Count = Math.Min(count, unlockAmount);
			UnlockAmount = unlockAmount;
			UnlockString = $"{count} / {unlockAmount}";
			CanUnlock = count >= unlockAmount;
		}
	}

	class EndlessAmmoUI : UIState {
		public static readonly int UNLOCK_AMOUNT = 3996;
		public static int AmmoPicker = AmmoID.None;

		public Color PreviewColor = new Color(255 / 3, 255 / 3, 255 / 3, 255 / 3);
		private Color UnlockColor = new Color(255, 215, 0);

		private static readonly int DeltaX = 534 - 497;
		private static readonly int LeftX = 534 + DeltaX;
		private static RasterizerState overflowHiddenState;

		private readonly VelocityAnimation ScrollPosition = new VelocityAnimation();
		// public static bool IsDraggingScrollbar = false;

		private bool isMasked = false;
		private Rectangle scissorRectangle;
		private void MaskDrawTo(SpriteBatch spriteBatch, int x, int y, int width, int height) {
			if (isMasked)
				throw new Exception("spriteBatch is already masked.");

			isMasked = true;
			scissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;

			spriteBatch.End();
			spriteBatch.GraphicsDevice.ScissorRectangle = Rectangle.Intersect(new Rectangle(x, y, width, height), scissorRectangle);
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, overflowHiddenState, null, Main.UIScaleMatrix);
		}

		private void ResetMask(SpriteBatch spriteBatch) {
			if (!isMasked)
				return;

			spriteBatch.End();
			spriteBatch.GraphicsDevice.ScissorRectangle = scissorRectangle;
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, spriteBatch.GraphicsDevice.RasterizerState, null, Main.UIScaleMatrix);
			isMasked = false;
		}

		public override void OnInitialize() {
			Scrollbar.OnInitialize();
			if (overflowHiddenState == null) {
				overflowHiddenState = new RasterizerState {
					CullMode = CullMode.None,
					ScissorTestEnable = true
				};
			}
		}

		private void DrawAmmoPicker(SpriteBatch spriteBatch, EndlessAmmoPlayer modPlayer, ref string mouseText, ref Item hoverItem) {
			Main.HidePlayerCraftingMenu = true;

			int dx = LeftX;
			Color titleColor = new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor);
			Point mousePoint = new Point(Main.mouseX, Main.mouseY);

			Main.LocalPlayer.mouseInterface = true;
			Color locked = new Color(187, 187, 187, 187);

			float stringScale = 0.75f;
			float gap = 8;

			List<AmmoData> ammoData = new List<AmmoData>();
			float widestAmmoName = 0;
			float stringHeight = Main.fontMouseText.LineSpacing;

			foreach (Item ammoItem in DataContainer.AmmoItems) {
				if (ammoItem.ammo != AmmoPicker)
					continue;

				bool unlocked = modPlayer.HasEndlessAmmoItemUnlocked(ammoItem.type);
				int count = modPlayer.CountItemsInInventory(ammoItem.type);
				AmmoData data = new AmmoData(ammoItem, unlocked, count, UNLOCK_AMOUNT);
				ammoData.Add(data);

				widestAmmoName = Math.Max(
					widestAmmoName,
					Math.Max(
						Main.fontMouseText.MeasureString(ammoItem.Name).X,
						Main.fontMouseText.MeasureString(data.UnlockString).X
					)
				);
			}

			Vector2 stringLeftAlignedVertCenter = new Vector2(0, stringHeight / 2);

			widestAmmoName *= stringScale;
			stringHeight *= stringScale;

			float lineHeight = stringHeight * 0.6f;

			foreach (EndlessAmmoType ammoType in DataContainer.EndlessAmmoTypes) {
				if (ammoType.Type == AmmoPicker) {
					ChatManager.DrawColorCodedString(spriteBatch, Main.fontMouseText, ammoType.Title, new Vector2(dx, 84f), titleColor, 0, Vector2.Zero, Vector2.One * 0.75f);
					break;
				}
			}

			float unit = 56 * Main.inventoryScale;
			Rect dropdownRect = new Rect(
				dx,
				105f,
				unit + widestAmmoName + gap,
				Main.inventoryScale * (52 + (56 * Math.Min(3, ammoData.Count - 1)))
			);

			// float scrollMin = 0;
			float scrollTop = (float) (ScrollPosition.GetValue() * unit);
			// float scrollMax = (ammoData.Count - 4) * unit;

			float scrollbarWidth = 20 * Main.inventoryScale;
			bool drawScrollbar = ammoData.Count > 4;
			if (drawScrollbar) {
				dropdownRect.Width += scrollbarWidth + 4;
			}

			ScalableItemSlot.DrawPanel(spriteBatch, dropdownRect, Main.inventoryScale);

			if (drawScrollbar) {
				Rect scrollbarRect = new Rect(dropdownRect.Right - scrollbarWidth - 4, dropdownRect.Y + 4, scrollbarWidth, dropdownRect.Height - 8);

				float offsetHeight = dropdownRect.Height;
				float scrollHeight = Main.inventoryScale * (52 + (56 * (ammoData.Count - 1)));
				Scrollbar.Draw(spriteBatch, scrollbarRect, offsetHeight, scrollHeight, scrollTop, Main.inventoryBack, Main.inventoryScale);

				MaskDrawTo(spriteBatch,
					1 + (int) (Main.UIScale * dropdownRect.X),
					1 + (int) (Main.UIScale * dropdownRect.Y),
					((int) (Main.UIScale * dropdownRect.Width)) - 2,
					((int) (Main.UIScale * dropdownRect.Height)) - 2
				);
			}

			Rect itemRect = new Rect(dx, 105 - scrollTop, 52, 52);
			itemRect.Dimensions *= Main.inventoryScale;

			Rect textRect = new Rect(itemRect.Position, unit + widestAmmoName, 52);
			textRect.Dimensions *= Main.inventoryScale;

			for (int i = 0; i < ammoData.Count; i++) {
				if (itemRect.Bottom < dropdownRect.Y) {
					itemRect.Y += unit;
					textRect.Y += unit;
					continue;
				}

				if (itemRect.Y > dropdownRect.Bottom) {
					break;
				}

				AmmoData ammo = ammoData[i];
				ScalableItemSlot.DrawItem(spriteBatch, itemRect, ammo.Item, Main.inventoryBack, Main.inventoryScale);

				Vector2 position = itemRect.ClonePosition();
				position.X += unit;
				position.Y += unit / 2 + 2;

				if (ammo.Unlocked) {
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch,
						Main.fontMouseText,
						ammo.Item.Name,
						position,
						Color.White,
						0f,
						stringLeftAlignedVertCenter,
						Vector2.One * stringScale
					);
				} else {
					position.Y -= lineHeight / 2;
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch,
						Main.fontMouseText,
						ammo.Item.Name,
						position,
						ammo.CanUnlock ? UnlockColor : locked,
						0f,
						stringLeftAlignedVertCenter,
						Vector2.One * stringScale
					);

					position.Y += lineHeight;
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch,
						Main.fontMouseText,
						ammo.UnlockString,
						position,
						ammo.CanUnlock ? Color.White : locked,
						0f,
						stringLeftAlignedVertCenter,
						Vector2.One * stringScale
					);
				}

				if (textRect.Contains(mousePoint)) {
					if (Main.mouseLeft && Main.mouseLeftRelease) {
						if (ammo.Unlocked) {
							if (modPlayer.SelectUnlockedAmmo(ammo.Item.type))
								Main.PlaySound(SoundID.Grab);

							AmmoPicker = AmmoID.None;
							break;
						}

						if (ammo.CanUnlock && !itemRect.Contains(mousePoint)) {
							if (modPlayer.UnlockEndlessAmmo(ammo.Item.type))
								Main.PlaySound(SoundID.Grab);

							AmmoPicker = AmmoID.None;
							break;
						}
					}

					if (itemRect.Contains(mousePoint)) {
						hoverItem = ammo.Item.Clone();
						hoverItem.ammo = 0;
						hoverItem.material = false;
						hoverItem.consumable = false;
					} else if (ammo.Unlocked) {
						mouseText = string.Format("Select {0}", ammo.Item.Name);
					} else if (ammo.CanUnlock) {
						mouseText = string.Format("Unlock {0}", ammo.Item.Name);
					} else {
						mouseText = string.Format("Unlock {0} ({1})", ammo.Item.Name, ammo.UnlockString);
					}
				}

				itemRect.Y += unit;
				textRect.Y += unit;
			}

			if (AmmoPicker != AmmoID.None && !dropdownRect.Contains(mousePoint) && Main.mouseLeft && Main.mouseLeftRelease) {
				Main.PlaySound(SoundID.MenuClose);
				AmmoPicker = AmmoID.None;
			} else if (PlayerInput.ScrollWheelDeltaForUI != 0) {
				int delta = PlayerInput.ScrollWheelDeltaForUI < 0 ? 1 : -1;
				int currentTarget = (int) ScrollPosition.Target;
				int newTarget = Math.Max(0, Math.Min(ammoData.Count - 4, delta + currentTarget));

				if (currentTarget != newTarget) {
					Main.PlaySound(SoundID.MenuTick);
					ScrollPosition.SetValue(newTarget);
				}

				PlayerInput.ScrollWheelDeltaForUI = 0;
			}

			ResetMask(spriteBatch);
		}

		private void DrawUnlockedAmmo(SpriteBatch spriteBatch, EndlessAmmoPlayer modPlayer, ref string mouseText, ref Item hoverItem) {
			Vector2 stringScale = Vector2.One * 0.75f;

			int dx = LeftX;
			Color titleColor = new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor);
			Point mousePoint = new Point(Main.mouseX, Main.mouseY);

			string EndlessAmmoLabel = "Endless Ammo";
			ChatManager.DrawColorCodedString(spriteBatch, Main.fontMouseText, EndlessAmmoLabel, new Vector2(dx, 84f), titleColor, 0, Vector2.Zero, stringScale);
			Rect checkboxRect = new Rect(dx, 84, 16, 16);
			checkboxRect.X += (Main.fontMouseText.MeasureString(EndlessAmmoLabel) * stringScale).X + 4;

			EndlessAmmoInventory.SmallItemSlotTexture.Draw(spriteBatch, checkboxRect, Color.White, Main.inventoryScale);
			if (modPlayer.useEndlessAmmoFirst) {
				string checkmark = "✓";

				Vector2 stringRect = Main.fontMouseText.MeasureString(checkmark) * stringScale;
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch,
					Main.fontMouseText,
					checkmark,
					checkboxRect.Center(),
					Color.White,
					0f,
					stringRect / 2,
					stringScale
				);
			}

			if (checkboxRect.Contains(mousePoint)) {
				Main.LocalPlayer.mouseInterface = true;
				mouseText = "Use endless ammo first.";

				if (Main.mouseLeft && Main.mouseLeftRelease) {
					Main.PlaySound(SoundID.MenuTick);
					modPlayer.useEndlessAmmoFirst = !modPlayer.useEndlessAmmoFirst;
				}
			}

			Rect slotRect = new Rect(52, 52);
			slotRect.Dimensions *= Main.inventoryScale;

			int typeCount = DataContainer.EndlessAmmoTypes.Length;
			for (int i = 0; i < typeCount; i++) {
				int x = i / 4;
				int y = i % 4;

				slotRect.X = dx + x * DeltaX;
				slotRect.Y = (int) (105f + y * 56 * Main.inventoryScale);

				EndlessAmmoType AmmoType = DataContainer.EndlessAmmoTypes[i];
				Item ammo = modPlayer.GetItemForEndlessAmmoType(AmmoType.Type);

				if (ammo.type == ItemID.None) {
					Item refItem = new Item();
					refItem.SetDefaults(AmmoType.PreviewItemType);
					ScalableItemSlot.DrawPanel(spriteBatch, slotRect, Main.inventoryScale);
					ScalableItemSlot.DrawItem(spriteBatch, slotRect, refItem, PreviewColor, Main.inventoryScale);
				} else {
					ScalableItemSlot.DrawPanel(spriteBatch, slotRect, Main.inventoryScale);
					ScalableItemSlot.DrawItem(spriteBatch, slotRect, ammo, Main.inventoryBack, Main.inventoryScale);
				}

				if (modPlayer.CanUnlockAmmoForType(AmmoType.Type)) {
					Vector2 hPosition = slotRect.ClonePosition();
					hPosition.X += 24;
					hPosition.Y += 4;
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch,
						Main.fontMouseText,
						"!",
						hPosition,
						UnlockColor,
						0f,
						Vector2.Zero,
						Vector2.One * 0.6f
					);
				}

				if (AmmoPicker == AmmoID.None && slotRect.Contains(mousePoint)) {
					Main.LocalPlayer.mouseInterface = true;
					if (ammo.type != ItemID.None) {
						hoverItem = ammo.Clone();
						hoverItem.ammo = 0;
						hoverItem.material = false;
						hoverItem.consumable = false;
					} else {
						mouseText = DataContainer.EndlessAmmoTypes[i].NoSelected;
					}

					if (Main.mouseLeft && Main.mouseLeftRelease) {
						Main.PlaySound(SoundID.MenuOpen);
						AmmoPicker = AmmoType.Type;
					}
				}
			}
		}

		public override void Draw(SpriteBatch spriteBatch) {
			EndlessAmmoPlayer modPlayer = Main.LocalPlayer.GetModPlayer<EndlessAmmoPlayer>();
			if (!modPlayer.hasEndlessAmmo) {
				base.Draw(spriteBatch);
				return;
			}

			Main.inventoryScale = 0.6f;

			string mouseText = "";
			Item hoverItem = null;

			if (AmmoPicker != AmmoID.None) {
				DrawAmmoPicker(spriteBatch, modPlayer, ref mouseText, ref hoverItem);
			} else {
				ScrollPosition.SetValueImmediate(0);
				DrawUnlockedAmmo(spriteBatch, modPlayer, ref mouseText, ref hoverItem);
			}

			if (hoverItem != null) {
				Main.HoverItem = hoverItem.Clone();
				Main.instance.MouseText(hoverItem.Name, hoverItem.rare, 0);
			} else if (mouseText != "") {
				Main.instance.MouseText(mouseText);
			}

			base.Draw(spriteBatch);
		}
	}
}