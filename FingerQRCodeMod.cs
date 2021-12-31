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
using System.Reflection;

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

        // this is a private method
        [HarmonyPatch(typeof(PhotoCaptureManager), "TakePhoto")]
        public class FingerQRPatch
        {
            // asset provides the Uri of the photo we took with the finger photo
            public static void Postfix(Slot rootSpace, int2 resolution, bool addTemporaryHolder, 
                ref Task __result, PhotoCaptureManager __instance)
            {

                // Reflection magic
                float _flash = (float)AccessTools.Field(__instance.GetType(), "_flash").GetValue(__instance);
                Sync<Camera> __camera = (Sync<Camera>)AccessTools.Field(__instance.GetType(), "_camera").GetValue(__instance);
                SyncRef<QuadMesh> __quad = (SyncRef<QuadMesh>)AccessTools.Field(__instance.GetType(), "_quad").GetValue(__instance);
                SyncRef<Slot> __previewRoot = (SyncRef<Slot>)AccessTools.Field(__instance.GetType(), "_previewRoot").GetValue(__instance);

                _flash = 1f;
                __instance.PlayCaptureSound();
                Sync<float> fov = __camera.Value.FieldOfView;
                float2 float2 = __quad.Target.Size;
                float3 position = __previewRoot.Target.GlobalPosition;
                floatQ rotation = __previewRoot.Target.GlobalRotation;
                float3 globalScale = __previewRoot.Target.GlobalScale;
                float3 scale = globalScale * (float2.x / float2.Normalized.x);
                position = rootSpace.GlobalPointToLocal(in position);
                rotation = rootSpace.GlobalRotationToLocal(in rotation);
                scale = rootSpace.GlobalScaleToLocal(in scale);
                __result = __instance.StartTask((Func<Task>)(async () =>
                {
                    RenderSettings renderSettings = __camera.Value.GetRenderSettings(resolution);
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
                            catch (Exception e)
                            {
                                Warn(e.ToString());
                            }
                            finally
                            {
                                client.Dispose();
                            }
                        }
                    }

                    Debug("END URI DECODE PROCESS");
                    Debug("");
                }));
            }
        }
    }
}
