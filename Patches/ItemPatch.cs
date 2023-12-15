﻿using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;
using LethalLib.Modules;
using System.Collections.Generic;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches {
    internal class ItemPatch {

        private static List<SpawnableItemWithRarity> MoonScrap = new List<SpawnableItemWithRarity>();
        private class ItemData {
            private string ItemPath;
            private int Rarity;
            private Item Itemref;
            public ItemData(string path, int rarity) {
                ItemPath = path;
                Rarity = rarity;
            }
            public string GetItemPath(){  return ItemPath; }
            public int GetRarity(){ return Rarity; }
            public void SetItem(Item ItemToSet){ Itemref= ItemToSet; }
            public Item GetItem(){ return Itemref; }

        }
        //This array stores all our custom items
        private static ItemData[] ItemList = new ItemData[] { 
            new ItemData("Assets/CustomItems/AlienCrate.asset", 30),
            new ItemData("Assets/Customitems/FiveSixShovel.asset", 10),
            new ItemData("Assets/CustomItems/HandCrystal.asset", 30),
            new ItemData("Assets/CustomItems/OoblCorpse.asset", 5),
            new ItemData("Assets/CustomItems/StatueSmall.asset", 40),
            new ItemData("Assets/CustomItems/WandCorpse.asset", 5),
            new ItemData("Assets/CustomItems/WandFeed.asset", 20),
        };

        //Add our custom items
        public static void AddCustomItems() {
            WTOBase.LogToConsole("Adding custom items...");
            //Create our custom items
            Item NextItem;
            SpawnableItemWithRarity MoonScrapItem;

            foreach(ItemData MyCustomScrap in ItemList){
                NextItem = WTOBase.ItemAssetBundle.LoadAsset<Item>(MyCustomScrap.GetItemPath());               
                NetworkPrefabs.RegisterNetworkPrefab(NextItem.spawnPrefab);
                MyCustomScrap.SetItem(NextItem);
                Items.RegisterScrap(NextItem, MyCustomScrap.GetRarity(), Levels.LevelTypes.All);
                
                MoonScrapItem = new SpawnableItemWithRarity {
                    spawnableItem = NextItem,
                    rarity = MyCustomScrap.GetRarity()
                };
                MoonScrap.Add(MoonScrapItem);
            }
        }

        /* TODO: This signatures SHOULD be taking the list as a param, 
         * and the SOR Instance should probably be grabbed on awake so it 
         * doesn't need to be passed. 
         */
        public static void SetMoonItemList(bool UseDefaultList, SelectableLevel Moon, StartOfRound __instance) {
            if (UseDefaultList) {
                Moon.spawnableScrap = __instance.levels[2].spawnableScrap;
                return;
            }
            WTOBase.LogToConsole("Items in MoonScrap list: " + MoonScrap.Count.ToString());
            foreach (SpawnableItemWithRarity item in MoonScrap) {
                Moon.spawnableScrap.Add(item);
            }
        }


        //try to spawn the object 
        //[HarmonyPatch(typeof(StartOfRound), "Update")]
        //[HarmonyPostfix]
        private static void DebugSpawnItem(StartOfRound __instance) {
            if (Keyboard.current.f8Key.wasPressedThisFrame) {
                //var Crystal = UnityEngine.Object.Instantiate(ItemList[2].GetItem().spawnPrefab, __instance.localPlayerController.gameplayCamera.transform.position, Quaternion.identity);
                //Crystal.GetComponent<NetworkObject>().Spawn();
                WTOBase.LogToConsole("Custom item spawned...");
            }
        }

        [HarmonyPatch(typeof(RoundManager), "SpawnScrapInLevel")]
        [HarmonyPrefix]
        private static bool SetItemSpawnPoints(){
            /*Notably, if the first item in the source array is say, a TableTopSpawn,
             * This will mean items can only spawn on tabletops, and there tends to be only like 
             * 2 of those. Will probably cause issues
             * TODO: Ensure that the spawn we grab is a GeneralItemSpawn or even make it so we can
             * specify the spawn type for each item 
            */
            RandomScrapSpawn[] source = Object.FindObjectsOfType<RandomScrapSpawn>();
            foreach (SpawnableItemWithRarity item in MoonScrap) {
                item.spawnableItem.spawnPositionTypes[0] = source[0].spawnableItems;
            }
            return true;
        }

    }
}