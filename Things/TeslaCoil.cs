﻿using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
internal class TeslaCoil : NetworkBehaviour {

    public BoxCollider RangeBox;
    public GameObject SmallRing;
    public GameObject MediumRing;
    public GameObject LargeRing;

    [HideInInspector]
    private bool TeslaCoilOn = true;


    private Vector3 SmallRingStartPos;
    private Vector3 MediumRingStartPos;
    private Vector3 LargeRingStartPos;

    private List<PlayerControllerB> PlayerInRangeList = new();

    public void OnTriggerEnter(Collider other) {
        try {
            EyeSecAI EyeSecInRange = other.gameObject.GetComponent<EyeSecAI>();
            EyeSecInRange.BuffedByTeslaCoil = true;
        } catch {}       
        try {
            PlayerControllerB PlayerInRange = other.gameObject.GetComponent<PlayerControllerB>();
            
            if (!PlayerInRangeList.Contains(PlayerInRange) && PlayerInRange != null){
                WTOBase.LogToConsole($"Adding Player {PlayerInRange} to player in range list...");
                PlayerInRangeList.Add(PlayerInRange);
            }
        } catch {}
        try {
            RadarBoosterItem RadarBoosterInRange = other.gameObject.GetComponent<RadarBoosterItem>();
            RadarBoosterInRange.EnableRadarBooster(false);
        } catch { }
    }
    public void OnTriggerExit(Collider other) {
        try {
            EyeSecAI EyeSecInRange = other.gameObject.GetComponent<EyeSecAI>();
            other.gameObject.GetComponent<EyeSecAI>().BuffedByTeslaCoil = false;
        } catch { }
        try {
            PlayerControllerB PlayerInRange = other.gameObject.GetComponent<PlayerControllerB>();
            if (PlayerInRangeList.Contains(PlayerInRange) && PlayerInRange != null) {
                WTOBase.LogToConsole($"Removing Player {PlayerInRange} from player in range list...");
                PlayerInRangeList.Remove(PlayerInRange);
            }
        } catch { }
    }

    private void Start() {
        SmallRingStartPos = SmallRing.transform.localPosition;
        MediumRingStartPos = MediumRing.transform.localPosition;
        LargeRingStartPos = LargeRing.transform.localPosition;
        TeslaCoilOn = true;
    }

    private void Update() {
        //WTOBase.LogToConsole($"TESLA COIL STATUS: {TeslaCoilOn}");
        if (!TeslaCoilOn) {
            return;
        }
        SpinRings();
        //Wow this code sucks cock
        if(PlayerInRangeList.Count <= 0) {
            return;
        }
        WTOBase.LogToConsole($"Players in range: {PlayerInRangeList.Count}");
        foreach (PlayerControllerB Player in PlayerInRangeList) {
            if(Player.ItemSlots.Count() <= 0) {
                continue;
            }
            foreach (GrabbableObject HeldObject in Player.ItemSlots) {
                if(HeldObject is WalkieTalkie) {
                    WalkieTalkie NowYoureOnWalkies = HeldObject.GetComponent<WalkieTalkie>();
                    if(HeldObject.isBeingUsed == false) {
                        continue;
                    }
                    if (NowYoureOnWalkies.clientIsHoldingAndSpeakingIntoThis) {
                        NowYoureOnWalkies.BroadcastSFXFromWalkieTalkie(NowYoureOnWalkies.playerDieOnWalkieTalkieSFX, (int)NowYoureOnWalkies.playerHeldBy.playerClientId);
                    }
                    NowYoureOnWalkies.SwitchWalkieTalkieOn(false);
                }
                if(HeldObject is FlashlightItem) {
                    HeldObject.GetComponent<FlashlightItem>().SwitchFlashlight(false);
                }
                if(HeldObject is BoomboxItem) {
                    HeldObject.GetComponent<BoomboxItem>().StartMusic(false);
                }
                if(HeldObject is PatcherTool) {
                    HeldObject.GetComponent<PatcherTool>().DisablePatcherGun();
                }
                if(HeldObject is RadarBoosterItem) {
                    HeldObject.GetComponent<RadarBoosterItem>().EnableRadarBooster(false);
                }
            }
        }
    }

    private void SpinRings() {
        SmallRing.transform.Rotate(0, 0, -160 * Time.deltaTime);
        MediumRing.transform.Rotate(0, 0, -160 * Time.deltaTime);
        LargeRing.transform.Rotate(0, 0, -160 * Time.deltaTime);
    }
    private void DropRings() {
        SmallRing.transform.localPosition = new Vector3(0, 0, 0);
        MediumRing.transform.localPosition = new Vector3(0, 0, 0);
        LargeRing.transform.localPosition = new Vector3(0, 0, 0);
    }
    private void RaiseRings() {
        SmallRing.transform.localPosition = SmallRingStartPos;
        MediumRing.transform.localPosition = MediumRingStartPos;
        LargeRing.transform.localPosition = LargeRingStartPos;
    }

    public void RecieveToggleTeslaCoil(bool enabled) {
        WTOBase.LogToConsole($"Called toggle tesla coil with state: {enabled}");
        ToggleTeslaCoilServerRpc(enabled);
        ToggleTeslaCoil(enabled);
    }

    [ServerRpc]
    public void ToggleTeslaCoilServerRpc(bool enabled) {
        WTOBase.LogToConsole($"Toggling tesla coil to {enabled} serverRpc");
        ToggleTeslaCoilClientRpc(enabled);
    }

    [ClientRpc]
    public void ToggleTeslaCoilClientRpc(bool enabled) {
        WTOBase.LogToConsole($"Toggling tesla coil to {enabled} clientRpc");
        if(TeslaCoilOn != enabled) { 
            ToggleTeslaCoil(enabled);
        }
    }
    private void ToggleTeslaCoil(bool enabled) {
        TeslaCoilOn = enabled;
        WTOBase.LogToConsole($"TESLA COIL STATE: {TeslaCoilOn}");
        if (TeslaCoilOn) {
            RaiseRings();
        } else {
            DropRings();
        }
    }
}
