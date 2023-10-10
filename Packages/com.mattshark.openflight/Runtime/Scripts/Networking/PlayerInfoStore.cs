﻿
using OpenFlightVRC.UI;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace OpenFlightVRC.Net
{
    public class PlayerInfoStore : UdonSharpBehaviour
    {
        /// <summary> Current player on this object, null if none </summary>
        public VRCPlayerApi Owner;

        [UdonSynced]
        public float WingtipOffset = 0;
        [UdonSynced]
        public double d_spinetochest = 0;
        [UdonSynced]
        public bool isFlying = false;
        [UdonSynced]
        public bool isGliding = false;
        [UdonSynced]
        public bool isFlapping = false;
        [UdonSynced]
        public string flightMode = "Auto";
        [UdonSynced]
        public bool isContributer = false;

        [HideInInspector]
        public AvatarDetection avatarDetection;
        [HideInInspector]
        public WingFlightPlusGlide wingFlightPlusGlide;
        [HideInInspector]
        public OpenFlight openFlight;
        [HideInInspector]
        public ContributerDetection contributerDetection;

        void Start()
        {

        }

        void Update()
        {
            //check to make sure both scripts are available. If they arent, return
            if (avatarDetection == null || wingFlightPlusGlide == null || openFlight == null)
            {
                return;
            }

            //if the local player owns this object, update the values
            if (Networking.LocalPlayer == Owner)
            {
                WingtipOffset = avatarDetection.WingtipOffset;
                d_spinetochest = avatarDetection.d_spinetochest;
                isFlying = wingFlightPlusGlide.isFlying;
                isGliding = wingFlightPlusGlide.isGliding;
                isFlapping = wingFlightPlusGlide.isFlapping;
                flightMode = openFlight.flightMode;
                isContributer = contributerDetection.localPlayerIsContributer;
            }
        }

        public void _OnOwnerSet()
        {
            Logger.Log("Owner set to " + Owner.displayName, this);
        }

        public void _OnCleanup()
        {

        }
    }
}
