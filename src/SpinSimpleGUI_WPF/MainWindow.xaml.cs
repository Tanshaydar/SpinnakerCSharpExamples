//=============================================================================
// Copyright (c) 2001-2023 FLIR Systems, Inc. All Rights Reserved.
//
// This software is the confidential and proprietary information of FLIR
// Integrated Imaging Solutions, Inc. ("Confidential Information"). You
// shall not disclose such Confidential Information and shall use it only in
// accordance with the terms of the license agreement you entered into
// with FLIR Integrated Imaging Solutions, Inc. (FLIR).
//
// FLIR MAKES NO REPRESENTATIONS OR WARRANTIES ABOUT THE SUITABILITY OF THE
// SOFTWARE, EITHER EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE, OR NON-INFRINGEMENT. FLIR SHALL NOT BE LIABLE FOR ANY DAMAGES
// SUFFERED BY LICENSEE AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
// THIS SOFTWARE OR ITS DERIVATIVES.
//=============================================================================

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SpinnakerNET;
using SpinnakerNET.GUI;
using SpinnakerNET.GUI.WPFControls;
using System.Windows.Controls;
using SpinnakerNET.GenApi;

namespace SpinSimpleGUI_WPF
{
    /// <summary>
    /// Interaction logic for FullImageWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        CameraSelectionControl camSelControl;
        PropertyGridControl gridControl;
        ImageDrawingControl drawControl;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Title = string.Format("FLIR IIS SpinSimpleGUI_WPF");

            camSelControl = new CameraSelectionControl();
            gridControl = new PropertyGridControl();
            drawControl = new ImageDrawingControl();

            Grid.SetRow(gridControl, 2);
            Grid.SetColumn(drawControl, 2);

            LayoutLeft.Children.Add(camSelControl);
            LayoutLeft.Children.Add(gridControl);

            LayoutRoot.Children.Add(drawControl);
        }

        /// <summary>
        /// Image Event Handler to process image data
        /// </summary>
        class ImageEventListener : ManagedImageEventHandler
        {
            public int imageCount;
            public String modelName;
            public String serialNumber;
            public ImageEventListener(IManagedCamera cam)
            {
                // Initialize image counter
                imageCount = 0;
                modelName = cam.DeviceModelName;
                serialNumber = cam.DeviceSerialNumber;

                Console.WriteLine("Initialized ImageEventListener for {0} {1}", modelName, serialNumber);
            }

            // This image event handler shows a basic example of reading image frameID and incrementing a local counter.
            // Please see ImageEvents_CSharp project for more examples of OnImageEvent usage.
            override protected void OnImageEvent(ManagedImage image)
            {
                imageCount++;
                if (!image.IsIncomplete)
                {
                    Console.WriteLine(
                        "{0} {1} Image event received. Total Number of images: {2}, Image FrameID: {3}",
                        modelName,
                        serialNumber,
                        imageCount,
                        image.FrameID);
                }
                else
                {
                    // In case of incomplete image, the frameID may be unreliable because it could have been lost.
                    Console.WriteLine(
                        "{0} {1} Image event received for incomplete image. Total Number of images: {2}",
                        modelName,
                        serialNumber,
                        imageCount);
                }
                image.Dispose();
                image = null;
            }
        }

        /// <summary>
        /// Event handler to Window Loaded event.
        /// </summary>
        /// <param name="sender">Window</param>
        /// <param name="e">RoutedEventArgs</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            camSelControl.OnDeviceDoubleClicked += ConditionalStreamingEvent;
            camSelControl.OnDeviceClicked += ConnectControls;
            camSelControl.RegisterStartStopContextMenuEvent(ConditionalStreamingEvent);
        }

