# nuimo-windows-unofficial

Unofficial support for Nuimo from Senic for Windows 10.
I didn't want to wait for Senic to support Windows, they aim at July 2016.
So here is a early solution I wrote myself.

## Disclaimer
Limitations:
 - It runs only on Windows 10 (older versions not supported)
 - It can be included only in Windows Universal Apps written in C#
 - Native Windows support for Bluetooth Low Energy (BLE) is limited, the user has to pair with Nuimo via the Windows system before.
 - Over time the connection may grow instable
 - My C# and Windows programming skill are limited, there might be unexpected code
 - This is probably not a final version

## Nuimo Demo App
Next to the Nuimo Controller there is a Demo, which is tested to work on Windows 10 Desktop systems. 
It can be used to toy around with the controller and is not very complex.

## How to include the Nuimo Controller
You can simply include the DLL from the Nuimo Controller in your Universal App. As far as I know, it will only work with apps written in C#. Distinguish between the Release and the Debug version of the DLL. The latter includes more debug output. If you are going to build the DLL yourself anyway, you could also include the whole Nuimo Controller project.
Then, most important, add the Bluetooth capability in your Package.appxmanifest.
The rest is simple code:
```csharp
Nuimo nuimo = new Nuimo(id) // or don't supply an id. The controller might find Nuimo anyway

bool success = await nuimo.Init();

nuimo.Delegate = MyDelegate; // instance of INuimoDelegate, a simple listener interface
```
And that's it. For further documentation, please refer to the code. Especially Nuimo.cs is well commented.
