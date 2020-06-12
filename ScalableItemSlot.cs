using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using EndlessAmmoInventory.Helpers;

namespace EndlessAmmoInventory.UI {
	class ScalableItemSlot {
		public static ScalableTexture2D texture = new ScalableTexture2D(Main.inventoryBackTexture, 8);

		public static void DrawPanel(SpriteBatch spriteBatch, Rect rect, float detailScale = 1) {
			texture.Draw(spriteBatch, rect, Main.inventoryBack, detailScale);
		}

		public static void DrawItem(SpriteBatch spriteBatch, Rect rect, Item item, Color color, float scale = 1) {
			Texture2D itemTexture = Main.itemTexture[item.type];
			Rectangle itemSourceRect = (Main.itemAnimations[item.type] == null) ? itemTexture.Frame() : Main.itemAnimations[item.type].GetFrame(itemTexture);

			Color currentColor = color;

			float secondaryScale = 1f;
			ItemSlot.GetItemLight(ref currentColor, ref secondaryScale, item);

			float itemScale = 1f;
			if (itemSourceRect.Width > 32 || itemSourceRect.Height > 32) {
				itemScale = (itemSourceRect.Width <= itemSourceRect.Height) ? (32f / itemSourceRect.Height) : (32f / itemSourceRect.Width);
			}

			itemScale *= scale;

			Vector2 itemPosition = rect.Center() - itemSourceRect.Size() * itemScale / 2f;
			Vector2 origin = itemSourceRect.Size() * (secondaryScale / 2f - 0.5f);

			float finalScale = itemScale * secondaryScale;

			if (ItemLoader.PreDrawInInventory(item, spriteBatch, itemPosition, itemSourceRect, item.GetAlpha(currentColor), item.GetColor(color), origin, finalScale)) {
				spriteBatch.Draw(itemTexture, itemPosition, itemSourceRect, item.GetAlpha(currentColor), 0f, origin, finalScale, SpriteEffects.None, 0f);
				if (item.color != Color.Transparent) {
					spriteBatch.Draw(itemTexture, itemPosition, itemSourceRect, item.GetColor(color), 0f, origin, finalScale, SpriteEffects.None, 0f);
				}
			}

			ItemLoader.PostDrawInInventory(item, spriteBatch, itemPosition, itemSourceRect, item.GetAlpha(currentColor), item.GetColor(color), origin, finalScale);
		}
	}
}
