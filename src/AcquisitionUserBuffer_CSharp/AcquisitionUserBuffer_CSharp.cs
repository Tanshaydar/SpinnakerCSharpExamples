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
 *  @example AcquisitionUserBuffer.cs
 *
 *  @brief AcquisitionUserBuffer.cs shows how to use User Buffers for image
 *  acquisition.  The acquisition engine uses a pool of memory buffers.  The
 *  memory of a buffer can be allocated by the library (default) or the user.
 *  User Buffers refer to the latter.  This example relies on information
 *  provided in the Acquisition example.
 *
 *  This example demonstrates setting up the user allocated memory just before
 *  the acquisition of images.  First, the size of each buffer is determined
 *  based on the data payload size.  Then, depending on the the number of
 *  buffers (numBuffers) specified, the corresponding amount of memory is
 *  allocated.  Finally, after setting the buffer ownership to be users,
 *  the image acquisition can commence.
 *
 *  It is important to note that if the user provides the memory for the
 *  buffers, the user is ultimately responsible for freeing up memory.
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
using System.Runtime.InteropServices;

namespace Acquisition_CSharp
{
    class Program
    {
        // Whether the user memory is contiguous or non-contiguous
        static bool isContiguous = true;

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
                            Console.WriteLine(
                                "         Heartbeat will be reset upon the completion of this run.  If the ");
                            Console.WriteLine(
                                "         example is aborted unexpectedly before the heartbeat is reset, the");
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

