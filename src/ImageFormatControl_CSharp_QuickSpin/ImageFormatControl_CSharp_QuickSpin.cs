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
/**
 *  @example ImageFormatControl_CSharp_QuickSpin.cs
 *
 *  @brief ImageFormatControl_CSharp.cs shows how to apply custom image settings to
 *	the camera. It relies on information provided in the Enumeration_CSharp,
 *	Acquisition_CSharp, and NodeMapInfo_CSharp examples.
 *
 *	This example demonstrates setting minimums to offsets, X and Y, and maximums
 *	to width and height. It also shows the setting of a new pixel format, which
 *	is slightly more complex because it is an enumeration type node.
 *
 *	Following this, we suggest familiarizing yourself with the Exposure_CSharp example
 *	if you haven't already. Exposure_CSharp is another example on camera customization
 *	that is shorter and simpler than many of the others. Once comfortable with
 *	Exposure_CSharp and ImageFormatControl_CSharp, we suggest checking out any of the longer,
 *	more complicated examples related to camera configuration: ChunkData_CSharp,
 *	LookupTable_CSharp, Sequencer_CSharp, or Trigger_CSharp.
 *	
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics
 */

using System;
using System.Collections.Generic;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace ImageFormatControl_CSharp_QuickSpin
{
    class Program
    {
        // This function configures a number of settings on the camera including offsets
        // X and Y, width, height, and pixel format. These settings will be applied when
        // an image is acquired.
        int ConfigureCustomImageSettings(IManagedCamera cam)
        {
            int result = 0;

            Console.WriteLine("\n*** CONFIGURING CUSTOM IMAGE SETTINGS ***\n");

            try
            {
                //
                // Apply mono 8 pixel format
                //
                // *** NOTES ***
                // In QuickSpin, Enumeration nodes are as easy to set as other node
                // types. This is because enum values representing each entry node
                // are added to the API.
                //
                if (cam.PixelFormat != null && cam.PixelFormat.IsWritable)
                {
                    cam.PixelFormat.Value = PixelFormatEnums.Mono8.ToString();

                    Console.WriteLine("Pixel format set to {0}...", cam.PixelFormat.DisplayName);
                }
                else
                {
                    Console.WriteLine("Height not available...", cam.Height.Value);
                    result = -1;
                }

                //
                // Apply minimum to offset X
                //
                // *** NOTES ***
                // Numeric nodes have both a minimum and maximum. A minimum is retrieved
                // with the method GetMin(). Sometimes it can be important to check
                // minimums to ensure that your desired value is within range.
                //
                if (cam.OffsetX != null && cam.OffsetX.IsWritable && cam.OffsetX.IsReadable)
                {
                    cam.OffsetX.Value = cam.OffsetX.Min;

                    Console.WriteLine("Offset X set to {0}...", cam.OffsetX.Value);
                }
                else
                {
                    Console.WriteLine("Offset Y not available...");
                    result = -1;
                }

                //
                // Apply minimum to offset Y
                //
                // *** NOTES ***
                // It is often desirable to check the increment as well. The increment
                // is a number of which a desired value must be a multiple. Certain
                // nodes, such as those corresponding to offsets X and Y, have an
                // increment of 1, which basically means that any value within range
                // is appropriate. The increment is retrieved with the method GetInc().
                //
                if (cam.OffsetY != null && cam.OffsetY.IsWritable && cam.OffsetY.IsReadable)
                {
                    cam.OffsetY.Value = cam.OffsetY.Min;

                    Console.WriteLine("Offset Y set to {0}...", cam.OffsetY.Value);
                }
                else
                {
                    Console.WriteLine("Offset Y not available...");
                    result = -1;
                }

                //
                // Set maximum width
                //
                // *** NOTES ***
                // Other nodes, such as those corresponding to image width and height,
                // might have an increment other than 1. In these cases, it can be
                // important to check that the desired value is a multiple of the
                // increment. However, as these values are being set to the maximum,
                // there is no reason to check against the increment.
                //
                if (cam.Width != null && cam.Width.IsWritable && cam.Width.IsReadable)
                {
                    cam.Width.Value = cam.Width.Max;

                    Console.WriteLine("Width set to {0}...", cam.Width.Value);
                }
                else
                {
                    Console.WriteLine("Width not available...");
                    result = -1;
                }

                //
                // Set maximum height
                //
                // *** NOTES ***
                // A maximum is retrieved with the method GetMax(). A node's minimum and
                // maximum should always be a multiple of its increment.
                //
                if (cam.Height != null && cam.Height.IsWritable && cam.Height.IsReadable)
                {
                    cam.Height.Value = cam.Height.Max;

                    Console.WriteLine("Height set to {0}...", cam.Height.Value);
                }
                else
                {
                    Console.WriteLine("Height not available...");
                    result = -1;
                }
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function prints the device information of the camera from the
        // transport layer; please see NodeMapInfo_CSharp example for more
        // in-depth comments on printing device information from the nodemap.
        static int PrintDeviceInfo(IManagedCamera cam)
        {
            int result = 0;

            try
            {
                Console.WriteLine("\n*** DEVICE INFORMATION ***\n");

                INodeMap nodeMap = cam.GetTLDeviceNodeMap();

                ICategory category = nodeMap.GetNode<ICategory>("DeviceInformation");
                if (category != null && category.IsReadable)
                {
                    for (int i = 0; i < category.Children.Length; i++)
                    {
                        Console.WriteLine(
                            "{0}: {1}",
                            category.Children[i].Name,
                            (category.Children[i].IsReadable ? category.Children[i].ToString()
                             : "Node not available"));
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Device control information not available.");
                }
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function acquires and saves 10 images from a device; please see
        // Acquisition_CSharp example for more in-depth comments on the
        // acquisition of images.
        static int AcquireImages(IManagedCamera cam)
        {
            int result = 0;

            Console.WriteLine("\n\n*** IMAGE ACQUISITION ***\n");

            try
            {
                // Set acquisition mode to continuous
                if (cam.AcquisitionMode == null || !cam.AcquisitionMode.IsWritable)
                {
                    Console.WriteLine("Unable to set acquisition mode to continuous. Aborting...\n");
                    return -1;
                }

                cam.AcquisitionMode.Value = AcquisitionModeEnums.Continuous.ToString();

                Console.WriteLine("Acquisition mode set to continuous...");

                // Begin acquiring images
                cam.BeginAcquisition();

                Console.WriteLine("Acquiring images...");

                // Retrieve device serial number for filename
                String deviceSerialNumber = "";

                if (cam.TLDevice.DeviceSerialNumber != null || cam.TLDevice.DeviceSerialNumber.IsReadable)
                {
                    deviceSerialNumber = cam.TLDevice.DeviceSerialNumber.Value;

                    Console.WriteLine("Device serial number retrieved as {0}...", deviceSerialNumber);
                }
                Console.WriteLine();

                // Retrieve, convert, and save images
                const int NumImages = 10;

                //
                // Create ImageProcessor instance for post processing images
                //
                IManagedImageProcessor processor = new ManagedImageProcessor();

                //
                // Set default image processor color processing method
                //
                // *** NOTES ***
                // By default, if no specific color processing algorithm is set, the image
                // processor will default to NEAREST_NEIGHBOR method.
                //
                processor.SetColorProcessing(ColorProcessingAlgorithm.HQ_LINEAR);

                for (int imageCnt = 0; imageCnt < NumImages; imageCnt++)
                {
                    try
                    {
                        // Retrieve next received image and ensure image completion
                        using(IManagedImage rawImage = cam.GetNextImage(1000))
                        {
                            if (rawImage.IsIncomplete)
                            {
                                Console.WriteLine("Image incomplete with image status {0}...", rawImage.ImageStatus);
                            }
                            else
                            {
                                // Print image information
                                Console.WriteLine(
                                    "Grabbed image {0}, width = {1}, height = {2}",
                                    imageCnt,
                                    rawImage.Width,
                                    rawImage.Height);

                                // Convert image to mono 8
                                using(
                                    IManagedImage convertedImage = processor.Convert(rawImage, PixelFormatEnums.Mono8))
                                {
                                    // Create unique file name
                                    String filename = "ImageFormatControlQS-CSharp-";
                                    if (deviceSerialNumber != "")
                                    {
                                        filename = filename + deviceSerialNumber + "-";
                                    }
                                    filename = filename + imageCnt + ".jpg";

                                    // Save image
                                    convertedImage.Save(filename);

                                    Console.WriteLine("Image saved at {0}\n", filename);
                                }
                            }
                        }
                    }
                    catch (SpinnakerException ex)
                    {
                        Console.WriteLine("Error: {0}", ex.Message);
                        result = -1;
                    }
                }

                // End acquisition
                cam.EndAcquisition();
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function acts as the body of the example; please see
        // NodeMapInfo_CSharp_QuickSpin example for more in-depth comments on
        // setting up cameras.
        int RunSingleCamera(IManagedCamera cam)
        {
            int result = 0;
            int err = 0;

            try
            {
                // Initialize camera
                cam.Init();

                // Print device information
                result = PrintDeviceInfo(cam);

                // Configure custom image settings
                err = ConfigureCustomImageSettings(cam);
                if (err < 0)
                {
                    return err;
                }

                // Acquire images
                result = result | AcquireImages(cam);

                // Deinitialize camera
                cam.DeInit();
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // Example entry point; please see Enumeration_CSharp_QuickSpin
        // example for more in-depth comments on preparing and cleaning up
        // the system.
        static int Main(string[] args)
        {
            int result = 0;

            Program program = new Program();

            // Retrieve singleton reference to system object
            ManagedSystem system = new ManagedSystem();

            // Print out current library version
            LibraryVersion spinVersion = system.GetLibraryVersion();
            Console.WriteLine(
                "Spinnaker library version: {0}.{1}.{2}.{3}\n\n",
                spinVersion.major,
                spinVersion.minor,
                spinVersion.type,
                spinVersion.build);

            // Retrieve list of cameras from the system
            ManagedCameraList camList = system.GetCameras();

            Console.WriteLine("Number of cameras detected: {0}\n\n", camList.Count);

            // Finish if there are no cameras
            if (camList.Count == 0)
            {
                // Clear camera list before releasing system
                camList.Clear();

                // Release system
                system.Dispose();

                Console.WriteLine("Not enough cameras!");
                Console.WriteLine("Done! Press Enter to exit...");
                Console.ReadLine();

                return -1;
            }

            // Run example on each camera
            int index = 0;

            foreach(IManagedCamera managedCamera in camList) using(managedCamera)
            {
                Console.WriteLine("Running example for camera {0}...", index);

                try
                {
                    // Run example
                    result = result | program.RunSingleCamera(managedCamera);
                }
                catch (SpinnakerException ex)
                {
                    Console.WriteLine("Error: {0}", ex.Message);
                    result = -1;
                }

                Console.WriteLine("Camera {0} example complete...\n", index++);
            }

            // Clear camera list before releasing system
            camList.Clear();

            // Release system
            system.Dispose();

            Console.WriteLine("\nDone! Press Enter to exit...");
            Console.ReadLine();

            return result;
        }
    }
}
