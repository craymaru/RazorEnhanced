﻿using Assistant;
using System.Collections.Generic;
using System.Threading;

namespace RazorEnhanced
{
	public class Target
	{
		private int m_ptarget;

		internal static bool TargetMessage = false;

		public static bool HasTarget()
		{
			return Assistant.Targeting.HasTarget;
		}

		public static void WaitForTarget(int delay) // Delay in MS
		{
			int subdelay = delay;
			while (Assistant.Targeting.HasTarget == false)
			{
				Thread.Sleep(2);
				subdelay -= 2;
				if (subdelay <= 0)
					break;
			}
		}

		public static void TargetExecute(int serial)
		{
            if (!CheckHealPoisonTarg(serial))
			{
				Assistant.Serial s = serial;
				TargetInfo info = new TargetInfo();
				info.Type = 0;
				info.Flags = 0;
				info.Serial = s;

				if (s.IsItem)
				{
					Assistant.Item item = World.FindItem(s);
					if (item != null)
					{
						info.X = item.Position.X;
						info.Y = item.Position.Y;
						info.Z = item.Position.Z;
						info.Gfx = item.ItemID;
					}
				}
				else if (s.IsMobile)
				{
					Assistant.Mobile m = World.FindMobile(s);
					if (m != null)
					{
						info.X = m.Position.X;
						info.Y = m.Position.Y;
						info.Z = m.Position.Z;
						info.Gfx = m.Body;
					}
				}

			}
		}

		public static void TargetExecute(RazorEnhanced.Item item)
		{
			TargetExecute(item.Serial);
        }

		public static void TargetExecute(RazorEnhanced.Mobile mobile)
		{
			if (!CheckHealPoisonTarg(mobile.Serial))
			{
				TargetExecute(mobile.Serial);
			}
		}

		public static void TargetExecute(int x, int y, int z)
		{
			TargetInfo info = new TargetInfo();
			info.Type = 1;
			info.Flags = 0;
			info.Serial = 0;
			info.X = x;
			info.Y = y;
			info.Z = z;
			info.Gfx = 0;
			Assistant.Targeting.TargetByScript(info);
		}

		public static void Cancel()
		{
			Assistant.Targeting.CancelClientTargetByScript();
            Assistant.Targeting.CancelOneTimeTargetByScript();
        }

		public static void Self()
		{
			if (!CheckHealPoisonTarg(World.Player.Serial))
			{
				TargetExecute(World.Player.Serial);
			}
		}

		public static void SelfQueued()
		{
			Assistant.Targeting.TargetSelf(true);
		}

		public static void Last()
		{
			if (!CheckHealPoisonTarg(GetLast()))
				TargetExecute((int)Assistant.Targeting.GetLastTarger);
		}

		public static void LastQueued()
		{
			Assistant.Targeting.LastTarget(true);
		}

		public static int GetLast()
		{
			return (int)Assistant.Targeting.GetLastTarger;
		}

		public static int GetLastAttack()
		{
			return (int)Assistant.Targeting.LastAttack;
		}

		public static void SetLast(RazorEnhanced.Mobile mob)
		{
			Assistant.Mobile mobile = World.FindMobile(mob.Serial);
			Assistant.Targeting.SetLastTarget(mobile, 0);
		}

		public static void SetLast(int serial)
		{
			Assistant.Mobile mobile = World.FindMobile(serial);
			if (mobile != null)
				Assistant.Targeting.SetLastTarget(mobile, 0);
		}

		public static void ClearQueue()
		{
			Assistant.Targeting.ClearQueue();
		}

		public static void ClearLast()
		{
			Assistant.Targeting.ClearLast();
		}

		public static void ClearLastandQueue()
		{
			Assistant.Targeting.ClearQueue();
			Assistant.Targeting.ClearLast();
		}

		public int PromptTarget()
		{
			m_ptarget = -1;
			Misc.SendMessage("Select Item or Mobile");
			Targeting.OneTimeTarget(new Targeting.TargetResponseCallback(PromptTargetExex_Callback));

			while (m_ptarget == -1)
				Thread.Sleep(30);

			return m_ptarget;
		}

		private void PromptTargetExex_Callback(bool loc, Assistant.Serial serial, Assistant.Point3D pt, ushort itemid)
		{
			m_ptarget = serial;
		}

		// Check Poison 
		private static bool CheckHealPoisonTarg(Assistant.Serial ser)
		{
			if (World.Player == null)
				return false;

			if (!RazorEnhanced.Settings.General.ReadBool("BlockHealPoison"))
				return false;

			if (ser.IsMobile && (World.Player.LastSpell == Spell.ToID(1, 4) || World.Player.LastSpell == Spell.ToID(4, 5) || World.Player.LastSpell == 202))
			{
				Assistant.Mobile m = World.FindMobile(ser);

				if (m != null && m.Poisoned)
				{
					World.Player.SendMessage(MsgLevel.Warning, "Heal blocked, Target is poisoned!");
					return true;
				}
				else if (m != null && m.Blessed)
				{
					World.Player.SendMessage(MsgLevel.Warning, "Heal blocked, Target is mortelled!");
					return true;
				}
				return false;
			}
			else
				return false;
		}

