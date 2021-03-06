﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;

namespace AutoBoss
{
    public static class BossEvents
    {
        private static readonly Random R = new Random();

        public static void StartBossBattle(BattleType type)
        {
            var bossLists = new List<Dictionary<int, int>>();

            switch (type)
            {
                case BattleType.Day:
                    bossLists = AutoBoss.config.DayBosses.Values.ToList();
                    break;
                case BattleType.Night:
                    bossLists = AutoBoss.config.NightBosses.Values.ToList();
                    break;
                case BattleType.Special:
                    bossLists = AutoBoss.config.SpecialBosses.Values.ToList();
                    break;
            }

            var bosses = bossLists[R.Next(0, bossLists.Count)];
            AutoBoss.bossList.Clear();
            AutoBoss.bossCounts.Clear();

            //bossPair.Key = npc type. bossPair.Value = amount of npc to spawn
            foreach (var bossPair in bosses)
            {
                var npc = TShock.Utils.GetNPCById(bossPair.Key);
                AutoBoss.bossCounts.Add(npc.FullName, bossPair.Value);

                for (var i = 0; i < bossPair.Value; i++)
                {
                    foreach (var region in AutoBoss.ActiveArenas)
                    {

                        var arenaX = region.Area.X + (region.Area.Width / 2);
                        var arenaY = region.Area.Y + (region.Area.Height / 2);

                        int spawnTileX;
                        int spawnTileY;
                        TShock.Utils.GetRandomClearTileWithInRange(arenaX, arenaY, 50, 20, out spawnTileX, out spawnTileY);

                        var npcid = NPC.NewNPC(spawnTileX * 16, spawnTileY * 16, bossPair.Key);
                        // This is for special slimes
                        Main.npc[npcid].SetDefaults(npc.netID);

                        AutoBoss.bossList.Add(npcid, bossPair.Key);
                    }
                }
            }

            var broadcast =
                //format: {0}x {1} where {0} is number of npc to spawn times number of arenas and {1} is npc name
                AutoBoss.bossCounts.Select(kvp => string.Format("{0}x {1}", kvp.Value * AutoBoss.ActiveArenas.Count,
                    kvp.Key)).ToList();

            TShock.Utils.Broadcast("Bosses selected: " + string.Join(", ", broadcast), Color.Crimson);

            if (AutoBoss.config.MinionToggles[type])
            {
                BossTimer.minionTime = R.Next(AutoBoss.config.MinionsSpawnTimer[0],
                    AutoBoss.config.MinionsSpawnTimer[1] + 1);

                BossTimer.minionSpawnCount = R.Next(AutoBoss.config.MinionSpawnCount[0],
                    AutoBoss.config.MinionSpawnCount[1] + 1);

                StartMinionSpawns(SelectMinions(type));
            }
        }

        public static void StartMinionSpawns(IEnumerable<int> types)
        {
            var minionCounts = new Dictionary<string, int>();

            foreach (var minion in types)
            {
                var npc = TShock.Utils.GetNPCById(minion);

                if (!minionCounts.ContainsKey(npc.FullName))
                    minionCounts.Add(npc.FullName, 1);
                else
                    minionCounts[npc.FullName]++;

                foreach (var region in AutoBoss.ActiveArenas)
                {
                    var arenaX = region.Area.X + (region.Area.Width / 2);
                    var arenaY = region.Area.Y + (region.Area.Height / 2);

                    TSPlayer.Server.SpawnNPC(minion, npc.FullName, 1, arenaX, arenaY, 50, 20);
                }
            }
            if (!AutoBoss.config.AnnounceMinions) return;

            var broadcast =
                minionCounts.Select(kvp => string.Format("{0}x {1}", kvp.Value * AutoBoss.ActiveArenas.Count,
                    kvp.Key)).ToList();

            TShock.Utils.Broadcast("Minions selected: " + string.Join(", ", broadcast), Color.Crimson);
        }


        public static IEnumerable<int> SelectMinions(BattleType type)
        {
            bool day = false, night = false, special = false;
            switch (type)
            {
                case BattleType.Day:
                    day = true;
                    break;
                case BattleType.Night:
                    night = true;
                    break;
                case BattleType.Special:
                    special = true;
                    break;
            }

            var ret = new List<int>();
            for (var i = 0; i < BossTimer.minionSpawnCount; i++)
            {
                if (day)
                    ret.AddCheck(AutoBoss.config.DayMinionList[R.Next(0, AutoBoss.config.DayMinionList.Count)]);
                if (night)
                    ret.AddCheck(AutoBoss.config.NightMinionList[R.Next(0, AutoBoss.config.NightMinionList.Count)]);
                if (special)
                    ret.AddCheck(AutoBoss.config.SpecialMinionList[R.Next(0, AutoBoss.config.SpecialMinionList.Count)]);
            }

            return ret;
        }
    }
}