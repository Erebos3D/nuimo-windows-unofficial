using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
using NuimoController;

/*
 * A simple demonstration for the use of the Nuimo Windows Controller.
 * Do not forget to add the Bluetooth capability in the Package.appxmanifest.
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



namespace NuimoDemoApp
{
    public sealed partial class MainPage : Page, INuimoDelegate
    {
        public Nuimo nuimo;
        public int angle = 0;

        public string symbol1 = ("******** " +
                                 "*********" +
                                 "       **" +
                                 "       **" +
                                 "       **" +
                                 " *     **" +
                                 " **   ** " +
                                 "  ** **  " +
                                 "   ***   ");
        public string symbol2 = ("*********" +
                                 "*********" +
                                 "**       " +
                                 "**       " +
                                 "******   " +
                                 "*****    " +
                                 "**       " +
                                 "**       " +
                                 "**       ");
        public string symbol3 = ("* * * * *" +
                                 " * * * * " +
                                 "* * * * *" +
                                 " * * * * " +
                                 "* * * * *" +
                                 " * * * * " +
                                 "* * * * *" +
                                 " * * * * " +
                                 "* * * * *");

        //public byte[] defaultSymbol = { (Byte)'U', (Byte)'U', (Byte)'U', (Byte)'U', (Byte)'U', (Byte)'U', (Byte)'U', (Byte)'U', (Byte)'U', (Byte)'U', (Byte)0x01, (Byte)0xFF, (Byte)0x14 };

        public int counter = 0;
        public DispatcherTimer swipeTimer = new DispatcherTimer();

        public MainPage()
        {
            // If no Id is supplied, it will search for a Bluetooth device called nuimo.Name (default is 'Nuimo', but you can change that).
            nuimo = new Nuimo("d3b48a8b91ac");
            // e.g
            // nuimo.Name = "MyNewNuimoName";
            // Sadly, different Nuimo names are AFAIK not supported yet on the hardware.

            this.InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

        }

        // I don't know if this helps at all. Windows app life cycle is strange.
        public void SuspendNuimo()
        {
            batteryOutput.Text = "Suspended";
            Debug.WriteLine("Suspending Nuimo...");
            nuimo.CleanUp();
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Sometimes the connection just breaks, at least for the notification characteristics.
            // The display control still works. Restarting Nuimo fixes exactly that.
            if (nuimo.Initialised)
            {
                nuimo.Restart();
                return;
            }
            try
            {
                greetingOutput.Text = "Starting...";

                // Set the listener
                nuimo.Delegate = this;

                // Initialise Nuimo.
                // DO NOT FORGET THE BLUETOOTH CAPABILITY!
                // Or else you will get no working Nuimo,
                // without as much as an exception.
                await nuimo.Init();


                swipeTimer.Interval = new TimeSpan(0, 0, 1);
                swipeTimer.Stop();
                swipeTimer.Tick += updateSwipe;
                greetingOutput.Text = "Name: " + nuimo.Name + "\nId: " + nuimo.Id;
                batteryOutput.Text = "Battery: " + nuimo.BatteryLevel + "%";
                startButton.Content = "Restart Nuimo";
            }
            catch (Exception exc)
            {
                greetingOutput.Text = "Initialising Nuimo failed :(\n" + exc.ToString();
            }
        }

        public void updateSwipe(object sender, object e)
        {
            swipeTimer.Stop();
            swipeOutput.Text = "";
        }

        private void DisplayButton_Click(object sender, RoutedEventArgs e)
        {
            counter++;
            if (counter % 3 == 0)
                nuimo.LedDisplay(symbol3, 50, 10);
            else if (counter % 3 == 1)
                nuimo.LedDisplay(symbol1);
            else
                nuimo.LedDisplay(symbol2, 100, 22.0f);
        }


        // -------------- INuimoDelegate Methods ---------------------------//

        public async void OnBattery(Nuimo nuimo, short level)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                batteryOutput.Text = "Battery: " + level + "%";
            });
        }

        public async void OnButton(Nuimo nuimo, ButtonAction state)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                buttonOutput.Text = "Button state: " + state;
            });
        }

        public async void OnRotation(Nuimo nuimo, short steps)
        {
            angle += steps;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                rotOutput.Text = "Angle: " + angle;
            });
        }

        public async void OnSwipe(Nuimo nuimo, SwipeDirection direction)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                swipeTimer.Stop();
                swipeOutput.Text = "Swipe " + direction.ToString();
                swipeTimer.Start();
            });
        }

        public async void OnFly(Nuimo nuimo, FlyDirection direction, short distance)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                swipeTimer.Stop();
                if (direction == FlyDirection.UpDown) // Means close/far
                {
                    swipeOutput.Text = "Hover distance: " + distance;
                }
                else
                {
                    swipeOutput.Text = "Fly " + direction.ToString();
                }
                swipeTimer.Start();
            });
        }
    }
}
