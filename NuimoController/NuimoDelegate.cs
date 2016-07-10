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
        /// <summary>
        /// Called upon a battery update
        /// </summary>
        /// <param name="nuimo">Nuimo that sent the event</param>
        /// <param name="level">battery level (in %): 0..100</param>
        void OnBattery(Nuimo nuimo, short level);

        /// <summary>
        /// Called upon a button press
        /// </summary>
        /// <param name="nuimo">Nuimo that sent the event</param>
        /// <param name="action">change to the button</param>
        void OnButton(Nuimo nuimo, ButtonAction action);

        /// <summary>
        /// Called upon a rotation event
        /// </summary>
        /// <param name="nuimo">Nuimo that sent the event</param>
        /// <param name="action">delta steps (Clockwise: > 0, Counterclockwise: < 0)</param>
        /// <remarks>this is a relative amount, you need to keep track of the absolute amount yourself</remarks>
        void OnRotation(Nuimo nuimo, short steps);

        /// <summary>
        /// Called upon a swipe event
        /// </summary>
        /// <param name="nuimo">Nuimo that sent the event</param>
        /// <param name="direction">swipe direction</param>
        void OnSwipe(Nuimo nuimo, SwipeDirection direction);

        /// <summary>
        /// Called upon a fly/hover event
        /// </summary>
        /// <param name="nuimo">Nuimo that sent the event</param>
        /// <param name="direction">fly direction</param>
        /// <param name="distanceFromSensor">this is only needed for the UpDown event. Values range from  0..255</param>
        /// <remarks>this is a relative amount, you need to keep track of the absolute amount yourself</remarks>
        void OnFly(Nuimo nuimo, FlyDirection direction, short distanceFromSensor);
    }

    
}
