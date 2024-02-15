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
 *  @example BufferHandling.cs
 *
 *  @brief BufferHandling_CSharp.cs shows how the different buffer handling modes work.
 *  It relies on information provided in the Acquisition and Trigger examples.
 *
 *  Buffer handling determines the ordering in which images are retrieved, and
 *  what occurs when an image is transmitted while the buffer is full.  There are
 *  four different buffer handling modes available; NewestFirst, NewestOnly,
 *  OldestFirst and OldestFirstOverwrite.
 *
 *  This example explores retrieving images in a set pattern; triggering the camera
 *  while not retrieving an image (letting the buffer fill up), and retrieving
 *  images while not triggering.  We cycle through the different buffer handling
 *  modes to see which images are retrieved, confirming their identites via their
 *  Frame ID values.
 *
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using SpinnakerNET.GenApi;
using SpinnakerNET;

namespace BufferHandling_CSharp
{
    class Program
    {
        // Total number of GenTL buffers. 1-2 buffers unavailable for some buffer modes
        static readonly int numBuffers = 6;

        // Number of triggers to load images from camera to Spinnaker
        static readonly int numTriggers = 10;

        // Number of times attempted to grab an image from Spinnaker to application
        static readonly int numGrabs = 10;

        // This helper function determines the appropriate number of images to expect
        // when running this example on various cameras and stream modes.
        static int GetExpectedImageCount(INodeMap nodeMapTLDevice, INodeMap sNodeMap)
        {
            // Check DeviceType and only adjust count for GigEVision device
            IEnum iDeviceType = nodeMapTLDevice.GetNode<IEnum>("DeviceType");
            IEnumEntry iDeviceTypeGEV = iDeviceType.GetEntryByName("GigEVision");
            if (iDeviceType != null && iDeviceType.IsReadable && (iDeviceType.Value == iDeviceTypeGEV.Value))
            {
                // Check StreamMode and only adjust count for TeledyneGigeVision stream mode
                IEnum iStreamMode = sNodeMap.GetNode<IEnum>("StreamMode");
                if (iStreamMode == null || !iStreamMode.IsReadable)
                {
                    Console.WriteLine("Unable to get device's stream mode. Aborting...");
                    return -1;
                }

                // Adjust the expected image count to account for the trash buffer in
                // TeledyneGigeVision driver, where we expect one less image than the
                // total number of buffers
                if (iStreamMode.ToString().Equals("TeledyneGigeVision"))
                {
                    return (numBuffers - 1);
                }
            }

            return numBuffers;
        }

