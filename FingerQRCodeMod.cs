using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;
using HarmonyLib;
using NeosModLoader;
using ZXing;
using System.Net;

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
            // asset provides the Uri of the photo we took with the finger photo
            public static bool Postfix(Uri asset)
            {
                Debug("");
                Debug("BEGIN URI DECODE PROCESS");
                Debug("");
                IBarcodeReader reader = new BarcodeReader();
                WebClient client = new WebClient();
                var image = Image.FromStream(client.OpenRead(asset));

                Bitmap barcodeBitmap = new Bitmap(image);

                if (barcodeBitmap != null)
                {
                    var result = reader.Decode(barcodeBitmap);

                    if (result != null)
                    {
                        try
                        { 
                            if (Uri.IsWellFormedUriString(result.BarcodeFormat.ToString(), UriKind.RelativeOrAbsolute))
                            {
                                Debug("Payload detected: " + result.BarcodeFormat.ToString());
                                Debug("");
                                Uri qrCodeUri = new Uri(result.BarcodeFormat.ToString());
                                Slot slot = Userspace.UserspaceWorld.AddSlot("Hyperlink Dialog");
                                slot.AttachComponent<HyperlinkOpenDialog>().Setup(qrCodeUri, "Finger Photo QR Code");
                                slot.PositionInFrontOfUser(new float3?(float3.Backward));
                            }
                        }
                        catch(Exception e)
                        {
                            Warn(e.ToString());
                            return true;
                        }
                        finally
                        {
                            client.Dispose();
                        }  
                    }
                }

                Debug("END URI DECODE PROCESS");
                Debug("");

                return true;
            }
        }
    }
}
