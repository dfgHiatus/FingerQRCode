using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;

namespace FingerQRCode
{
    public class FingerQRCodeMod : NeosMod
    {
        public override string Name => "FingerQRCode";
        public override string Author => "dfgHiatus";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/dfgHiatus/FingerQRCode/";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("net.dfgHiatus.FingerQRCode");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(PhotoCaptureManager), "TakePhoto")]
        public class FingerQRPatch
        {
            public static void Prefix()
            {

            }
        }
}