        // This function configures the camera to use a trigger. First, trigger mode is
        // set to off in order to select the trigger source. Once the trigger source
        // has been selected, trigger mode is then enabled, which has the camera
        // capture only a single image upon the execution of the trigger.
        static int ConfigureTrigger(INodeMap nodeMap)
        {
            int result = 0;

            Console.WriteLine("\n*** CONFIGURING TRIGGER ***\n");

            try
            {
                //
                // Ensure trigger mode off
                //
                // *** NOTES ***
                // The trigger must be disabled in order to configure the
                // trigger source.
                //
                IEnum iTriggerMode = nodeMap.GetNode<IEnum>("TriggerMode");
                if (iTriggerMode == null || !iTriggerMode.IsReadable || !iTriggerMode.IsWritable)
                {
                    Console.WriteLine("Unable to disable trigger mode (enum retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry iTriggerModeOff = iTriggerMode.GetEntryByName("Off");
                if (iTriggerModeOff == null || !iTriggerModeOff.IsReadable)
                {
                    Console.WriteLine("Unable to disable trigger mode (entry retrieval). Aborting...");
                    return -1;
                }

                iTriggerMode.Value = iTriggerModeOff.Value;

                Console.WriteLine("Trigger mode disabled...");

                // Set trigger source to software
                IEnum iTriggerSource = nodeMap.GetNode<IEnum>("TriggerSource");
                if (iTriggerSource == null || !iTriggerSource.IsReadable || !iTriggerSource.IsWritable)
                {
                    Console.WriteLine("Unable to set trigger mode (enum retrieval). Aborting...");
                    return -1;
                }

                // Set trigger mode to software
                IEnumEntry iTriggerSourceSoftware = iTriggerSource.GetEntryByName("Software");
                if (iTriggerSourceSoftware == null || !iTriggerSourceSoftware.IsReadable)
                {
                    Console.WriteLine("Unable to set software trigger mode (entry retrieval). Aborting...");
                    return -1;
                }

                iTriggerSource.Value = iTriggerSourceSoftware.Value;

                Console.WriteLine("Trigger source set to software...");

                // Turn trigger mode on
                IEnumEntry iTriggerModeOn = iTriggerMode.GetEntryByName("On");
                if (iTriggerModeOn == null || !iTriggerModeOn.IsReadable)
                {
                    Console.WriteLine("Unable to enable trigger mode (entry retrieval). Aborting...");
                    return -1;
                }

                iTriggerMode.Value = iTriggerModeOn.Value;

                Console.WriteLine("Trigger mode turned back on...");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function retrieves a single image using the trigger. In this example,
        // only a single image is captured and made available for acquisition - as such,
        // attempting to acquire two images for a single trigger execution would cause
        // the example to hang. This is different from other examples, whereby a
        // constant stream of images are being captured and made available for image
        // acquisition.
        static int GrabNextImageByTrigger(INodeMap nodeMap)
        {
            int result = 0;

            try
            {
                // Execute software trigger
                ICommand softwareTriggerCommand = nodeMap.GetNode<ICommand>("TriggerSoftware");
                if (softwareTriggerCommand == null || !softwareTriggerCommand.IsWritable)
                {
                    Console.WriteLine("Unable to execute trigger. Aborting...");
                    return -1;
                }

                softwareTriggerCommand.Execute();
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function returns the camera to a normal state by turning off trigger
        // mode.
        static int ResetTrigger(INodeMap nodeMap)
        {
            int result = 0;

            try
            {
                //
                // Turn trigger mode back off
                //
                // *** NOTES ***
                // Once all images have been captured, turn trigger mode back off to
                // restore the camera to a clean state.
                //
                IEnum iTriggerMode = nodeMap.GetNode<IEnum>("TriggerMode");
                if (iTriggerMode == null || !iTriggerMode.IsWritable)
                {
                    Console.WriteLine("Unable to disable trigger mode (enum retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry iTriggerModeOff = iTriggerMode.GetEntryByName("Off");
                if (iTriggerModeOff == null || !iTriggerModeOff.IsReadable)
                {
                    Console.WriteLine("Unable to disable trigger mode (entry retrieval). Aborting...");
                    return -1;
                }

                iTriggerMode.Value = iTriggerModeOff.Value;

                Console.WriteLine("Trigger mode disabled...");
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
        static int PrintDeviceInfo(INodeMap nodeMap)
        {
            int result = 0;

            try
            {
                Console.WriteLine("\n*** DEVICE INFORMATION ***\n");

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
        static int AcquireImages(IManagedCamera cam, INodeMap nodeMap, INodeMap nodeMapTLDevice)
        {
            int result = 0;

            Console.WriteLine("\n*** IMAGE ACQUISITION ***\n");

            try
            {
                // Set acquisition mode to continuous
                IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");
                if (iAcquisitionMode == null || !iAcquisitionMode.IsReadable || !iAcquisitionMode.IsWritable)
                {
                    Console.WriteLine("Unable to set acquisition mode to continuous (node retrieval). Aborting...\n");
                    return -1;
                }

                IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");
                if (iAcquisitionModeContinuous == null || !iAcquisitionModeContinuous.IsReadable)
                {
                    Console.WriteLine(
                        "Unable to set acquisition mode to continuous (enum entry retrieval). Aborting...\n");
                    return -1;
                }

                iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;

                Console.WriteLine("Acquisition mode set to continuous...");

                // Set pixel format to mono8
                IEnum pixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");

                if (pixelFormat == null || !pixelFormat.IsWritable)
                {
                    Console.WriteLine("Unable to set Pixel Format mode (node retrieval). Aborting...\n");
                    return -1;
                }
                IEnumEntry mono8 = pixelFormat.GetEntryByName("Mono8");
                if (mono8 == null || !mono8.IsReadable)
                {
                    Console.WriteLine(
                        "Unable to set pixel format (entry 'mono8' retrieval). Aborting...\n");
                    return -1;
                }
                pixelFormat.Value = mono8.Symbolic;
                Console.WriteLine("Pixel format set to Mono8");

                // Retrieve device serial number for filename
                String deviceSerialNumber = "";

                IString iDeviceSerialNumber = nodeMapTLDevice.GetNode<IString>("DeviceSerialNumber");
                if (iDeviceSerialNumber != null && iDeviceSerialNumber.IsReadable)
                {
                    deviceSerialNumber = iDeviceSerialNumber.Value;

                    Console.WriteLine("Device serial number retrieved as {0}...", deviceSerialNumber);
                }
                Console.WriteLine();

                // Retrieve Stream Parameters device nodemap
                INodeMap sNodeMap = cam.GetTLStreamNodeMap();

                // Retrieve Buffer Handling Mode Information
                IEnum handlingMode = sNodeMap.GetNode<IEnum>("StreamBufferHandlingMode");
                if (handlingMode == null || !handlingMode.IsWritable)
                {
                    Console.WriteLine("Unable to set Buffer Handling mode (node retrieval). Aborting...");
                    return -1;
                }

                // Set stream buffer Count Mode to manual
                IEnum streamBufferCountMode = sNodeMap.GetNode<IEnum>("StreamBufferCountMode");
                if (streamBufferCountMode == null || !streamBufferCountMode.IsReadable || !streamBufferCountMode.IsWritable)
                {
                    Console.WriteLine("Unable to set Buffer Count Mode (node retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry streamBufferCountModeManual = streamBufferCountMode.GetEntryByName("Manual");
                if (streamBufferCountModeManual == null || !streamBufferCountModeManual.IsReadable)
                {
                    Console.WriteLine("Unable to set Buffer Count Mode (entry retrieval). Aborting...");
                    return -1;
                }

                streamBufferCountMode.Value = streamBufferCountModeManual.Value;

                Console.WriteLine("Stream Buffer Count Mode set to manual...");

                // Retrieve and modify Stream Buffer Count
                IInteger bufferCount = sNodeMap.GetNode<IInteger>("StreamBufferCountManual");
                if (bufferCount == null || !bufferCount.IsWritable)
                {
                    Console.WriteLine("Unable to set Buffer Count (Integer node retrieval). Aborting...");
                    return -1;
                }

                // Display buffer info
                Console.WriteLine($"Default Buffer Count: {bufferCount.Value}");
                Console.WriteLine($"Maximum Buffer Count: {bufferCount.Max}");

                bufferCount.Value = numBuffers;

                Console.WriteLine($"Buffer count now set to : {bufferCount.Value}");

                Console.WriteLine(
                    $"Camera will be triggered {numTriggers} times in a row, followed by {numGrabs} image retrieval attempts");
                Console.WriteLine("Note - Buffer behaviour is different for USB3 and GigE cameras");
                Console.WriteLine("     - USB3 cameras buffer images internally if no host buffers are available");
                Console.WriteLine("     - Once the stream buffer is released, the USB3 camera will fill that buffer");
                Console.WriteLine("     - GigE cameras do not buffer images");
                Console.WriteLine("     - In TeledyneGigEVision stream mode an extra trashing buffer will be reserved\n");

                bool firstStart = true;
                string[] bufferHandlingModes = { "NewestFirst", "OldestFirst", "NewestOnly", "OldestFirstOverwrite" };
                foreach (var mode in bufferHandlingModes)
                {
                    IEnumEntry handlingModeEntry = handlingMode.GetEntryByName(mode);
                    handlingMode.Value = handlingModeEntry.Value;
                    Console.WriteLine($"\n*** Buffer handling mode has been set to {handlingModeEntry.Symbolic} ***\n");

                    // Begin capturing images
                    cam.BeginAcquisition();

                    if (firstStart)
                    {
                        // Sleep for one second; only necessary when using non-BFS/ORX cameras on startup
                        Thread.Sleep(1000);
                        firstStart = false;
                    }

                    try
                    {
                        for (int i = 0; i < numTriggers; i++)
                        {
                            result = result | GrabNextImageByTrigger(nodeMap);
                            // Control framerate
                            Thread.Sleep(250);
                        }
                        Console.WriteLine($"Camera triggered {numTriggers} times\n");

                        Console.WriteLine("Retrieving images from library until no image data is returned (errors out)\n");
                        for (int i = 1; i < numGrabs; i++)
                        {
                            IManagedImage image = cam.GetNextImage(500);
                            if (image.IsIncomplete)
                            {
                                Console.WriteLine($"Image incomplete with image status {image.ImageStatus} ...");
                            }

                            // Create a unique filename
                            String filename = $"{handlingModeEntry.Symbolic }-{deviceSerialNumber}-{i}.jpg";

                            // Save image
                            image.Save(filename);
                            Console.WriteLine($"GetNextImage() #{i}, Frame ID: {image.FrameID}, Image saved at {filename}");

                            // Release image
                            image.Release();
                        }
                    }
                    catch (SpinnakerException e)
                    {
                        Console.WriteLine($"\nError: {e.Message}\n");
                        if (handlingModeEntry.Symbolic == "NewestFirst" ||
                            handlingModeEntry.Symbolic == "OldestFirst")
                        {
                            // In this mode, one buffer is used to cycle images within spinnaker acquisition engine.
                            // Only numBuffers - 1 images will be stored in the library; additional triggered images will be
                            // dropped.
                            // Calling GetNextImage() more than buffered images will return an error.
                            // Note: These two modes differ in the order of images returned.
                            int expectedImageCount = GetExpectedImageCount(nodeMapTLDevice, sNodeMap);
                            Console.WriteLine($"EXPECTED: error getting image #{expectedImageCount + 1} with handling mode set to NewestFirst or OldestFirst in GigE Streaming");
                        }
                        else if (handlingModeEntry.Symbolic == "NewestOnly")
                        {
                            // In this mode, a single buffer is overwritten if not read out in time    
                            Console.WriteLine("EXPECTED: error occur when getting image #2 with handling mode set to NewestOnly");
                        }
                        else if (handlingModeEntry.Symbolic == "OldestFirstOverwrite")
                        {
                            // In this mode, two buffers are used to cycle images within
                            // the spinnaker acquisition engine. Only numBuffers - 2 images will return to the user.
                            // Calling GetNextImage() without additional triggers will return an error
                            Console.WriteLine($"EXPECTED: error occur when getting image #{numBuffers - 1} with handling mode set to OldestFirstOverwrite");
                        }

                        result = -1;
                    }

                    cam.EndAcquisition();

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
        int RunSingleCamera(IManagedCamera cam)
        {
            int result = 0;
            int err = 0;

            try
            {
                // Retrieve TL device nodemap and print device information
                INodeMap nodeMapTLDevice = cam.GetTLDeviceNodeMap();

                result = PrintDeviceInfo(nodeMapTLDevice);

                // Initialize camera
                cam.Init();

                // Retrieve GenICam nodemap
                INodeMap nodeMap = cam.GetNodeMap();

                // Configure trigger
                err = ConfigureTrigger(nodeMap);
                if (err < 0)
                {
                    return err;
                }

                // Acquire images
                result = result | AcquireImages(cam, nodeMap, nodeMapTLDevice);

                // Reset trigger
                Console.WriteLine();
                result = result | ResetTrigger(nodeMap);
                Console.WriteLine();
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
            int i = 0;

            foreach(IManagedCamera managedCamera in camList) using(managedCamera)
            {
                Console.WriteLine("Running example for camera {0}...", i);

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

                Console.WriteLine("Camera {0} example complete...\n", i++);
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
