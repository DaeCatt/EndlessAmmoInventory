using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ID;
using static Mono.Cecil.Cil.OpCodes;

namespace EndlessAmmoInventory.Hooks {
	class Hook {
		public static void PickAmmo(ILContext il) {
			ILLabel UseNormalAmmoLabel = il.DefineLabel();
			ILLabel UseEndlessAmmoLabel = il.DefineLabel();
			ILLabel CanShootLabel = il.DefineLabel();

			// Item item = new Item();
			// bool flag = false;
			ILCursor cursor = new ILCursor(il);
			if (!cursor.TryGotoNext(i => i.MatchLdcI4(0) && i.Next.MatchStloc(1)))
				throw new Exception("Could not locate flag = false");

			cursor.Index += 2;

			// bool useEndlessAmmoFirst = Delegate(this);
			cursor.Emit(Ldarg_0); // Player player
			cursor.EmitDelegate<Func<Player, bool>>((player) => {
				EndlessAmmoPlayer modPlayer = player.GetModPlayer<EndlessAmmoPlayer>();
				return modPlayer.useEndlessAmmoFirst;
			});

			cursor.Emit(Stloc_2);

			// if (useEndlessAmmoFirst == false) goto USE_NORMAL_AMMO;
			cursor.Emit(Ldloc_2);
			cursor.Emit(Brfalse, UseNormalAmmoLabel);

			// USE_ENDLESS_AMMO:
			cursor.MarkLabel(UseEndlessAmmoLabel);

			// item = Delegate(this, sItem);
			cursor.Emit(Ldarg_0); // Player player
			cursor.Emit(Ldarg_1); // Item weapon
			cursor.EmitDelegate<Func<Player, Item, Item>>((player, weapon) => {
				EndlessAmmoPlayer modPlayer = player.GetModPlayer<EndlessAmmoPlayer>();
				Item playerAmmo = modPlayer.GetItemForEndlessAmmoType(weapon.useAmmo);
				return playerAmmo;
			});
			cursor.Emit(Stloc_0);

			ILLabel IfItemIsAirLabel = il.DefineLabel();
			// if (item.type == 0) goto ITEM_IS_AIR;
			cursor.Emit(Ldloc_0);
			cursor.Emit(Ldfld, typeof(Item).GetField(nameof(Item.type)));
			cursor.Emit(Brfalse, IfItemIsAirLabel);
			// canShoot = true;
			cursor.Emit(Ldarg, 4);
			cursor.Emit(Ldc_I4_1);
			cursor.Emit(Stind_I1);
			// dontConsume = true;
			cursor.Emit(Ldc_I4_1);
			cursor.Emit(Starg, 7);

			// goto CAN_SHOOT;
			cursor.Emit(Br, CanShootLabel);

			// ITEM_IS_AIR:
			cursor.MarkLabel(IfItemIsAirLabel);

			// if (useEndlessAmmoFirst == false) goto CAN_SHOOT;
			cursor.Emit(Ldloc_2);
			cursor.Emit(Brfalse, CanShootLabel);

			// USE_NORMAL_AMMO:
			cursor.MarkLabel(UseNormalAmmoLabel);

			// ..
			if (!cursor.TryGotoNext(i => i.MatchLdarg(4) && i.Next.MatchLdindU1()))
				throw new Exception("Could not locate canShoot == false conditional.");

			// if (useEndlessAmmoFirst) goto CAN_SHOOT;
			ILLabel SkipAheadLabel = il.DefineLabel();
			cursor.Emit(Ldloc_2);
			cursor.Emit(Brtrue, CanShootLabel);

			// if (item.type == 0) goto USE_ENDLESS_AMMO;
			cursor.Emit(Ldloc_0);
			cursor.Emit(Ldfld, typeof(Item).GetField(nameof(Item.type)));
			cursor.Emit(Brfalse, UseEndlessAmmoLabel);

			// CAN_SHOOT:
			cursor.MarkLabel(CanShootLabel);

			// if (!canShoot) return;
			// ...
		}

		public static void HasAmmo(ILContext il) {
			ILCursor cursor = new ILCursor(il);
			cursor.Emit(Ldarg_0);
			cursor.Emit(Ldarg_1);
			cursor.EmitDelegate<Func<Player, Item, bool>>((player, item) => {
				EndlessAmmoPlayer modPlayer = player.GetModPlayer<EndlessAmmoPlayer>();
				return modPlayer.GetItemForEndlessAmmoType(item.useAmmo).type > ItemID.None;
			});

			ILLabel label = il.DefineLabel();
			cursor.Emit(Brfalse, label);
			cursor.Emit(Ldc_I4_1);
			cursor.Emit(Ret);
			cursor.MarkLabel(label);
		}

		public static void ItemSlotDraw(ILContext il) {
			ILCursor cursor = new ILCursor(il);
			if (!cursor.TryGotoNext(i => i.MatchLdcI4(-1) && i.Next.Next.MatchLdarg(2) && i.Next.Next.Next.MatchLdcI4(13)))
				throw new Exception("Could not locate int [x] = -1; if (context == 13) in IL.Terraria.UI.ItemSlot.Draw");

			byte indx = (byte) ((VariableDefinition) cursor.Next.Next.Operand).Index;
			System.Reflection.MethodInfo callTo = typeof(Int32).GetMethod(nameof(Int32.ToString), new Type[] { });

			if (!cursor.TryGotoNext(i => i.MatchLdloca(indx) && i.Next.MatchCall(callTo)))
				throw new Exception("Could not locate call to ChatManager.DrawColorCodedStringWithShadow");

			cursor.Index += 2;
			cursor.Emit(Ldloc_1);
			cursor.EmitDelegate<Func<string, Item, string>>((ammoCount, weapon) => {
				EndlessAmmoPlayer modPlayer = Main.LocalPlayer.GetModPlayer<EndlessAmmoPlayer>();
				Item ammo = modPlayer.GetItemForEndlessAmmoType(weapon.useAmmo);
				if (modPlayer.useEndlessAmmoFirst)
					return ammo.type > ItemID.None ? "Inf" : ammoCount;

				if (ammoCount != "0")
					return ammoCount;

				return ammo.type > ItemID.None ? "Inf" : "0";
			});

			/* Resize text?
			cursor.Emit(Stloc_S, indx);
			cursor.Emit(Ldloc_S, indx);

			if (cursor.TryGotoNext(i => i.MatchLdcR4(0.8f))) {
				cursor.Remove();
				cursor.Emit(Ldloc_S, indx);
				cursor.EmitDelegate<Func<string, float>>((count) => {

					if (count != "∞")
						return 0.8f;

					return 1.6f;
				});
			}
			*/
		}
	}
}
