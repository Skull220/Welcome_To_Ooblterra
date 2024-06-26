﻿using System.Linq;
using Unity.Netcode;
using UnityEngine;
using LethalLib.Modules;
using HarmonyLib;
using Welcome_To_Ooblterra.Properties;
using static LethalLib.Modules.Dungeon;
using DunGen.Graph;
using LethalLib.Extras;
using GameNetcodeStuff;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;
using System;
using System.Collections.Generic;
using UnityEngine.AI;
using LethalLevelLoader;
using UnityEngine.Rendering;

namespace Welcome_To_Ooblterra.Patches;
internal class FactoryPatch {

    public static EntranceTeleport MainExit;
    public static EntranceTeleport FireExit;
    private static readonly AssetBundle FactoryBundle = WTOBase.FactoryAssetBundle;
    private const string DungeonPath = WTOBase.RootPath + "CustomDungeon/Data/";
    private const string BehaviorPath = WTOBase.RootPath + "CustomDungeon/Behaviors/";
    private const string SecurityPath = WTOBase.RootPath + "CustomDungeon/Security/";
    private const string DoorPath = WTOBase.RootPath + "CustomDungeon/Doors/";
    public static List<SpawnableMapObject> SecurityList = new();
    public static ExtendedDungeonFlow OoblDungeonFlow;

    //PATCHES 
    [HarmonyPatch(typeof(RoundManager), "SpawnMapObjects")]
    [HarmonyPrefix]
    private static bool WTOSpawnMapObjects(RoundManager __instance) {
        if (WTOBase.WTOForceHazards.Value == TiedToLabEnum.UseMoonDefault && __instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName) {
            return true;
            
        }
        if (DungeonManager.CurrentExtendedDungeonFlow != OoblDungeonFlow) {
            return true;
        }
        if (__instance.currentLevel.spawnableMapObjects == null || __instance.currentLevel.spawnableMapObjects.Length == 0) {
            return true;
        }
        System.Random MapHazardRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 587);
        __instance.mapPropsContainer = GameObject.FindGameObjectWithTag("MapPropsContainer");
        RandomMapObject[] AllRandomSpawnList = UnityEngine.Object.FindObjectsOfType<RandomMapObject>();
        List<string> ValidHazardList = WTOBase.CSVSeperatedStringList(WTOBase.WTOHazardList.Value);
        if(WTOBase.WTOForceHazards.Value != TiedToLabEnum.WTOOnly) {
            foreach(SpawnableMapObject MapObj in __instance.currentLevel.spawnableMapObjects) {
                ValidHazardList.Add(MapObj.prefabToSpawn.name.ToLower());
            }
        }
        for (int MapObjectIndex = 0; MapObjectIndex < __instance.currentLevel.spawnableMapObjects.Length; MapObjectIndex++) {
            //Check if the map object is allowed to spawn :)
            if (!ValidHazardList.Contains(__instance.currentLevel.spawnableMapObjects[MapObjectIndex].prefabToSpawn.name.ToLower())){
                WTOBase.LogToConsole($"Object {__instance.currentLevel.spawnableMapObjects[MapObjectIndex].prefabToSpawn.name.ToLower()} not found in valid spawn list!");
                continue;
            }
            List<RandomMapObject> ValidRandomSpawnList = new List<RandomMapObject>();
            int MapObjectsToSpawn = (int)__instance.currentLevel.spawnableMapObjects[MapObjectIndex].numberToSpawn.Evaluate((float)MapHazardRandom.NextDouble());
            WTOBase.WTOLogSource.LogInfo($"Attempting to spawn {__instance.currentLevel.spawnableMapObjects[MapObjectIndex].prefabToSpawn}; Quantity: {MapObjectsToSpawn}");
            if (__instance.increasedMapHazardSpawnRateIndex == MapObjectIndex) {
                MapObjectsToSpawn = Mathf.Min(MapObjectsToSpawn * 2, 150);
            }
            if (MapObjectsToSpawn <= 0) {
                continue;
            }
            for (int NextSpawnIndex = 0; NextSpawnIndex < AllRandomSpawnList.Length; NextSpawnIndex++) {
                string List = "";
                foreach (GameObject SpawnablePrefab in AllRandomSpawnList[NextSpawnIndex].spawnablePrefabs) {
                    if (Equals(SpawnablePrefab, __instance.currentLevel.spawnableMapObjects[MapObjectIndex].prefabToSpawn)) {

                        ValidRandomSpawnList.Add(AllRandomSpawnList[NextSpawnIndex]);
                    }
                    List += SpawnablePrefab+ ", ";
                }
                //WTOBase.WTOLogSource.LogInfo($"Spawn point {AllRandomSpawnList[NextSpawnIndex].name} contains: {List}");
                

            }
            //WTOBase.WTOLogSource.LogInfo($"Valid Spawns Found: {ValidRandomSpawnList.Count}");
            for (int i = 0; i < MapObjectsToSpawn; i++) {
                //lol
                if(ValidRandomSpawnList.Count <= 0) {
                    WTOBase.WTOLogSource.LogInfo($"Objects will not spawn; no valid random spots found!");
                    break;
                }
                RandomMapObject RandomSpawn = ValidRandomSpawnList[MapHazardRandom.Next(0, ValidRandomSpawnList.Count)];
                Vector3 SpawnPos = RandomSpawn.transform.position;
                GameObject NewHazard = UnityEngine.Object.Instantiate(__instance.currentLevel.spawnableMapObjects[MapObjectIndex].prefabToSpawn, SpawnPos, RandomSpawn.transform.rotation, __instance.mapPropsContainer.transform);
                NewHazard.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
                WTOBase.WTOLogSource.LogInfo($"Spawned new {NewHazard}"); 
                ValidRandomSpawnList.Remove(RandomSpawn);
            }
        }
        return false;
    }

    //METHODS
    public static void Start() {

        OoblDungeonFlow = WTOBase.ContextualLoadAsset<ExtendedDungeonFlow>(FactoryBundle, DungeonPath + "OoblLabExtendedDungeonFlow.asset");
        //OoblDungeonFlow.manualPlanetNameReferenceList.Clear();
        //OoblDungeonFlow.manualPlanetNameReferenceList.Add(new StringWithRarity("523 Ooblterra", 300));
        PatchedContent.RegisterExtendedDungeonFlow(OoblDungeonFlow);
        

        NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(FactoryBundle, BehaviorPath + "ChargedBattery.prefab"));
        NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(FactoryBundle, SecurityPath + "TeslaCoil.prefab"));
        NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(FactoryBundle, SecurityPath + "SpikeTrap.prefab"));
        NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(FactoryBundle, SecurityPath + "BabyLurkerEgg.prefab"));
        
    }
}
