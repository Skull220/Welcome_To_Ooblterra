﻿using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Items;
internal class CursedEffigy : GrabbableObject {
    
    public List<AudioClip> AmbientSounds;
    public AudioSource AudioPlayer;
    public EnemyType TheMimic;

    private bool MimicSpawned;
    private PlayerControllerB previousPlayerHeldBy;

    public override void GrabItem() {
        base.GrabItem();
        SetOwningPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerHeldBy));
    }
    public override void DiscardItem() {
        base.DiscardItem();
        if (!previousPlayerHeldBy.isPlayerDead) {
            SetOwningPlayerServerRpc(-1);
        }
    }
    public override void Update() {
        base.Update();
        if (previousPlayerHeldBy == null) {
            return;
        }
        if (previousPlayerHeldBy.isPlayerDead) {
            if (!MimicSpawned && IsOwner) {
                WTOBase.LogToConsole($"Effigy knows that {previousPlayerHeldBy} is dead!");
                CreateMimicServerRpc(previousPlayerHeldBy.isInsideFactory, previousPlayerHeldBy.transform.position);
                MimicSpawned = true;
            }
            //Destroy(this);
        }
    }

    [ServerRpc]
    public void SetOwningPlayerServerRpc(int OwnerID) {
        SetOwningPlayerClientRpc(OwnerID);
    }

    [ClientRpc]
    public void SetOwningPlayerClientRpc(int OwnerID) {
        if (OwnerID == -1) {
            previousPlayerHeldBy = null;
            return;
        }
        previousPlayerHeldBy = StartOfRound.Instance.allPlayerScripts[OwnerID];
    }

    [ServerRpc(RequireOwnership = false)]
    public void CreateMimicServerRpc(bool inFactory, Vector3 playerPositionAtDeath) {
        if (previousPlayerHeldBy == null) {
            WTOBase.LogToConsole("Previousplayerheldby is null so the mask mimic could not be spawned");
            return;
        }
        WTOBase.LogToConsole($"Server creating mimic from Effigy. Previous Player: {previousPlayerHeldBy}");
        Vector3 MimicSpawnPos = RoundManager.Instance.GetNavMeshPosition(playerPositionAtDeath, default, 10f);
        if (!RoundManager.Instance.GotNavMeshPositionResult) {
            WTOBase.LogToConsole("No nav mesh found; no WTOMimic could be created");
            return;
        }
        const int MimicIndex = 12;
        TheMimic = StartOfRound.Instance.levels[8].Enemies[MimicIndex].enemyType;
        WTOBase.LogToConsole($"Mimic Found: {TheMimic != null}");

        NetworkObjectReference MimicNetObject = RoundManager.Instance.SpawnEnemyGameObject(MimicSpawnPos, 0, -1, TheMimic);

        if (MimicNetObject.TryGet(out var networkObject)) {
            Debug.Log("Got network object for WTOMimic");
            MaskedPlayerEnemy MimicScript = networkObject.GetComponent<MaskedPlayerEnemy>();
            MimicScript.mimickingPlayer = previousPlayerHeldBy;
            Material suitMaterial = SuitPatch.GhostPlayerSuit;
            MimicScript.rendererLOD0.material = suitMaterial;
            MimicScript.rendererLOD1.material = suitMaterial;
            MimicScript.rendererLOD2.material = suitMaterial;
            MimicScript.SetEnemyOutside(!inFactory);
            MimicScript.SetVisibilityOfMaskedEnemy();

            //This makes it such that the mimic has no visible mask :)
            MimicScript.maskTypes[0].SetActive(value: false);
            MimicScript.maskTypes[1].SetActive(value: false);
            MimicScript.maskTypeIndex = 0;

            previousPlayerHeldBy.redirectToEnemy = MimicScript;
            previousPlayerHeldBy.deadBody.DeactivateBody(setActive: false);
        }
        CreateMimicClientRpc(MimicNetObject, inFactory);
    }

    [ClientRpc]
    public void CreateMimicClientRpc(NetworkObjectReference netObjectRef, bool inFactory) {
        StartCoroutine(waitForMimicEnemySpawn(netObjectRef, inFactory));
    }

    private IEnumerator waitForMimicEnemySpawn(NetworkObjectReference netObjectRef, bool inFactory) {
        NetworkObject netObject = null;
        float startTime = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime > 20f || netObjectRef.TryGet(out netObject));
        if (previousPlayerHeldBy.deadBody == null) {
            startTime = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime > 20f || previousPlayerHeldBy.deadBody != null);
        }
        previousPlayerHeldBy.deadBody.DeactivateBody(setActive: false);
        if (netObject == null) {
            yield break;
        }
        Debug.Log("Got network object for WTOMimic enemy client");
        MaskedPlayerEnemy MimicReference = netObject.GetComponent<MaskedPlayerEnemy>();
        MimicReference.mimickingPlayer = previousPlayerHeldBy;
        Material suitMaterial = SuitPatch.GhostPlayerSuit;
        MimicReference.rendererLOD0.material = suitMaterial;
        MimicReference.rendererLOD1.material = suitMaterial;
        MimicReference.rendererLOD2.material = suitMaterial;
        MimicReference.SetEnemyOutside(!inFactory);
        MimicReference.SetVisibilityOfMaskedEnemy();

        //This makes it such that the mimic has no visible mask :)
        MimicReference.maskTypes[0].SetActive(value: false);
        MimicReference.maskTypes[1].SetActive(value: false);
        MimicReference.maskTypeIndex = 0;

        previousPlayerHeldBy.redirectToEnemy = MimicReference;
    }
}
