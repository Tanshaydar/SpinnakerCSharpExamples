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
 *  @example SaveToAvi_CSharp.cs
 *
 *  @brief SaveToAvi_CSharp.cs shows how to create an video video from a list of
 *  images. It relies on information provided in the Enumeration_CSharp,
 *  Acquisition_CSharp, and NodeMapInfo_CSharp examples.
 *
 *  This example introduces the video class, which is used to quickly and
 *  easily create various types of videos. It demonstrates the creation of
 *  three types: uncompressed, MJPG, and H264.
 *
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics
 */

using System;
using System.Collections.Generic;
using System.IO;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using SpinnakerNET.Video;

namespace SaveToAvi_CSharp
{
    class Program
    {
        // Use the following enum and global static variable to select the type
        // of video file to be created and saved.
        enum VideoType
        {
            Uncompressed,
            Mjpg,
            H264
        }

        static VideoType chosenFileType = VideoType.Uncompressed;

        // This function prepares, saves, and cleans up an video from a list of images.
        int SaveListToVideo(INodeMap nodeMap, INodeMap nodeMapTLDevice, ref List<IManagedImage>images)
        {
            int result = 0;

            Console.WriteLine("\n\n*** CREATING VIDEO ***\n");

            try
            {
                // Retrieve device serial number for filename
                String deviceSerialNumber = "";

                IString iDeviceSerialNumber = nodeMapTLDevice.GetNode<IString>("DeviceSerialNumber");
                if (iDeviceSerialNumber != null && iDeviceSerialNumber.IsReadable)
                {
                    deviceSerialNumber = iDeviceSerialNumber.Value;

                    Console.WriteLine("Device serial number retrieved as {0}...", deviceSerialNumber);
                }

                //
                // Retrieve the current frame rate; acquisition frame rate recorded in hertz
                //
                // *** NOTES ***
                // The video frame rate can be set to anything; however, in
                // order to have videos play in real-time, the acquisition frame
                // rate can be retrieved from the camera.
                //
                IFloat iAcquisitionFrameRate = nodeMap.GetNode<IFloat>("AcquisitionFrameRate");
                if (iAcquisitionFrameRate == null || !iAcquisitionFrameRate.IsReadable)
                {
                    Console.WriteLine("Unable to retrieve frame rate. Aborting...\n");
                    return -1;
                }

                float frameRateToSet = (float) iAcquisitionFrameRate.Value;

                Console.WriteLine("Frame rate to be set to {0}", frameRateToSet);

                //
                // Create a unique filename
                //
                // *** NOTES ***
                // This example creates filenames according to the type of
                // video being created. Notice that '.avi' does not need to be
                // appended to the name of the file. This is because the video
                // recorder object takes care of the file extension
                // automatically.
                //
                string videoFilename;

                switch (chosenFileType)
                {
                    case VideoType.Uncompressed:
                        videoFilename = "SaveToAvi-CSharp-Uncompressed";
                        if (deviceSerialNumber != "")
                        {
                            videoFilename = videoFilename + "-" + deviceSerialNumber;
                        }
                        break;

                    case VideoType.Mjpg:
                        videoFilename = "SaveToAvi-CSharp-MJPG";
                        if (deviceSerialNumber != "")
                        {
                            videoFilename = videoFilename + "-" + deviceSerialNumber;
                        }
                        break;

                    case VideoType.H264:
                        videoFilename = "SaveToAvi-CSharp-H264";
                        if (deviceSerialNumber != "")
                        {
                            videoFilename = videoFilename + "-" + deviceSerialNumber;
                        }
                        break;

                    default:
                        videoFilename = "SaveToAvi-CSharp";
                        break;
                }

                //
                // Select option and open video file type
                //
                // *** NOTES ***
                // Depending on the filetype, a number of settings need to be
                // set in an object called an option. An uncompressed option
                // only needs to have the video frame rate set whereas videos
                // with MJPG or H264 compressions should have more values set.
                //
                // Once the desired option object is configured, open the video
                // file with the option in order to create the image file.
                //
                // *** LATER ***
                // Once all images have been added, it is important to close the
                // file - this is similar to many other standard file streams.
                //
                using(IManagedSpinVideo video = new ManagedSpinVideo())
                {
                    // Set maximum video file size to 2GB. A new video file is generated when 2GB
                    // limit is reached. Setting maximum file size to 0 indicates no limit.
                    const uint FileMaxSize = 2048;

                    video.SetMaximumFileSize(FileMaxSize);

                    switch (chosenFileType)
                    {
                        case VideoType.Uncompressed:
                            AviOption uncompressedOption = new AviOption();
                            uncompressedOption.frameRate = frameRateToSet;
                            video.Open(videoFilename, uncompressedOption);
                            break;

                        case VideoType.Mjpg:
                            MJPGOption mjpgOption = new MJPGOption();
                            mjpgOption.frameRate = frameRateToSet;
                            mjpgOption.quality = 75;
                            video.Open(videoFilename, mjpgOption);
                            break;

                        case VideoType.H264:
                            H264Option h264Option = new H264Option();
                            h264Option.frameRate = frameRateToSet;
                            h264Option.bitrate = 1000000;
                            h264Option.height = Convert.ToInt32(images[0].Height);
                            h264Option.width = Convert.ToInt32(images[0].Width);
                            video.Open(videoFilename, h264Option);
                            break;
                    }

                    //
                    // Construct and save video
                    //
                    // *** NOTES ***
                    // Although the video file has been opened, images must be
                    // individually appended in order to construct the video.
                    //
                    Console.WriteLine("Appending {0} images to video file {1}.avi...", images.Count, videoFilename);

                    for (int imageCnt = 0; imageCnt < images.Count; imageCnt++)
                    {
                        video.Append(images[imageCnt]);

                        Console.WriteLine("Appended image {0}...", imageCnt);
                    }
                    Console.WriteLine();

                    //
                    // Close video file
                    //
                    // *** NOTES ***
                    // Once all images have been appended, it is important to
                    // close the video file. Notice that once an video file has
                    // been closed, no more images can be added.
                    //
                    video.Close();
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
        int AcquireImages(IManagedCamera cam, INodeMap nodeMap, ref List<IManagedImage>images)
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
                    Console.WriteLine("Unable to set acquisition mode to continuous (entry retrieval). Aborting...\n");
                    return -1;
                }

                iAcquisitionMode.Value = iAcquisitionModeContinuous.Value;

                Console.WriteLine("Acquisition mode set to continuous...");

                // Begin acquiring images
                cam.BeginAcquisition();

                Console.WriteLine("Acquiring images...\n");

                // Retrieve and convert images
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
                        // Retrieve the next received images
                        using(IManagedImage rawImage = cam.GetNextImage(1000))
                        {
                            if (rawImage.IsIncomplete)
                            {
                                Console.WriteLine("Image incomplete with image status {0}...\n", rawImage.ImageStatus);
                            }
                            else
                            {
                                // Print image information
                                Console.WriteLine(
                                    "Grabbed image {0}, width = {1}, height {2}",
                                    imageCnt,
                                    rawImage.Width,
                                    rawImage.Height);

                                // Deep copy image into list
                                images.Add(processor.Convert(rawImage, PixelFormatEnums.Mono8));
                            }
                        }
                    }
                    catch (SpinnakerException ex)
                    {
                        Console.WriteLine("Error: {0}", ex.Message);
                        result = -1;
                    }
                }

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

                // Acquire images
                List<IManagedImage>images = new List<IManagedImage>();

                err = result | AcquireImages(cam, nodeMap, ref images);
                if (err < 0)
                {
                    return err;
                }

                // Create video
                result = result | SaveListToVideo(nodeMap, nodeMapTLDevice, ref images);

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

            // Ensure write permissions to current folder
            try
            {
                FileStream fileStream = new FileStream(@"test.txt", FileMode.Create);
                fileStream.Close();
                File.Delete("test.txt");
            }
            catch
            {
                Console.WriteLine("Failed to create file in current folder.  Please check permissions.\n");
                Console.WriteLine("\nDone! Press enter to exit...");
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

            Console.WriteLine("Number of cameras detected: {0}\n", camList.Count);

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
