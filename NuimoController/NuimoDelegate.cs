using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace NuimoController
{
    /// <summary>
    /// Callback listener for the Nuimo.
    /// </summary>
    public interface INuimoDelegate
    {
        void OnBattery(Nuimo nuimo, byte level);
        void OnButton(Nuimo nuimo, byte state);
        void OnRotation(Nuimo nuimo, short steps);
        void OnSwipe(Nuimo nuimo, byte direction);
        void OnFly(Nuimo nuimo, byte direction, byte speed);
    }

}
