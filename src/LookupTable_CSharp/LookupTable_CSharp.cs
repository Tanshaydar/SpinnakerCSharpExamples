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
 *  @example LookupTable_CSharp.cs
 *
 *  @brief LookupTable_CSharp.cs shows how to configure lookup tables on the
 *  camera. It relies on information provided in the Enumeration_CSharp,
 *  Acquisition_CSharp, and NodeMapInfo_CSharp examples.
 *
 *	It can also be helpful to familiarize yourself with the
 *  ImageFormatControl_CSharp and Exposure_CSharp examples. As they are somewhat
 *  shorter and simpler, either provides a strong introduction to camera
 *  customization.
 *
 *	Lookup tables allow for the customization and control of individual pixels.
 *	This can be a very powerful and deeply useful tool; however, because use
 *	cases are context dependent, this example only explores lookup table
 *	configuration.
 *	
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics
 */

using System;
using System.IO;
using System.Collections.Generic;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace LookupTable_CSharp
{
    class Program
    {
        // This function handles the error prints when a node or entry is unavailable or
        // not readable on the connected camera
        void PrintRetrieveNodeFailure(string node, string name)
        {
            Console.WriteLine("Unable to get {0} ({1} {0} retrieval failed).\n\n", node, name);
            Console.WriteLine("The {0} may not be available on all camera models...\n", node);
            Console.WriteLine("Please try a Blackfly S camera.\n\n");
        }

        // This function configures lookup tables linearly. This involves
        // selecting the type of lookup table, finding the appropriate increment
        // calculated from the maximum value, and enabling lookup tables on the
        // camera.
        int ConfigureLookupTables(INodeMap nodeMap)
        {
            int result = 0;

            Console.WriteLine("\n\n*** CONFIGURING LOOKUP TABLE ***\n");

            try
            {
                //
                // Select lookup table type
                //
                // *** NOTES ***
                // Setting the lookup table selector. It is important to note
                // that this does not enable lookup tables.
                //
                IEnum iLUTSelector = nodeMap.GetNode<IEnum>("LUTSelector");
                if (iLUTSelector == null || !iLUTSelector.IsWritable || !iLUTSelector.IsReadable)
                {
                    PrintRetrieveNodeFailure("node", "LUTSelector");
                    return -1;
                }

                IEnumEntry iLUTSelectorLUT1 = iLUTSelector.GetEntryByName("LUT1");
                if (iLUTSelectorLUT1 == null || !iLUTSelectorLUT1.IsReadable)
                {
                    PrintRetrieveNodeFailure("entry", "LUTSelector LUT1");
                    return -1;
                }

                iLUTSelector.Value = iLUTSelectorLUT1.Value;

                Console.WriteLine("Lookup table selector set to LUT 1...");

                //
                // Determine pixel increment and set indexes and values as
                // desired
                //
                // *** NOTES ***
                // To get the pixel increment, the maximum range of the value
                // node must first be retrieved. The value node represents an
                // index, so its value should be one less than a power of 2
                // (e.g. 511, 1023, etc.). Add 1 to this index to get the
                // maximum range. Divide the maximum range by 512 to calculate
                // the pixel increment.
                //
                // Finally, all values (in the value node) and their
                // corresponding indexes (in the index node) need to be set.
                // The goal of this example is to set the lookup table linearly.
                // As such, the slope of the values should be set according to
                // the increment, but the slope of the indexes is
                // inconsequential.
                //
                // Retrieve value node
                IInteger iLUTValue = nodeMap.GetNode<IInteger>("LUTValue");
                if (iLUTValue == null || !iLUTValue.IsWritable || !iLUTValue.IsReadable)
                {
                    PrintRetrieveNodeFailure("node", "LUTValue");
                    return -1;
                }

                // Retrieve maximum range
                int maxRange = (int) iLUTValue.Max + 1;
                Console.WriteLine("\tMaximum range: {0}", maxRange);

                // Calculate increment
                int increment = maxRange / 512;
                Console.WriteLine("\tIncrement: {0}", increment);

                // Retrieve index node
                IInteger iLUTIndex = nodeMap.GetNode<IInteger>("LUTIndex");
                if (iLUTIndex == null || !iLUTIndex.IsWritable)
                {
                    PrintRetrieveNodeFailure("node", "LUTIndex");
                    return -1;
                }

                // Set values and indexes
                for (int i = 0; i < maxRange; i += increment)
                {
                    iLUTIndex.Value = i;

                    iLUTValue.Value = i;
                }

                Console.WriteLine("All lookup table values set...");

                //
                // Enable lookup tables
                //
                // *** NOTES ***
                // Once lookup tables have been configured, don't forget to
                // enable them with the appropriate node.
                //
                // *** LATER ***
                // Once the images with lookup tables have been collected,
                // turn the feature off with the same node.
                //
                IBool iLUTEnable = nodeMap.GetNode<IBool>("LUTEnable");
                if (iLUTEnable == null || !iLUTEnable.IsWritable)
                {
                    PrintRetrieveNodeFailure("node", "LUTEnable");
                    return -1;
                }

                iLUTEnable.Value = true;

                Console.WriteLine("Lookup tables enabled.\n");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function resets the camera by disabling lookup tables.
        int ResetLookupTables(INodeMap nodeMap)
        {
            int result = 0;

            try
            {
                //
                // Disable lookup tables
                //
                // *** NOTES ***
                // Turn look up tables off when they are not needed to reduce
                // overhead.
                //
                IBool iLUTEnable = nodeMap.GetNode<IBool>("LUTEnable");
                if (iLUTEnable == null || !iLUTEnable.IsWritable)
                {
                    PrintRetrieveNodeFailure("node", "LUTEnable");
                    return -1;
                }

                iLUTEnable.Value = false;

                Console.WriteLine("Lookup tables disabled.\n");
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
                if (iAcquisitionMode == null || !iAcquisitionMode.IsWritable || !iAcquisitionMode.IsReadable)
                {
                    Console.WriteLine("Unable to set acquisition mode to continuous (node retrieval). Aborting...\n");
                    return -1;
                }

                IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");
                if (iAcquisitionModeContinuous == null || !iAcquisitionModeContinuous.IsReadable)
                {
                    Console.WriteLine(
                        "Unable to get acquisition mode to continuous (enum entry retrieval). Aborting...\n");
                    return -1;
                }

                iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;

                Console.WriteLine("Acquisition mode set to continuous...");

                // Begin acquiring images
                cam.BeginAcquisition();

                Console.WriteLine("Acquiring images...");

                // Retrieve device serial number for filename
                String deviceSerialNumber = "";

                IString iDeviceSerialNumber = nodeMapTLDevice.GetNode<IString>("DeviceSerialNumber");
                if (iDeviceSerialNumber != null && iDeviceSerialNumber.IsReadable)
                {
                    deviceSerialNumber = iDeviceSerialNumber.Value;

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
                                    String filename = "LookupTable-CSharp-";
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

                // Configure lookup tables
                err = ConfigureLookupTables(nodeMap);
                if (err < 0)
                {
                    return err;
                }

                // Acquire images
                result = result | AcquireImages(cam, nodeMap, nodeMapTLDevice);

                // Reset lookup tables
                result = result | ResetLookupTables(nodeMap);

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
