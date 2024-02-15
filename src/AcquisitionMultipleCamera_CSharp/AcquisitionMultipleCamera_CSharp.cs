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
 *  @example AcquisitionMultipleCamera_CSharp.cs
 *
 *  @brief AcquisitionMultipleCamera_CSharp.cs shows how to capture images from
 *  multiple cameras simultaneously. It relies on information provided in the
 *  Enumeration_CSharp, Acquisition_CSharp, and NodeMapInfo_CSharp examples.
 *
 *  This example reads similarly to the Acquisition_CSharp example, except that
 *  loops and vectors are used to allow for simultaneous acquisitions.
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics
 */

using System;
using System.IO;
using System.Collections.Generic;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace AcquisitionMultipleCamera_CSharp
{
    class Program
    {
        // Disables or enables heartbeat on GEV cameras so debugging does not incur timeout errors
        static int ConfigureGVCPHeartbeat(IManagedCamera cam, bool enable)
        {
            //
            // Write to boolean node controlling the camera's heartbeat
            //
            // *** NOTES ***
            // This applies only to GEV cameras.
            //
            // GEV cameras have a heartbeat built in, but when debugging applications the
            // camera may time out due to its heartbeat. Disabling the heartbeat prevents
            // this timeout from occurring, enabling us to continue with any necessary 
            // debugging.
            //
            // *** LATER ***
            // Make sure that the heartbeat is reset upon completion of the debugging.  
            // If the application is terminated unexpectedly, the camera may not locked
            // to Spinnaker indefinitely due to the the timeout being disabled.  When that 
            // happens, a camera power cycle will reset the heartbeat to its default setting.
            //

            // Retrieve TL device nodemap and print device information
            INodeMap nodeMapTLDevice = cam.GetTLDeviceNodeMap();

            // Retrieve GenICam nodemap
            INodeMap nodeMap = cam.GetNodeMap();

            IEnum iDeviceType = nodeMapTLDevice.GetNode<IEnum>("DeviceType");
            IEnumEntry iDeviceTypeGEV = iDeviceType.GetEntryByName("GigEVision");
            // We first need to confirm that we're working with a GEV camera
            if (iDeviceType != null && iDeviceType.IsReadable)
            {
                if (iDeviceType.Value == iDeviceTypeGEV.Value)
                {
                    if (enable)
                    {
                        Console.WriteLine("Resetting heartbeat");
                    }
                    else
                    {
                        Console.WriteLine("Disabling heartbeat");
                    }
                    IBool iGEVHeartbeatDisable = nodeMap.GetNode<IBool>("GevGVCPHeartbeatDisable");
                    if (iGEVHeartbeatDisable == null || !iGEVHeartbeatDisable.IsWritable)
                    {
                        Console.WriteLine(
                            "Unable to disable heartbeat on camera. Continuing with execution as this may be non-fatal...");
                    }
                    else
                    {
                        iGEVHeartbeatDisable.Value = enable;

                        if (!enable)
                        {
                            Console.WriteLine("WARNING: Heartbeat has been disabled for the rest of this example run.");
                            Console.WriteLine("         Heartbeat will be reset upon the completion of this run.  If the ");
                            Console.WriteLine("         example is aborted unexpectedly before the heartbeat is reset, the");
                            Console.WriteLine("         camera may need to be power cycled to reset the heartbeat.\n");
                        }
                        else
                        {
                            Console.WriteLine("Heartbeat has been reset.\n");
                        }
                        Console.WriteLine();
                    }
                }
            }
            else
            {
                Console.WriteLine("Unable to access TL device nodemap. Aborting...");
                return -1;
            }

            return 0;
        }

        static int ResetGVCPHeartbeat(IManagedCamera cam)
        {
            return ConfigureGVCPHeartbeat(cam, true);
        }
        static int DisableGVCPHeartbeat(IManagedCamera cam)
        {
            return ConfigureGVCPHeartbeat(cam, false);
        }

        // This function acquires and saves 10 images from each device.
        int AcquireImages(ManagedCameraList cameraList)
        {
            int result = 0;

            Console.WriteLine("\n*** IMAGE ACQUISITION ***\n");

            try
            {
                //
                // Prepare each camera to acquire images
                //
                // *** NOTES ***
                // For pseudo-simultaneous streaming, each camera is prepared as
                // if it were just one, but in a loop. Notice that cameras are
                // selected with an index. We demonstrate pseduo-simultaneous
                // streaming because true simultaneous streaming would require
                // multiple process or threads, which is too complex for an
                // example.
                //
                // Serial numbers are the only persistent objects we gather in
                // this example, which is why a List<String> to hold them is
                // created.
                //
                List<String>deviceSerialNumbers = new List<String>();
                int index = 0;

                foreach(IManagedCamera managedCamera in cameraList)
                {
                    // Set acquisition mode to continuous
                    IEnum iAcquisitionMode = managedCamera.GetNodeMap().GetNode<IEnum>("AcquisitionMode");
                    if (iAcquisitionMode == null || !iAcquisitionMode.IsWritable)
                    {
                        Console.WriteLine(
                            "Unable to set acquisition mode to continuous (node retrieval camera {0}). Aborting...\n",
                            index);
                        return -1;
                    }

                    IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");
                    if (iAcquisitionModeContinuous == null || !iAcquisitionMode.IsReadable)
                    {
                        Console.WriteLine(
                            "Unable to set acquisition mode to continuous (enum entry retrieval camera {0}). Aborting...\n",
                            index);
                        return -1;
                    }

                    iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;

                    Console.WriteLine("Camera {0} acquisition mode set to continuous...", index);

#if DEBUG
                    // Disable heartbeat for GEV camera for Debug mode
                    if (DisableGVCPHeartbeat(managedCamera) != 0)
#else
                    // Reset heartbeat for GEV camera for Release mode
                    if (ResetGVCPHeartbeat(managedCamera) != 0)
#endif
                    {
                        return -1;
                    }

                    // Begin acquiring images
                    managedCamera.BeginAcquisition();

                    Console.WriteLine("Camera {0} started acquiring images...", index);

                    // Retrieve device serial numbers for filenames
                    deviceSerialNumbers.Add("");

                    IString iDeviceSerialNumber =
                        managedCamera.GetTLDeviceNodeMap().GetNode<IString>("DeviceSerialNumber");

                    if (iDeviceSerialNumber != null && iDeviceSerialNumber.IsReadable)
                    {
                        deviceSerialNumbers[index] = iDeviceSerialNumber.Value;

                        Console.WriteLine("Camera {0} serial number set to {1}...", index, deviceSerialNumbers[index]);
                    }

                    Console.WriteLine();
                    index++;
                }

                //
                // Retrieve, convert, and save images for each camera
                //
                // *** NOTES ***
                // In order to work with simultaneous camera streams, nested
                // loops are needed. It is important that the inner loop be the
                // one iterating through the cameras; otherwise, all image still
                // be grabbed from a single camera before grabbing any images
                // from another.
                //
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
                    index = 0;

                    foreach(IManagedCamera managedCamera in cameraList)
                    {

                        try
                        {
                            // Retrieve an image and ensure image completion
                            using(IManagedImage rawImage = managedCamera.GetNextImage(1000))
                            {
                                if (rawImage.IsIncomplete)
                                {
                                    Console.WriteLine(
                                        "Image incomplete with image status {0}...", rawImage.ImageStatus);
                                }
                                else
                                {
                                    // Print image information
                                    Console.WriteLine(
                                        "Camera {0} grabbed image {1}, width = {2}, height = {3}",
                                        index,
                                        imageCnt,
                                        rawImage.Width,
                                        rawImage.Height);

                                    // Convert image to mono 8
                                    using(
                                        IManagedImage convertedImage =
                                            processor.Convert(rawImage, PixelFormatEnums.Mono8))
                                    {
                                        // Create a unique filename
                                        String filename = "AcquisitionMultipleCamera-CSharp-";
                                        if (deviceSerialNumbers[index] != "")
                                        {
                                            filename = filename + deviceSerialNumbers[index] + "-";
                                        }
                                        filename = filename + index + "-" + imageCnt + ".jpg";

                                        // Save the image
                                        convertedImage.Save(filename);

                                        Console.WriteLine("Image saved at {0}\n", filename);
                                    }
                                }
                            }

                            index++;
                        }
                        catch (SpinnakerException ex)
                        {
                            Console.WriteLine("Error: {0}", ex.Message);
                            result = -1;
                        }
                    }
                }

                //
                // End acquisition for each camera
                //
                // *** NOTES ***
                // EndAcquisition() must be called for each camera.
                //
                foreach(IManagedCamera managedCamera in cameraList)
                {
                    // End acquisition
                    managedCamera.EndAcquisition();
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
        static int PrintDeviceInfo(INodeMap nodeMap, int camNum)
        {
            int result = 0;

            try
            {
                Console.WriteLine("Printing device information for camera {0}...\n", camNum);

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

        // This function acts as the body of the example; please see
        // NodeMapInfo_CSharp example for more in-depth comments on setting up
        // cameras.
        int RunMultipleCameras(ManagedCameraList cameraList)
        {
            int result = 0;

            try
            {
                //
                // Retrieve TL device nodemaps and print device information for
                // each camera
                //
                // *** NOTES ***
                // This example retrieves information from the transport layer
                // nodemap twice: once to print device information and once to
                // grab the device serial number. Rather than caching the
                // nodemap, each nodemap is retrieved both times as needed.
                //
                Console.WriteLine("\n*** DEVICE INFORMATION ***\n");
                int i = 0;

                foreach(IManagedCamera managedCamera in cameraList)
                {
                    // Retrieve TL device nodemap
                    INodeMap nodeMapTLDevice = managedCamera.GetTLDeviceNodeMap();

                    // Print device information
                    result = result | PrintDeviceInfo(nodeMapTLDevice, i++);
                }

                //
                // Initialize each camera
                //
                // *** NOTES ***
                // In this function, each step is placed in its own loop. This
                // constrasts AcquireImages() where there are less loops, each
                // housing more steps. Either strategy is sufficient.
                //
                // *** LATER ***
                // Each camera needs to be deinitialized once all images have
                // been acquired.
                //
                foreach(IManagedCamera managedCamera in cameraList)
                {
                    // Initialize camera
                    managedCamera.Init();
                }

                // Acquire images on all cameras
                result = result | AcquireImages(cameraList);

                //
                // Deinitialize each camera
                //
                // *** NOTES ***
                // Each camera must be deinitialized. A using statement is used
                // here to dispose of each camera prior to the end of the
                // example. Alternatively, Dispose() may be called on each
                // camera to the same end.
                //
                foreach(IManagedCamera managedCamera in cameraList) using(managedCamera)
                {
#if DEBUG
                    // Reset heartbeat for GEV camera
                    ResetGVCPHeartbeat(managedCamera);
#endif
                    // Deinitialize camera
                    managedCamera.DeInit();
                }
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // Example entry point; please see Enumeration_CSharp example for more
        // in-depth comments on preparing and cleaning up the system.
        static int Main(string[] args)
        {
            int result = 0;

            Program program = new Program();

            // Since this application saves images in the current folder
            // we must ensure that we have permission to write to this folder.
            // If we do not have permission, fail right away.
            FileStream fileStream;
            try
            {
                fileStream = new FileStream(@"test.txt", FileMode.Create);
                fileStream.Close();
                File.Delete("test.txt");
            }
            catch
            {
                Console.WriteLine("Failed to create file in current folder. Please check permissions.");
                Console.WriteLine("Press enter to exit...");
                Console.ReadLine();
                return -1;
            }

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
            ManagedCameraList cameraList = system.GetCameras();

            Console.WriteLine("Number of cameras detected: {0}\n\n", cameraList.Count);

            // Finish if there are no cameras
            if (cameraList.Count == 0)
            {
                // Clear camera list before releasing system
                cameraList.Clear();

                // Release system
                system.Dispose();

                Console.WriteLine("Not enough cameras!");
                Console.WriteLine("Done! Press Enter to exit...");
                Console.ReadLine();

                return -1;
            }

            // Run example on all cameras
            Console.WriteLine("Running example for all cameras...");

            try
            {
                result = program.RunMultipleCameras(cameraList);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            Console.WriteLine("Example complete...\n");

            // Clear camera list before releasing system
            cameraList.Clear();

            // Release system
            system.Dispose();

            Console.WriteLine("\nDone! Press Enter to exit...");
            Console.ReadLine();

            return result;
        }
    }
}