		// Funzioni target per richiamare i target della gui

		private static string GetPlayerName(int s)
		{
			Assistant.Mobile mob = World.FindMobile(s);
			return mob.Name;
		}

		private static int[] m_NotoHues = new int[8]
		{
			1, // black		unused 0
			0x059, // blue		0x0059 1
			0x03F, // green		0x003F 2
			0x3B2, // greyish	0x03b2 3
			0x3B2, // grey		   "   4
			0x090, // orange		0x0090 5
			0x022, // red		0x0022 6
			0x035, // yellow		0x0035 7
		};

		private static int GetPlayerColor(Mobile mob)
		{
			return m_NotoHues[mob.Notoriety];
		}
		public static void SetLastTargetFromList(string targetid)
		{
			TargetGUI.TargetGUIObject targetdata = Settings.Target.TargetRead(targetid);
			if (targetdata != null)
			{
				Mobiles.Filter filter = targetdata.Filter;
				string selector = targetdata.Selector;

				List<Mobile> filterresult;
				filterresult = Mobiles.ApplyFilter(filter);

				Mobile mobtarget = Mobiles.Select(filterresult, selector);
				if (mobtarget != null)
				{
					if (RazorEnhanced.Settings.General.ReadBool("ShowHeadTargetCheckBox"))
					{
						if (Friend.IsFriend(mobtarget.Serial))
							Assistant.ClientCommunication.SendToClient(new UnicodeMessage(World.Player.Serial, World.Player.Body, MessageType.Regular, 63, 3, Language.CliLocName, World.Player.Name, "Target: [" + GetPlayerName(mobtarget.Serial) + "]"));
						else
							Assistant.ClientCommunication.SendToClient(new UnicodeMessage(World.Player.Serial, World.Player.Body, MessageType.Regular, GetPlayerColor(mobtarget), 3, Language.CliLocName, World.Player.Name, "Target: [" + GetPlayerName(mobtarget.Serial) + "]"));
					}

					if (RazorEnhanced.Settings.General.ReadBool("HighlightTargetCheckBox"))
						Mobiles.MessageNoWait(mobtarget.Serial, 10, "* Target *");
					RazorEnhanced.Target.SetLast(mobtarget);
				}
			}
		}

		public static void PerformTargetFromList(string targetid)
		{
			TargetGUI.TargetGUIObject targetdata = Settings.Target.TargetRead(targetid);
			if (targetdata != null)
			{
				Mobiles.Filter filter = targetdata.Filter;
				string selector = targetdata.Selector;

				List<Mobile> filterresult;
				filterresult = Mobiles.ApplyFilter(filter);

				Mobile mobtarget = Mobiles.Select(filterresult, selector);
				if (mobtarget != null)
				{
					if (RazorEnhanced.Settings.General.ReadBool("ShowHeadTargetCheckBox"))
					{
						if (Friend.IsFriend(mobtarget.Serial))
							Assistant.ClientCommunication.SendToClient(new UnicodeMessage(World.Player.Serial, World.Player.Body, MessageType.Regular, 63, 3, Language.CliLocName, World.Player.Name, "Target: [" + GetPlayerName(mobtarget.Serial) + "]"));
						else
							Assistant.ClientCommunication.SendToClient(new UnicodeMessage(World.Player.Serial, World.Player.Body, MessageType.Regular, GetPlayerColor(mobtarget), 3, Language.CliLocName, World.Player.Name, "Target: [" + GetPlayerName(mobtarget.Serial) + "]"));
					}

					if (RazorEnhanced.Settings.General.ReadBool("HighlightTargetCheckBox"))
						Mobiles.MessageNoWait(mobtarget.Serial, 10, "* Target *");

					RazorEnhanced.Target.TargetExecute(mobtarget.Serial);
					RazorEnhanced.Target.SetLast(mobtarget);
				}
			}
		}

		public static void AttackTargetFromList(string targetid)
		{
			TargetGUI.TargetGUIObject targetdata = Settings.Target.TargetRead(targetid);
			if (targetdata != null)
			{
				Mobiles.Filter filter = targetdata.Filter;
				string selector = targetdata.Selector;

				List<Mobile> filterresult;
				filterresult = Mobiles.ApplyFilter(filter);

				Mobile mobtarget = Mobiles.Select(filterresult, selector);
				if (mobtarget != null)
				{
					RazorEnhanced.Player.Attack(mobtarget.Serial);
					if (RazorEnhanced.Settings.General.ReadBool("HighlightTargetCheckBox"))
						Mobiles.MessageNoWait(mobtarget.Serial, 10, "* Target *");
				}
			}		
		}
	}
}