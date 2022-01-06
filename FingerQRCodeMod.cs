using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;
using HarmonyLib;
using NeosModLoader;
using ZXing;
using System.Net;
using System.IO;
using CodeX;

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
            Harmony.DEBUG = true;
            Harmony harmony = new Harmony("net.dfgHiatus.FingerQRCode");
            harmony.PatchAll();
        }

        // this is a private method
        [HarmonyPatch(typeof(PhotoCaptureManager), "TakePhoto")]
        public class FingerQRPatch
        {
            // asset provides the Uri of the photo we took with the finger photo
            public static bool Prefix(Slot rootSpace, int2 resolution, bool addTemporaryHolder, 
                PhotoCaptureManager __instance,
                ref float ____flash, SyncRef<Camera> ____camera, SyncRef<QuadMesh> ____quad, SyncRef<Slot> ____previewRoot)
            {
                Debug("Take Photo");
                // SyncRef<Camera> ____camera = Traverse.Create(__instance).Field("_camera").GetValue() as SyncRef<Camera>;
                // SyncRef<QuadMesh> ____quad = Traverse.Create(__instance).Field("_quad").GetValue() as SyncRef<QuadMesh>;
                // SyncRef<Slot> ____previewRoot = Traverse.Create(__instance).Field("_previewRoot").GetValue() as SyncRef<Slot>;

                ____flash = 1f; // Traverse.Create(__instance).Field("_flash").SetValue(1f); //
                __instance.PlayCaptureSound();
                Debug("Sound Played");
                Debug(____camera);
                Debug(____camera.Target.FieldOfView);
                Sync<float> fov = ____camera.Target.FieldOfView;
                Debug("FOV");
                float2 quadSize = ____quad.Target.Size;
                Debug("Transforms Stored");
                float3 position = ____previewRoot.Target.GlobalPosition;
                floatQ rotation = ____previewRoot.Target.GlobalRotation;
                float3 globalScale = ____previewRoot.Target.GlobalScale;
                float3 scale = globalScale * (quadSize.x / quadSize.Normalized.x);
                Debug("Transforms Stored");
                position = rootSpace.GlobalPointToLocal(in position);
                rotation = rootSpace.GlobalRotationToLocal(in rotation);
                scale = rootSpace.GlobalScaleToLocal(in scale);
                Debug("Before Task");
                __instance.StartTask((Func<Task>)(async () =>
                {
                    Debug("Start Task");
                    RenderSettings renderSettings = ____camera.Target.GetRenderSettings(resolution);
                    if (renderSettings.excludeObjects == null)
                        renderSettings.excludeObjects = new List<Slot>();
                    CommonTool.GetLaserRoots((IEnumerable<User>)__instance.World.AllUsers, renderSettings.excludeObjects);
                    Uri asset = await __instance.World.Render.RenderToAsset(renderSettings);
                    bool canSpawn = __instance.World.CanSpawnObjects();
                    if (addTemporaryHolder & canSpawn)
                    {
                        rootSpace = rootSpace.AddSlot("Temporary Holder");
                        rootSpace.PersistentSelf = false;
                        rootSpace.AttachComponent<DestroyWithoutChildren>();
                    }
                    Slot s;
                    if (canSpawn)
                    {
                        s = rootSpace.AddSlot("Photo");
                    }
                    else
                    {
                        s = __instance.LocalUserRoot.Slot.AddLocalSlot("Photo", true);
                        s.LocalPosition = new float3(y: -10000f);
                    }
                    Debug("Slots Generated");
                    StaticTexture2D staticTexture2D = s.AttachTexture(asset, wrapMode: TextureWrapMode.Clamp);
                    ImageImporter.SetupTextureProxyComponents(s, (IAssetProvider<Texture2D>)staticTexture2D, StereoLayout.None, ImageProjection.Perspective, true);
                    PhotoMetadata componentInChildren = s.GetComponentInChildren<PhotoMetadata>();
                    componentInChildren.CameraManufacturer.Value = "Neos";
                    componentInChildren.CameraModel.Value = __instance.GetType().Name;
                    componentInChildren.CameraFOV.Value = (float)fov;
                    s.AttachComponent<Grabbable>().Scalable.Value = true;
                    AttachedModel<QuadMesh, UnlitMaterial> attachedModel = s.AttachMesh<QuadMesh, UnlitMaterial>();
                    attachedModel.material.Texture.Target = (IAssetProvider<ITexture2D>)staticTexture2D;
                    attachedModel.material.Sidedness.Value = Sidedness.Double;
                    TextureSizeDriver textureSizeDriver = s.AttachComponent<TextureSizeDriver>();
                    textureSizeDriver.Texture.Target = (IAssetProvider<ITexture2D>)staticTexture2D;
                    textureSizeDriver.DriveMode.Value = TextureSizeDriver.Mode.Normalized;
                    textureSizeDriver.Target.Target = (IField<float2>)attachedModel.mesh.Size;
                    if (canSpawn)
                    {
                        s.LocalPosition = position;
                        s.LocalRotation = rotation;
                        s.LocalScale = scale;
                    }
                    BoxCollider boxCollider = s.AttachComponent<BoxCollider>();
                    boxCollider.Size.DriveFromXY((IField<float2>)attachedModel.mesh.Size);
                    boxCollider.Type.Value = ColliderType.NoCollision;
                    await componentInChildren.NotifyOfScreenshot();
                    if (canSpawn)
                    {
                        s = (Slot)null;
                    }
                    else
                    {
                        s.Destroy();
                        s = (Slot)null;
                    }

                    Debug("");
                    Debug("BEGIN URI DECODE PROCESS");
                    Debug("");
                    Debug("Payload URL: " + asset.ToString());
                    Debug("");

                    IBarcodeReader reader = new BarcodeReader();

                    // local://u03evnzmzkijq3353zc__g/_DBzvWshEEapMpdmSNsEPg.webp
                    LocalDB localDB = __instance.World.Engine.LocalDB;  
                    string linkString = asset.ToString();
                    
                    linkString = localDB.AssetStoragePath +"/"+Path.GetFileName(linkString);//linkString.Replace("local://" + localDB.MachineID.ToString(), localDB.AssetStoragePath);
                    
                    string tempFilePath1 = localDB.GetTempFilePath("png");
                    TextureEncoder.ConvertToPNG(linkString, tempFilePath1);
                    Debug(tempFilePath1);
                    
                    System.Drawing.Bitmap barcodeBitmap = (System.Drawing.Bitmap) Image.FromFile(tempFilePath1);

                    Debug(barcodeBitmap.Size.Width);
                    Debug(barcodeBitmap.Size.Height);

                    if (barcodeBitmap != null)
                    {
                        var result = reader.Decode(barcodeBitmap);

                        if (result != null)
                        {
                            try
                            {
                                if (Uri.IsWellFormedUriString(result.BarcodeFormat.ToString(), UriKind.RelativeOrAbsolute))
                                {
                                    Debug("URI Payload detected: " + result.BarcodeFormat.ToString());
                                    Uri qrCodeUri = new Uri(result.BarcodeFormat.ToString());
                                    Slot slot = Userspace.UserspaceWorld.AddSlot("Hyperlink Dialog");
                                    slot.AttachComponent<HyperlinkOpenDialog>().Setup(qrCodeUri, "Finger Photo QR Code");
                                    slot.PositionInFrontOfUser(new float3?(float3.Backward));
                                }
                                else
                                {
                                    Debug("Non-URI Payload detected: " + result.BarcodeFormat.ToString());
                                }
                            }
                            catch (Exception e)
                            {
                                Warn(e.ToString());
                            }
                        }
                    }

                    Debug("");
                    Debug("END URI DECODE PROCESS");
                    Debug("");
                }));

                return false;
            }
        }
    }
}