#region CAMERA_CONTROL
        /// <summary>
        /// Event handler for device selection event from CameraSelectionControl
        /// </summary>
        /// <param name="sender">CameraSelectionControl</param>
        /// <param name="args">DeviceEventArgs</param>
        void ConnectControls(object sender, CameraSelectionWindow.DeviceEventArgs args)
        {
            // Check whether an Interface is selected
            if (args.IsCamera == false && args.Interface != null)
            {
                ResetWindowControl(); // Disconnect previous device
                gridControl.Connect(args.Interface.GetTLNodeMap());
            }
            // Check whether a System is selected
            else if (args.IsSystem == true && args.System != null)
            {
                ResetWindowControl(); // Disconnect previous device
                gridControl.Connect(args.System.GetTLNodeMap());
            }
            // Check if a camera is currently connected
            else if (drawControl.GetConnectedDevice() != null)
            {
                // Check if a new camera has been selected
                if (args.Camera.TLDevice.DeviceSerialNumber.Value !=
                    drawControl.GetConnectedDevice().TLDevice.DeviceSerialNumber.Value)
                {
                    ResetWindowControl(); // Disconnect previous device
                    SetCamera(args.Camera, false, args.Interface);
                }
                else // Same camera has been selected. Do nothing
                {
                    return;
                }
            }
            else // Connect a camera (Previous device was interface || Camera is first device connected)
            {
                SetCamera(args.Camera, false, args.Interface);
            }
        }

        /// <summary>
        /// Event handler for RegisterStartStopContextMenuEvent and OnDeviceDoubleClicked
        /// Note that ConnectControls is invoked prior to this function call
        /// </summary>
        /// <param name="sender">CameraSelectionControl</param>
        /// <param name="args">DeviceEventArgs</param>
        void ConditionalStreamingEvent(object sender, CameraSelectionWindow.DeviceEventArgs args)
        {
            if (drawControl.IsStreaming())
            {
                drawControl.Stop();
            }
            else
            {
                drawControl.Start();
            }
        }

        /// <summary>
        /// Disconnect and reinstate controls
        /// </summary>
        private void ResetWindowControl()
        {
            try
            {
                // Disconnect controls
                gridControl.Disconnect();
                drawControl.Disconnect();

                // Reinitialize controls
                gridControl = new PropertyGridControl();
                drawControl = new ImageDrawingControl();

                Grid.SetRow(gridControl, 2);
                Grid.SetColumn(drawControl, 2);

                LayoutLeft.Children.Add(gridControl);
                LayoutRoot.Children.Add(drawControl);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Connect ImageDrawingControl and PropertyGridControl with IManagedCamera
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="startStreaming">Boolean indicating whether to start streaming</param>
        /// <param name="parentInterface">Parent interface </param>
        void SetCamera(IManagedCamera cam, bool startStreaming = false, IManagedInterface parentInterface = null)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                cam.Init();
                gridControl.Connect(cam);

                // Set the Stream Buffer Handling Mode to Newest Only to optimize display delay
                INodeMap snodeMap = cam.GetTLStreamNodeMap();
                IEnum iHandlingMode = snodeMap.GetNode<IEnum>("StreamBufferHandlingMode");
                if (iHandlingMode != null && iHandlingMode.IsWritable && iHandlingMode.IsReadable)
                {
                    IEnumEntry iHandlingModeEntry = iHandlingMode.GetEntryByName("NewestOnly");
                    iHandlingMode.Value = iHandlingModeEntry.Value;
                }

                // Note this example does not include calling cam.UnregisterEventHandler(imageEventListener);
                // EventListeners should be unregistered at the end of a session.
                ImageEventListener imageEventListener = new ImageEventListener(cam);
                cam.RegisterEventHandler(imageEventListener);

                drawControl.Connect(cam, null, startStreaming, parentInterface);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("There was a problem connecting to IManagedCamera.\n{0}", ex.Message));
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
#endregion

#region WINDOW_EVENTS
        private void WindowClosing(object sender, EventArgs e)
        {
            try // Disconnect any connected component
            {
                gridControl.Disconnect();
                drawControl.Disconnect();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }

            Application.Current.Shutdown();
        }
#endregion
    }
}
