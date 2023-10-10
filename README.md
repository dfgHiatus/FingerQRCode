# FingerQRCode
![Screenshot](https://github.com/dfgHiatus/FingerQRCode/blob/master/QRCode.PNG)
A mod to extend URL-QR Code reader functionality to Resonite's finger photo gesture.

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place FingerPhotoQR.dll into your `rml_mods` folder, and MessagingToolkit.QRCode.dll into your Resonite base directory (the folder above `rml_mods`). This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
3. Start the game. If you want to verify that the mod is working you can check your Resonite logs.

## Notes: 
- Finger photos now take more time to process, on average 5 or 6 seconds 
- For the best QR reading, stand 2-3 meters away from the QR code when scanning, with the image as flat as possible for the camera. The QR code should take up no more than 1/3 of your picture, and be well-lit enough to read.
- Currently does not work with non-URL payloads
- Longer URL's (>25 characters, this number is flexible) are typically harder to decode
- QR Code ECC Level (M, L, Q, etc.) does not apear to change the readability of the QR Code. Keep this at L unless needs be