        // This function acquires and saves 10 images from a device.
        static int AcquireImages(IManagedCamera cam, INodeMap nodeMap, INodeMap nodeMapTLDevice)
        {
            int result = 0;
            ulong numBuffers = 10;
            IntPtr pMemBufferContiguous = IntPtr.Zero;
            IntPtr[] ppMemBuffersNonContiguous = new IntPtr[numBuffers];

            Console.WriteLine("\n*** IMAGE ACQUISITION ***\n");

            try
            {
                //
                // Set acquisition mode to continuous
                //
                // *** NOTES ***
                // Because the example acquires and saves 10 images, setting
                // acquisition mode to continuous lets the example finish. If
                // set to single frame or multiframe (at a lower number of
                // images), the example would just hang. This is because the
                // example has been written to acquire 10 images while the
                // camera would have been programmed to retrieve less than that.
                //
                // Setting the value of an enumeration node is slightly more
                // complicated than other node types. Two nodes are required:
                // first, the enumeration node is retrieved from the nodemap and
                // second, the entry node is retrieved from the enumeration node.
                // The symbolic of the entry node is then set as the new value
                // of the enumeration node.
                //
                // Notice that both the enumeration and entry nodes are checked
                // for availability and readability/writability. Enumeration
                // nodes are generally readable and writable whereas entry
                // nodes are only ever readable.
                //
                // Retrieve enumeration node from nodemap
                IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");
                if (iAcquisitionMode == null || !iAcquisitionMode.IsWritable || !iAcquisitionMode.IsReadable)
                {
                    Console.WriteLine("Unable to set acquisition mode to continuous (node retrieval). Aborting...\n");
                    return -1;
                }

                // Retrieve entry node from enumeration node
                IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");
                if (iAcquisitionModeContinuous == null || !iAcquisitionModeContinuous.IsReadable)
                {
                    Console.WriteLine(
                        "Unable to set acquisition mode to continuous (enum entry retrieval). Aborting...\n");
                    return -1;
                }

                // Set symbolic from entry node as new value for enumeration node
                iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;

                Console.WriteLine("Acquisition mode set to continuous...");

                // Retrieve Stream Parameters device nodemap
                INodeMap sNodeMap = cam.GetTLStreamNodeMap();

                // Set stream buffer Count Mode to manual
                IEnum iStreamBufferCountMode = sNodeMap.GetNode<IEnum>("StreamBufferCountMode");
                if (iStreamBufferCountMode == null || !iStreamBufferCountMode.IsReadable ||
                    !iStreamBufferCountMode.IsWritable)
                {
                    Console.WriteLine("Unable to set Buffer Count Mode (node retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry iStreamBufferCountModeManual = iStreamBufferCountMode.GetEntryByName("Manual");
                if (iStreamBufferCountModeManual == null || !iStreamBufferCountModeManual.IsReadable)
                {
                    Console.WriteLine("Unable to set Buffer Count Mode entry(Entry retrieval).Aborting...");
                    return -1;
                }

                iStreamBufferCountMode.Value = iStreamBufferCountModeManual.Symbolic;

                //
                // Allocate buffers
                //
                // *** NOTES ***
                //
                // When allocating memory for user buffers, keep in mind that implicitly you are specifying how many
                // buffers are used for the acquisition engine.  There are two ways to set user buffers for Spinnaker
                // to utilize.  You can either pass a pointer to a contiguous buffer, or pass an array of pointers to
                // non-contiguous buffers into the library.  In either case, you will be responsible for allocating and
                // de-allocating the memory buffers that the pointers point to.
                //
                // The acquisition engine will be utilizing a bufferCount equal to totalSize divided by bufferSize,
                // where totalSize is the total allocated memory in bytes, and bufferSize is the image payload size.
                //
                // This example here demonstrates how to determine how much memory needs to be allocated based on the
                // retrieved payload size from the node map for both cases.
                //
                // Note that the acquisition engine may use up to two buffers as a cycling buffer in the event that
                // images are not disposed (Dispose() explicitly or allowed to fall out of scope) of in time to be
                // filled again; so it is advised to allocate enough memory for at least 2 buffers for OldestFirst
                // and NewestFirst stream modes, and allocate enough memory for 3 buffers in OldestFirstOverwrite
                // and NewestOnly mode.

                IInteger iPayloadSize = nodeMap.GetNode<IInteger>("PayloadSize");
                if (iPayloadSize == null || !iPayloadSize.IsReadable)
                {
                    Console.WriteLine("Unable to determine the payload size from the nodemap. Aborting...");
                    return -1;
                }

                ulong bufferSize = (ulong) iPayloadSize.Value;

                // Calculate the 1024 aligned image size to be used for USB cameras
                var deviceType = cam.GetTLDeviceNodeMap().GetNode<IEnum>("DeviceType");
                if (deviceType != null && deviceType.Value == (int) DeviceTypeEnum.USB3Vision)
                {
                    const ulong usbPacketSize = 1024;
                    bufferSize = ((bufferSize + usbPacketSize - 1) / usbPacketSize) * usbPacketSize;
                }

                // Set buffer ownership to user.
                // This must be set before using user buffers when calling BeginAcquisition().
                // If not set, BeginAcquisition() will use the system's buffers.
                if (cam.GetBufferOwnership() != BufferOwnership.BUFFER_OWNERSHIP_USER)
                {
                    cam.SetBufferOwnership(BufferOwnership.BUFFER_OWNERSHIP_USER);
                }

                // Contiguous memory buffer
                if (isContiguous)
                {
                    try
                    {
                        // Make sure to allocate unmanaged memory with AllocHGlobal so that the memory
                        // is pinned and connot be moved by the garbage collector.
                        // C# will not automatically free memory allocated by Marshal.AllocHGlobal,
                        // so memory needs to be freed with Marshal.FreeHGlobal to avoid memory leak
                        pMemBufferContiguous = Marshal.AllocHGlobal((int)(numBuffers * bufferSize));
                    }
                    catch (OutOfMemoryException /*e*/)
                    {
                        Console.WriteLine("Unable to allocate the memory required. Aborting...");
                        return -1;
                    }

                    cam.SetUserBuffers(pMemBufferContiguous, (numBuffers * bufferSize));

                    Console.WriteLine(
                        "User-allocated memory 0x{0} will be used for user buffers...",
                        pMemBufferContiguous.ToString("X"));
                }
                else
                {
                    try
                    {
                        // Make sure to allocate unmanaged memory with AllocHGlobal so that the memory
                        // is pinned and connot be moved by the garbage collector.
                        // C# will not automatically free memory allocated by Marshal.AllocHGlobal,
                        // so memory needs to be freed with Marshal.FreeHGlobal to avoid memory leak
                        for (ulong i = 0; i < numBuffers; i++)
                        {
                            ppMemBuffersNonContiguous[i] = Marshal.AllocHGlobal((int)(bufferSize));
                        }
                    }
                    catch (OutOfMemoryException /*e*/)
                    {
                        Console.WriteLine("Unable to allocate the memory required. Aborting...");
                        return -1;
                    }

                    cam.SetUserBuffers(ppMemBuffersNonContiguous, numBuffers, bufferSize);

                    Console.WriteLine("User-allocated memory:");
                    for (ulong i = 0; i < numBuffers; i++)
                    {
                        Console.WriteLine("\t0x{0}", ppMemBuffersNonContiguous[i].ToString("X"));
                    }
                    Console.WriteLine("will be used for user buffers...");
                }

                //
                // Begin acquiring images
                //
                // *** NOTES ***
                // What happens when the camera begins acquiring images depends
                // on which acquisition mode has been set. Single frame captures
                // only a single image, multi frame catures a set number of
                // images, and continuous captures a continuous stream of images.
                // Because the example calls for the retrieval of 10 images,
                // continuous mode has been set for the example.
                //
                // *** LATER ***
                // Image acquisition must be ended when no more images are needed.
                //
                cam.BeginAcquisition();

                // Retrieve the resulting stream buffer count nBuffers
                // Note: the buffer count result is dependent on the Stream Buffer Count Mode (Auto/Manual).
                // For Manual mode, Spinnaker uses the allocated memory size and payload size to calculate the number
                // of buffers.  For Auto mode, Spinnaker uses additional information such as frame rate to determine
                // the number of buffers.
                IInteger iStreamBufferCountResult = sNodeMap.GetNode<IInteger>("StreamBufferCountResult");
                if (iStreamBufferCountResult == null || !iStreamBufferCountResult.IsReadable)
                {
                    Console.WriteLine("Unable to retrieve Buffer Count result (node retrieval). Aborting...");
                    return -1;
                }
                long streamBufferCountResult = iStreamBufferCountResult.Value;

                Console.WriteLine("Resulting stream buffer count: {0}.", streamBufferCountResult);

                Console.WriteLine("Acquiring images...");

                //
                // Retrieve device serial number for filename
                //
                // *** NOTES ***
                // The device serial number is retrieved in order to keep
                // different cameras from overwriting each other's images.
                // Grabbing image IDs and frame IDs make good alternatives for
                // this purpose.
                //
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
                // By default, if no specific color processing algorithm Is set, the image
                // processor will default to NEAREST_NEIGHBOR method.
                //
                processor.SetColorProcessing(ColorProcessingAlgorithm.HQ_LINEAR);

                for (int imageCnt = 0; imageCnt < NumImages; imageCnt++)
                {
                    try
                    {
                        //
                        // Retrieve next received image
                        //
                        // *** NOTES ***
                        // Capturing an image houses images on the camera buffer.
                        // Trying to capture an image that does not exist will
                        // hang the camera.
                        //
                        // Using-statements help ensure that images are released.
                        // If too many images remain unreleased, the buffer will
                        // fill, causing the camera to hang. Images can also be
                        // released manually by calling Release().
                        //
                        using(IManagedImage rawImage = cam.GetNextImage(1000))
                        {
                            //
                            // Ensure image completion
                            //
                            // *** NOTES ***
                            // Images can easily be checked for completion. This
                            // should be done whenever a complete image is
                            // expected or required. Alternatively, check image
                            // status for a little more insight into what
                            // happened.
                            //
                            if (rawImage.IsIncomplete)
                            {
                                Console.WriteLine("Image incomplete with image status {0}...", rawImage.ImageStatus);
                            }
                            else
                            {
                                //
                                // Print image information; width and height
                                // recorded in pixels
                                //
                                // *** NOTES ***
                                // Images have quite a bit of available metadata
                                // including CRC, image status, and offset
                                // values to name a few.
                                //
                                uint width = rawImage.Width;

                                uint height = rawImage.Height;

                                Console.WriteLine(
                                    "Grabbed image {0}, width = {1}, height = {1}", imageCnt, width, height);

                                //
                                // Convert image to mono 8
                                //
                                // *** NOTES ***
                                // Images can be converted between pixel formats
                                // by using the appropriate enumeration value.
                                // Unlike the original image, the converted one
                                // does not need to be released as it does not
                                // affect the camera buffer.
                                //
                                // Using statements are a great way to ensure code
                                // stays clean and avoids memory leaks.
                                // leaks.
                                //
                                using(
                                    IManagedImage convertedImage = processor.Convert(rawImage, PixelFormatEnums.Mono8))
                                {
                                    // Create a unique filename
                                    String filename = "AcquisitionUserBuffer-CSharp-";
                                    if (deviceSerialNumber != "")
                                    {
                                        filename = filename + deviceSerialNumber + "-";
                                    }
                                    filename = filename + imageCnt + ".jpg";

                                    //
                                    // Save image
                                    //
                                    // *** NOTES ***
                                    // The standard practice of the examples is
                                    // to use device serial numbers to keep
                                    // images of one device from overwriting
                                    // those of another.
                                    //
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

                //
                // End acquisition
                //
                // *** NOTES ***
                // Ending acquisition appropriately helps ensure that devices
                // clean up properly and do not need to be power-cycled to
                // maintain integrity.
                //
                cam.EndAcquisition();
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }
            finally
            {
                // Clean up memory
                if (isContiguous)
                {
                    if (pMemBufferContiguous != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pMemBufferContiguous);
                        Console.WriteLine("Cleaned up user-allocated memory used for user buffers...");
                    }
                }
                else
                {
                    for (ulong i = 0; i < numBuffers; i++)
                    {
                        if (ppMemBuffersNonContiguous[i] != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(ppMemBuffersNonContiguous[i]);
                            Console.WriteLine("Cleaned up user-allocated memory buffer array index {0}...", i);
                        }
                    }
                }
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

        // This function acts as the body of the example; please see
        // NodeMapInfo_CSharp example for more in-depth comments on setting up
        // cameras.
        int RunSingleCamera(IManagedCamera cam)
        {
            int result = 0;

            try
            {
                // Retrieve TL device nodemap and print device information
                INodeMap nodeMapTLDevice = cam.GetTLDeviceNodeMap();

                result = PrintDeviceInfo(nodeMapTLDevice);

                // Initialize camera
                cam.Init();

                // Retrieve GenICam nodemap
                INodeMap nodeMap = cam.GetNodeMap();

                // Configure heartbeat for GEV camera
#if DEBUG
                result = result | DisableGVCPHeartbeat(cam);
#else
                result = result | ResetGVCPHeartbeat(cam);
#endif

                // Acquire images
                result = result | AcquireImages(cam, nodeMap, nodeMapTLDevice);

#if DEBUG
                // Reset heartbeat for GEV camera
                result = result | ResetGVCPHeartbeat(cam);
#endif

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

            //
            // Run example on each camera
            //
            // *** NOTES ***
            // Cameras can either be retrieved as their own IManagedCamera
            // objects or from camera lists using the [] operator and an index.
            //
            // Using-statements help ensure that cameras are disposed of when
            // they are no longer needed; otherwise, cameras can be disposed of
            // manually by calling Dispose(). In C#, if cameras are not disposed
            // of before the system is released, the system will do so
            // automatically.
            //
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
