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
 *  @example ImageFormatControl_CSharp.cs
 *
 *  @brief ImageFormatControl_CSharp.cs shows how to apply custom image settings
 *  to the camera. It relies on information provided in the Enumeration_CSharp,
 *	Acquisition_CSharp, and NodeMapInfo_CSharp examples.
 *
 *	This example demonstrates setting minimums to offsets, X and Y, and maximums
 *	to width and height. It also shows the setting of a new pixel format, which
 *  is an enumeration type node.
 *
 *	Following this, we suggest familiarizing yourself with the Exposure_CSharp
 *  example if you haven't already. Exposure_CSharp is another example on camera
 *  customization that is shorter and simpler than many of the others. Once
 *  comfortable with Exposure_CSharp and ImageFormatControl_CSharp, we suggest
 *  checking out any of the longer, more complicated examples related to camera
 *  configuration: ChunkData_CSharp, LookupTable_CSharp, Sequencer_CSharp, or
 *  Trigger_CSharp.
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

namespace ImageFormatControl_CSharp
{
    class Program
    {
        // This function configures a number of settings on the camera including
        // offsets X and Y, width, height, and pixel format. These settings must
        // be applied before BeginAcquisition() is called; otherwise, they will
        // be read only. Also, it is important to note that settings are applied
        // immediately. This means if you plan to reduce the width and move the
        // x offset accordingly, you need to apply such changes int he
        // appropriate order.
        int ConfigureCustomImageSettings(INodeMap nodeMap)
        {
            int result = 0;

            Console.WriteLine("\n*** CONFIGURING CUSTOM IMAGE SETTINGS ***\n");

            try
            {
                //
                // Apply mono 8 pixel format
                //
                // *** NOTES ***
                // Enumeration nodes are slightly more complicated to set than
                // other nodes. This is because setting an enumeration node
                // requires working with two nodes instead of the usual one.
                //
                // As such, there are a number of steps to setting an
                // enumeration node: retrieve the enumeration node from the
                // nodemap, retrieve the desired entry node from the enumeration
                // node, retrieve the integer value from the entry node, and set
                // the new value of the enumeration node with the integer value
                // from the entry node.
                //
                // Retrieve the enumeration node from the nodemap
                IEnum iPixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");
                if (iPixelFormat != null && iPixelFormat.IsWritable && iPixelFormat.IsReadable)
                {
                    // Retrieve the desired entry node from the enumeration node
                    IEnumEntry iPixelFormatMono8 = iPixelFormat.GetEntryByName("Mono8");
                    if (iPixelFormatMono8 != null && iPixelFormatMono8.IsReadable)
                    {
                        // Set value of entry node as new value for enumeration node
                        iPixelFormat.Value = iPixelFormatMono8.Value;

                        Console.WriteLine("Pixel format set to {0}...", iPixelFormat.Value.String);
                    }
                    else
                    {
                        Console.WriteLine("Pixel format mono 8 not available...");
                    }
                }
                else
                {
                    Console.WriteLine("Pixel format not available...");
                }

                //
                // Apply minimum to offset X
                //
                // *** NOTES ***
                // Numeric nodes have both a minimum and maximum. A minimum is
                // retrieved with the method GetMin(). Sometimes it can be
                // important to check minimums to ensure that your desired value
                // is within range.
                //
                IInteger iOffsetX = nodeMap.GetNode<IInteger>("OffsetX");
                if (iOffsetX != null && iOffsetX.IsWritable && iOffsetX.IsReadable)
                {
                    iOffsetX.Value = iOffsetX.Min;

                    Console.WriteLine("Offset X set to {0}...", iOffsetX.Min);
                }
                else
                {
                    Console.WriteLine("Offset X not available...");
                }

                //
                // Apply minimum to offset Y
                //
                // *** NOTES ***
                // It is often desirable to check the increment as well. The
                // increment is a number of which a desired value must be a
                // multiple. Certain nodes, such as those corresponding to
                // offsets X and Y, have an increment of 1, which basically
                // means that any value within range is appropriate. The
                // increment is retrieved with the method GetInc().
                //
                IInteger iOffsetY = nodeMap.GetNode<IInteger>("OffsetY");
                if (iOffsetY != null && iOffsetY.IsWritable && iOffsetY.IsReadable)
                {
                    iOffsetY.Value = iOffsetY.Min;

                    Console.WriteLine("Offset Y set to {0}...", iOffsetY.Min);
                }
                else
                {
                    Console.WriteLine("Offset Y not available...");
                }

                //
                // Set maximum width
                //
                // *** NOTES ***
                // Other nodes, such as those corresponding to image width and
                // height, might have an increment other than 1. In these cases,
                // it can be important to check that the desired value is a
                // multiple of the increment. However, as these values are being
                // set to the maximum, there is no reason to check against the
                // increment.
                //
                IInteger iWidth = nodeMap.GetNode<IInteger>("Width");
                if (iWidth != null && iWidth.IsWritable && iWidth.IsReadable)
                {
                    iWidth.Value = iWidth.Max;

                    Console.WriteLine("Width set to {0}...", iWidth.Max);
                }
                else
                {
                    Console.WriteLine("Width not available...");
                }

                //
                // Set maximum height
                //
                // *** NOTES ***
                // A maximum is retrieved with the method GetMax(). A node's
                // minimum and maximum should always be a multiple of its
                // increment.
                //
                IInteger iHeight = nodeMap.GetNode<IInteger>("Height");
                if (iHeight != null && iHeight.IsWritable && iHeight.IsReadable)
                {
                    iHeight.Value = iHeight.Max;

                    Console.WriteLine("Height set to {0}...\n", iHeight.Max);
                }
                else
                {
                    Console.WriteLine("Height not available...\n");
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
                    Console.WriteLine("Unable to get or set acquisition mode to continuous (node retrieval). Aborting...\n");
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
                                    String filename = "ImageFormatControl-CSharp-";
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

                // Configure custom image settings
                err = ConfigureCustomImageSettings(nodeMap);
                if (err < 0)
                {
                    return err;
                }

                // Acquire images
                result = result | AcquireImages(cam, nodeMap, nodeMapTLDevice);

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
