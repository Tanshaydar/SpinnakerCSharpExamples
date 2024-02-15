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
 *  @example Exposure_CSharp_QuickSpin.cs
 *
 *  @brief Exposure_CSharp_QuickSpin.cs shows how to customize image exposure
 *  time using the QuickSpin API. QuickSpin is a subset of the Spinnaker library
 *  that allows for simpler node access and control.
 *
 *  This example prepares the camera, sets a new exposure time, and restores
 *  the camera to its default state. Ensure custom values fall within an
 *  acceptable range is also touched on. Retrieving and setting node values
 *  is the only portion of hte example that differs from Exposure_CSharp.
 *
 *  A much wider range of topics is covered in the full Spinnaker examples than
 *  in the QuickSpin ones. There are only enough QuickSpin examples to
 *  demonstrate node access and to get started with the API; please see full
 *  Spinnaker examples for further or specific knowledge on a topic.
 *  
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics
 */

using System;
using System.Collections.Generic;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace Exposure_CSharp_QuickSpin
{
    class Program
    {
        // This function configures a custom exposure time. Automatic exposure
        // is turned off in order to allow for the customization, and then the
        // custom setting is applied.
        int ConfigureExposure(IManagedCamera cam)
        {
            int result = 0;

            Console.WriteLine("\n*** CONFIGURING EXPOSURE ***\n");

            try
            {
                //
                // Turn off automatic exposure mode
                //
                // *** NOTES ***
                // Automatic exposure prevents the manual configuration of
                // exposure times and needs to be turned off for this example.
                // Enumerations representing entry nodes have been added to
                // QuickSpin. This allows for the much easier setting of
                // enumeration nodes to new values.
                //
                // In C#, the naming convention of the enum is the name of the
                // enumeration node followed by 'Enums' while the naming
                // convention of the entries is the symbolic of the entry node.
                // Selecting "Off" on the "ExposureAuto" node is thus named
                // accessed via "ExposureAutoEnums.Off".
                //
                // *** LATER ***
                // Exposure time can be set automatically or manually as needed.
                // This example turns automatic exposure off to set it manually
                // and back on to return the camera to its default state.
                //
                if (cam.ExposureAuto == null || !cam.ExposureAuto.IsWritable)
                {
                    Console.WriteLine("Unable to disable automatic exposure. Aborting...\n");
                    return -1;
                }

                cam.ExposureAuto.Value = ExposureAutoEnums.Off.ToString();

                Console.WriteLine("Automatic exposure disabled...");

                //
                // Set exposure time manually; exposure time recorded in
                // microseconds
                //
                // *** NOTES ***
                // Notice that the node is checked for availability and
                // writability prior to the setting of the node. Availability is
                // ensured by checking for null while writability is checked by
                // checking the access mode.
                //
                // Further, it is ensured that the desired exposure time does not
                // exceed the maximum. Exposure time is counted in microseconds -
                // this can be found out either by retrieving the unit with the
                // GetUnit() method or by checking SpinView.
                //
                const double exposureTimeToSet = 2000000.0;

                if (cam.ExposureTime == null || !cam.ExposureTime.IsWritable || !cam.ExposureTime.IsReadable)
                {
                    Console.WriteLine("Unable to set exposure time. Aborting...\n");
                    return -1;
                }

                // Ensure desired exposure time does not exceed the maximum
                cam.ExposureTime.Value = (exposureTimeToSet > cam.ExposureTime.Max ? cam.ExposureTime.Max
                                          : exposureTimeToSet);

                Console.WriteLine("Exposure time set to {0} us...\n", cam.ExposureTime.Value);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function returns the camera to a normal state by re-enabling
        // automatic exposure.
        int ResetExposure(IManagedCamera cam)
        {
            int result = 0;

            try
            {
                //
                // Turn automatic exposure back on
                //
                // *** NOTES ***
                // It is recommended to have automatic exposure enabled whenever
                // manual exposure settings are not required.
                //
                if (cam.ExposureAuto == null || !cam.ExposureAuto.IsWritable)
                {
                    Console.WriteLine("Unable to disable automatic exposure. Aborting...\n");
                    return -1;
                }

                cam.ExposureAuto.Value = ExposureAutoEnums.Continuous.ToString();

                Console.WriteLine("Automatic exposure enabled...");
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

            Console.WriteLine("\n*** IMAGE ACQUISITION ***\n");

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

                // Get the value of exposure time to set an appropriate timeout for GetNextImage
                if (cam.ExposureTime == null || !cam.ExposureTime.IsReadable)
                {
                    Console.WriteLine("Unable to read exposure time. Aborting...\n");
                    return -1;
                }

                // The exposure time is retrieved in µs so it needs to be converted to ms to keep consistency with the
                // unit being used in GetNextImage
                double timeout = cam.ExposureTime.Value / 1000 + 1000;

                // Retrieve, convert, and save images
                const int NumImages = 5;

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
                        // By default, GetNextImage will block indefinitely until an image arrives.
                        // In this example, the timeout value is set to [exposure time + 1000]ms to ensure that an image
                        // has enough time to arrive under normal conditions
                        using(IManagedImage rawImage = cam.GetNextImage((ulong) timeout))
                        {
                            if (rawImage.IsIncomplete)
                            {
                                Console.WriteLine("Image incomplete with image status {0}...", rawImage.ImageStatus);
                            }
                            else
                            {
                                // Print image information
                                Console.WriteLine(
                                    "Grabbed image {0}, width = {1}, height = {1}",
                                    imageCnt,
                                    rawImage.Width,
                                    rawImage.Height);

                                // Convert image to mono 8
                                using(
                                    IManagedImage convertedImage = processor.Convert(rawImage, PixelFormatEnums.Mono8))
                                {
                                    // Create unique file name
                                    String filename = "ExposureQS-CSharp-";
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

                // Configure exposure
                err = ConfigureExposure(cam);
                if (err < 0)
                {
                    return err;
                }

                // Acquire images
                result = result | AcquireImages(cam);

                // Reset exposure
                result = result | ResetExposure(cam);

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

            // Run exmaple on each camera
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
