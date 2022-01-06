using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;
using HarmonyLib;
using NeosModLoader;
using System.IO;
using CodeX;
using MessagingToolkit.QRCode.Codec;
using MessagingToolkit.QRCode.Codec.Data;
using System.Drawing;
using System.Windows.Forms;

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
                // SyncRef<Camera> ____camera = Traverse.Create(__instance).Field("_camera").GetValue() as SyncRef<Camera>;
                // SyncRef<QuadMesh> ____quad = Traverse.Create(__instance).Field("_quad").GetValue() as SyncRef<QuadMesh>;
                // SyncRef<Slot> ____previewRoot = Traverse.Create(__instance).Field("_previewRoot").GetValue() as SyncRef<Slot>;
                // Traverse.Create(__instance).Field("_flash").SetValue(1f);

                ____flash = 1f; 
                __instance.PlayCaptureSound();
                Sync<float> fov = ____camera.Target.FieldOfView;
                float2 quadSize = ____quad.Target.Size;
                float3 position = ____previewRoot.Target.GlobalPosition;
                floatQ rotation = ____previewRoot.Target.GlobalRotation;
                float3 globalScale = ____previewRoot.Target.GlobalScale;
                float3 scale = globalScale * (quadSize.x / quadSize.Normalized.x);
                position = rootSpace.GlobalPointToLocal(in position);
                rotation = rootSpace.GlobalRotationToLocal(in rotation);
                scale = rootSpace.GlobalScaleToLocal(in scale);

                __instance.StartTask((Func<Task>)(async () =>
                {
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
                    Debug("Local URL: " + asset.ToString());

                    LocalDB localDB = __instance.World.Engine.LocalDB;

                    // local://u03evnzmzkijq3353zc__g/_DBzvWshEEapMpdmSNsEPg.webp
                    string linkString = asset.ToString();

                    // %appdata..._DBzvWshEEapMpdmSNsEPg.webp
                    linkString = localDB.AssetStoragePath +"/"+Path.GetFileName(linkString);
                    
                    string tempFilePath1 = localDB.GetTempFilePath("png");
                    TextureEncoder.ConvertToPNG(linkString, tempFilePath1);
                    // %appdata..._DBzvWshEEapMpdmSNsEPg.png

                    if (File.Exists(tempFilePath1))
                    {
                        QRCodeDecoder decoder = new QRCodeDecoder();
                        string QRString = null;

                        try
                        {
                            QRString = decoder.Decode(new QRCodeBitmapImage(Image.FromFile(tempFilePath1) as System.Drawing.Bitmap));
                        }
                        catch (Exception e)
                        {
                            Warn("Error in URI decode: " + e.Message);
                        }

                        Debug("Raw Content: " + QRString);

                        // Need two copies of the end due to RunSync
                        if (QRString != null) 
                        {
                            if (Uri.IsWellFormedUriString(QRString, UriKind.RelativeOrAbsolute))
                            {
                                Userspace.UserspaceWorld.RunSynchronously( delegate
                                {
                                    Debug("URI Payload detected.");
                                    Slot slot = Userspace.UserspaceWorld.AddSlot("Hyperlink Dialog");
                                    slot.AttachComponent<HyperlinkOpenDialog>().Setup(new Uri(QRString), "Finger Photo QR Code");
                                    slot.PositionInFrontOfUser(new float3?(float3.Backward));
                                    QRString = null;
                                    Debug("");
                                    Debug("END URI DECODE PROCESS");
                                    Debug("");
                                });
                            }
                            else
                            {
                                Debug("Non-URI Payload detected. Copying to clipboard");
                                Clipboard.SetText(QRString);
                                QRString = null;
                                Debug("");
                                Debug("END URI DECODE PROCESS");
                                Debug("");
                            }
                        }
                        else
                        {
                            Warn("Error in Payload decode");
                        }
                    }
                }));

                return false;
            }
        }
    }
}
