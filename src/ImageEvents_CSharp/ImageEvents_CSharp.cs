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
 *  @example ImageEvents_CSharp.cs
 *
 *  @brief ImageEvents_CSharp.cs shows how to acquire images using the image
 *  event handler. It relies on information provided in the Enumeration_CSharp,
 *  Acquisition_CSharp, and NodeMapInfo_CSharp examples.
 *
 *	It can also be helpful to familiarize yourself with the
 *  NodeMapCallback_CSharp example, as nodemap callbacks follow the same
 *  general procedure as events, but with a few less steps.
 *
 *	This example creates a user-defined class, ImageEventListener, that inherits
 *  from the Spinnaker class, ManagedImageEventHandler. ImageEventListener allows the
 *  user to define any properties, parameters, and the event itself while
 *  ManagedImageEventHandler allows the child class to appropriately interface with
 *  Spinnaker.
 *  
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace ImageEvents_CSharp
{
    class Program
    {
        // This class defines the properties, parameters, and the event handler itself.
        // Take a moment to notice what parts of the class are mandatory, and
        // what have been added for demonstration purposes. First, any class
        // used to define image event handlers must inherit from ManagedImageEventHandler.
        // Second, the method signature of OnImageEvent() must also be
        // consistent and follow the override keyword. Everything else,
        // including the constructor, properties, body of OnImageEvent(), and
        // other functions, is particular to the example.
        class ImageEventListener : ManagedImageEventHandler
        {
            private string deviceSerialNumber;

            public const int NumImages = 10;
            public int imageCnt;
            IManagedImageProcessor processor;

            // The constructor retrieves the serial number and initializes the
            // image counter to 0.
            public ImageEventListener(IManagedCamera cam)
            {
                // Initialize image counter to 0
                imageCnt = 0;

                // Retrieve device serial number
                INodeMap nodeMap = cam.GetTLDeviceNodeMap();

                deviceSerialNumber = "";

                IString iDeviceSerialNumber = nodeMap.GetNode<IString>("DeviceSerialNumber");
                if (iDeviceSerialNumber != null && iDeviceSerialNumber.IsReadable)
                {
                    deviceSerialNumber = iDeviceSerialNumber.Value;
                }

                //
                // Create ImageProcessor instance for post processing images
                //
                processor = new ManagedImageProcessor();

                //
                // Set default image processor color processing method
                //
                // *** NOTES ***
                // By default, if no specific color processing algorithm is set, the image
                // processor will default to NEAREST_NEIGHBOR method.
                //
                processor.SetColorProcessing(ColorProcessingAlgorithm.HQ_LINEAR);
            }

            // This method defines an image event. In it, the image that
            // triggered the event is converted and saved before incrementing
            // the count. Please see Acquisition_CSharp example for more
            // in-depth comments on the acquisition of images.
            override protected void OnImageEvent(ManagedImage image)
            {
                if (imageCnt < NumImages)
                {
                    Console.WriteLine("Image event occurred...");

                    if (image.IsIncomplete)
                    {
                        Console.WriteLine("Image incomplete with image status {0}...\n", image.ImageStatus);
                    }
                    else
                    {
                        // Convert image
                        using(IManagedImage convertedImage = processor.Convert(image, PixelFormatEnums.Mono8))
                        {
                            // Print image information
                            Console.WriteLine(
                                "Grabbed image {0}, width = {1}, height = {2}",
                                imageCnt,
                                convertedImage.Width,
                                convertedImage.Height);

                            // Create unique filename in order to save file
                            String filename = "ImageEvents-CSharp-";
                            if (deviceSerialNumber != "")
                            {
                                filename = filename + deviceSerialNumber + "-";
                            }
                            filename = filename + imageCnt + ".jpg";

                            // Save image
                            convertedImage.Save(filename);

                            Console.WriteLine("Image saved at {0}\n", filename);

                            // Increment image counter
                            imageCnt++;
                        }
                    }

                    // Must manually release the image to prevent buffers on the camera stream from filling up
                    image.Release();
                }
            }
        }

        // This function configures the example to execute image events by
        // preparing and registering an image event.
        int ConfigureImageEvents(IManagedCamera cam, ref ImageEventListener imageEventListener)
        {
            int result = 0;

            Console.WriteLine("\n\n*** CONFIGURING IMAGE EVENTS ***\n");

            try
            {
                //
                // Create image event
                //
                // *** NOTES ***
                // The class has been constructed to accept a managed camera
                // in order to allow the saving of images with the device
                // serial number.
                //
                imageEventListener = new ImageEventListener(cam);

                //
                // Register image event handler
                //
                // *** NOTES ***
                // Image events are registered to cameras. If there are
                // multiple cameras, each camera must have the image events
                // registered to it separately. Also, multiple image events may
                // be registered to a single camera.
                //
                // *** LATER ***
                // Image events must be unregistered manually. This must be
                // done prior to releasing the system and while the image
                // events are still in scope.
                //
                cam.RegisterEventHandler(imageEventListener);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function waits for the appropriate amount of images. Notice
        // that whereas most examples actively retrieve images, the acquisition
        // of images is handled passively in this example.
        int WaitForImages(ref ImageEventListener imageEventListener)
        {
            int result = 0;

            try
            {
                //
                // Wait for images
                //
                // *** NOTES ***
                // In order to passively capture images using image events and
                // automatic polling, the main thread sleeps in increments of
                // 200 ms until 10 images have been acquired and saved.
                //
                const int sleepDuration = 200;

                while (imageEventListener.imageCnt < ImageEventListener.NumImages)
                {
                    Console.WriteLine("\t//");
                    Console.WriteLine("\t// Sleeping for {0} time slices. Grabbing images...", sleepDuration);
                    Console.WriteLine("\t//");

                    Thread.Sleep(sleepDuration);
                }
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This functions resets the example by unregistering the image event handler.
        int ResetImageEvents(IManagedCamera cam, ref ImageEventListener imageEventListener)
        {
            int result = 0;

            try
            {
                //
                // Unregister image event handler
                //
                // *** NOTES ***
                // It is important to unregister all image event handlers from all
                // cameras they are registered to.
                //
                cam.UnregisterEventHandler(imageEventListener);

                Console.WriteLine("Image events unregistered...\n");
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

        // This function passively waits for images by calling WaitForImages().
        // Notice that this function is much shorter than the AcquireImages()
        // function of other examples. This is because most of the code has
        // been moved to the image event's OnImageEvent() method.
        int AcquireImages(
            IManagedCamera cam,
            INodeMap nodeMap,
            INodeMap nodeMapTLDevice,
            ref ImageEventListener imageEventListener)
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
                        "Unable to get acquisition mode to continuous (enum entry retrieval). Aborting...\n");
                    return -1;
                }

                iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;

                Console.WriteLine("Acquisition mode set to continuous...");

                // Begin acquiring images
                cam.BeginAcquisition();

                Console.WriteLine("Acquiring images...");

                // Retrieve images using image event handler
                result = WaitForImages(ref imageEventListener);

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

                // Configure image event handlers
                ImageEventListener imageEventListener = null;

                err = ConfigureImageEvents(cam, ref imageEventListener);
                if (err < 0)
                {
                    return err;
                }

                // Acquire images using the image event handler
                result = result | AcquireImages(cam, nodeMap, nodeMapTLDevice, ref imageEventListener);

                // Reset image event handlers
                result = result | ResetImageEvents(cam, ref imageEventListener);

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
