using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

/*
 * Support for the Nuimo under Windows 10.
 * 
 * The code is similar to the approach from Senic with
 * their library for Linux/Raspbian here:
 * https://github.com/getsenic/nuimo-raspberrypi-demo
 * 
 * -----------------------------------------------------------------------
 * Copyright (c) 2016 Jakob Foos
 * Permission is hereby granted, free of charge, to any person 
 * obtaining a copy of this software and associated documentation 
 * files (the "Software"), to deal in the Software without restriction, 
 * including without limitation the rights to use, copy, modify, merge, 
 * publish, distribute, sublicense, and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice 
 * shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION 
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace NuimoController
{
    /// <summary>
    /// A controller for Nuimo that covers the Bluetooth connection.
    /// </summary>
    public class Nuimo
    {
        /// <summary>
        /// The sensor types of Nuimo.
        /// </summary>
        public enum Sensor
        {
            Battery,
            Button,
            Rotation,
            Swipe,
            Fly
        };

        // Bluetooth service UUIDs
        private Guid BatteryServiceGuid = new Guid("0000180f-0000-1000-8000-00805f9b34fb");
        private Guid DeviceInformationServiceGuid = new Guid("0000180A-0000-1000-8000-00805F9B34FB");
        private Guid SensorsServiceGuid = new Guid("f29b1525-cb19-40f3-be5c-7241ecb82fd2");
        private Guid LedMatrixServiceGuid = new Guid("f29b1523-cb19-40f3-be5c-7241ecb82fd1");

        // Bluetooth service UUIDs
        private Guid BatteryCharGuid = new Guid("00002a19-0000-1000-8000-00805f9b34fb");
        private Guid DeviceInformationCharGuid = new Guid("00002A29-0000-1000-8000-00805F9B34FB");
        private Guid ButtonCharGuid = new Guid("f29b1529-cb19-40f3-be5c-7241ecb82fd2");
        private Guid RotationCharGuid = new Guid("f29b1528-cb19-40f3-be5c-7241ecb82fd2");
        private Guid SwipeCharGuid = new Guid("f29b1527-cb19-40f3-be5c-7241ecb82fd2");
        private Guid FlyCharGuid = new Guid("f29b1526-cb19-40f3-be5c-7241ecb82fd2");
        private Guid LedMatrixCharGuid = new Guid("f29b1524-cb19-40f3-be5c-7241ecb82fd1");

        // In order like enum Characteristic
        private GattCharacteristic[] SensorsCharacteristics = new GattCharacteristic[5];
        private Dictionary<GattCharacteristic, Sensor> SensorMap = new Dictionary<GattCharacteristic, Sensor>();

        private GattCharacteristic Display;
        private GattCharacteristic Information;
        private BluetoothLEDevice Device = null;

        // Can be set by user to look for a differently named Nuimo
        // without providing an id.
        public String Name { get; set; } = "Nuimo";
        public String Id { get; private set; } = null;
        public int BatteryLevel { get; private set; } = 0;

        public Boolean Initialised { get; private set; } = false;
        public INuimoDelegate Delegate { get; set; } = null;

        /// <summary>
        /// Without an id, the Bluetooth device will be identified by name,
        /// default 'Nuimo'.
        /// </summary>
        public Nuimo()
        {

        }

        /// <summary>
        /// Looks for a Bluetooth device with the given id.
        /// </summary>
        /// <param name="id"></param>
        public Nuimo(string id)
        {
            if (id == null) return;

            // The internal id uses no colons for seperation.
            this.Id = id.Replace(":", "").ToLower();
        }

        /// <summary>
        /// Identifies Bluetooth device and sets up notification for sensors.
        /// </summary>
        /// <returns>True if successfull</returns>
        public async Task<bool> Init()
        {
            if (Initialised)
            {
                Debug.WriteLine("Already initialised.");
                return true;
            }

            // Happens only if the user doesn't give an Id and manually removes the default name.
            if (Name == null && Id == null)
            {
                throw new Exception("Without name and id an identification of the Bluetooth device is not possible.");
            }

            var message = "name '" + Name + "'";
            if (Id != null)
            {
                message = "id '" + Id + "'";
            }

            Debug.WriteLine("Initialising...");

            // Listing BLE devices does not seem to work, so it gathers all devices that use a battery service and picks
            // the correct device from them. 
            var devices = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(BatteryServiceGuid), null);

            Debug.WriteLine("Found " + devices.Count + " device(s) with battery status.");
            Debug.WriteLine("Looking for device with " + message + ".");

            foreach (var batteryDevice in devices)
            {
                // The internal device id is a long string, containing the Bluetooth id with colons.
                string id = batteryDevice.Id.ToLower();

                // Primarily look for the id, only if it's not given use the name.
                if ((Id != null && id.Contains(Id.ToLower())) || (Id == null && this.Name.Equals(batteryDevice.Name)))
                {

                    var s = await GattDeviceService.FromIdAsync(batteryDevice.Id);

                    // This would be unexpexted.
                    if (s == null)
                    {
                        Debug.WriteLine("Did not find Bluetooth service for device called '" + batteryDevice.Name + "'.");
                        continue;
                    }

                    Device = s.Device;
                    Debug.WriteLine("Identified device with id: " + Device.DeviceId);

                    SensorsCharacteristics[0] = s.GetCharacteristics(BatteryCharGuid)[0];
                    break;
                }
            }

            if (Device == null)
            {
                throw new Exception("Failed to find device with " + message + ".");
            }

            if (Name == null)
            {
                this.Name = Device.Name;
            }
            // Exact (empirically found) position of id
            this.Id = Device.DeviceId.Substring(14, 12);

            // Look for device information service/characteristic
            var service = Device.GetGattService(DeviceInformationServiceGuid);
            Information = service.GetCharacteristics(DeviceInformationCharGuid)[0];

            var infoResult = await Information.ReadValueAsync();
            byte[] bytes = new byte[infoResult.Value.Length];
            var buffer = DataReader.FromBuffer(infoResult.Value);
            // Why 5? Because it is only the String 'Senic'.
            var info = buffer.ReadString(5);
            Debug.WriteLine("Retrieved device information: " + info);


            // Get all the other characteristics:
            service = Device.GetGattService(SensorsServiceGuid);
            SensorsCharacteristics[1] = service.GetCharacteristics(ButtonCharGuid)[0];
            SensorsCharacteristics[2] = service.GetCharacteristics(RotationCharGuid)[0];
            SensorsCharacteristics[3] = service.GetCharacteristics(SwipeCharGuid)[0];
            SensorsCharacteristics[4] = service.GetCharacteristics(FlyCharGuid)[0];

            service = Device.GetGattService(LedMatrixServiceGuid);
            Display = service.GetCharacteristics(LedMatrixCharGuid)[0];

            Debug.WriteLine("Setting up listeners.");

            var gattResult = await SensorsCharacteristics[0].ReadValueAsync();
            BatteryLevel = DataReader.FromBuffer(gattResult.Value).ReadByte();

            for (var i = 0; i < 5; i++)
            {
                var characteristic = SensorsCharacteristics[i];
                characteristic.ValueChanged += BluetoothCallback;
                SensorMap.Add(characteristic, (Sensor)i);
                await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            }

            Debug.WriteLine("Done initialising.");
            Initialised = true;
            return Initialised;
        }

        public async void CleanUp()
        {
            if (!Initialised)
            {
                return;
            }

            Debug.WriteLine("Removing Bluetooth listeners.");
            for (var i = 0; i < 5; i++)
            {
                var characteristic = SensorsCharacteristics[i];
                try
                {
                    characteristic.ValueChanged -= BluetoothCallback;
                    await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                }
                catch (Exception exc)
                {
                    Debug.WriteLine("Cleaning up characteristic " + i + " failed (" + exc.ToString() + ").");
                }
            }

            Initialised = false;
            Debug.WriteLine("Done cleaning up.");

        }

        public async void Restart()
        {
            if (!Initialised)
            {
                await this.Init();
                return;
            }

            Debug.WriteLine("Setting Bluetooth listeners again.");
            for (var i = 0; i < 5; i++)
            {
                var characteristic = SensorsCharacteristics[i];
                characteristic.ValueChanged += BluetoothCallback;
                await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            }
            Debug.WriteLine("Done restarting.");
        }

        /// <summary>
        /// Computes the byte data from the given string with length 81, displays it for 2 seconds at maximum brightness.
        /// </summary>
        /// <param name="value">Each character stands for a single LED, from the top left. ' ' and '0' mean Off</param>
        public void LedDisplay(string value)
        {
            this.LedDisplay(value, 255, 20);
        }

        /// <summary>
        /// Computes the byte data from the given string with length 81, displays it for timeout seconds.
        /// </summary>
        /// <param name="value">Each character stands for a single LED, from the top left. ' ' and '0' mean Off</param>
        /// <param name="brightness"></param>
        /// <param name="timeout"></param>
        public void LedDisplay(string value, byte brightness, float timeout)
        {
            this.LedDisplay(value, 255, (Byte)((int)(timeout * 10)));
        }

        /// <summary>
        /// Computes the byte data from the given string with length 81, transmitts the timeout byte as it is.
        /// </summary>
        /// <param name="value">Each character stands for a single LED, from the top left. ' ' and '0' mean Off</param>
        /// <param name="brightness">From 0 to 255</param>
        /// <param name="timeoutByte">Tenth of seconds for the timeout</param>
        public void LedDisplay(string value, byte brightness, byte timeoutByte)
        {
            if (value.Length != 81)
            {
                throw new Exception("Wrong length of Nuimo display string: " + value.Length);
            }

            // Probably not the simplest way to compute the bytes
            byte[] data = new byte[13];
            for (var i = 0; i < 10; i++)
            {
                int b = 0;
                for (var j = 0; j < 8; j++)
                {
                    if (value[i * 8 + j] != ' ' && value[i * 8 + j] != '0')
                    {
                        b += 1 << j;
                    }
                }
                data[i] = (Byte)b;

            }

            data[10] = 0;
            if (value[80] != ' ' && value[80] != '0')
            {
                data[10] = 1;
            }
            data[11] = brightness;
            data[12] = timeoutByte;
            this.LedDisplayBytes(data);
        }

        /// <summary>
        /// Displays a symbol on the Nuimo. Fastest way.
        /// </summary>
        /// <param name="data">13 bytes, containing data, brightness and the timeout</param>
        public async void LedDisplayBytes(byte[] data)
        {
            var writer = new DataWriter();
            writer.WriteBytes(data);
            await Display.WriteValueAsync(writer.DetachBuffer());
        }

        /// <summary>
        /// Callback for the Bluetooth notifications. Do not call.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public void BluetoothCallback(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
        {
            if (Delegate == null)
            {
                return;
            }
            Sensor sensor = SensorMap[sender];

            byte[] bytes = new byte[eventArgs.CharacteristicValue.Length];
            DataReader.FromBuffer(eventArgs.CharacteristicValue).ReadBytes(bytes);

            if (sensor == Nuimo.Sensor.Button)
            {
                Delegate.OnButton(this, (ButtonAction)bytes[0]);
            }
            else if (sensor == Nuimo.Sensor.Swipe)
            {
                Delegate.OnSwipe(this, (SwipeDirection)bytes[0]);
            }
            else if (sensor == Nuimo.Sensor.Battery)
            {
                Delegate.OnBattery(this, bytes[0]);
                BatteryLevel = bytes[0];
            }
            else if (sensor == Nuimo.Sensor.Rotation)
            {
                var rot = BitConverter.ToInt16(bytes, 0);
                Delegate.OnRotation(this, rot);
            }
            else if (sensor == Nuimo.Sensor.Fly)
            {
                Delegate.OnFly(this, (FlyDirection)bytes[0], bytes[1]);
            }


        }
    }

}
