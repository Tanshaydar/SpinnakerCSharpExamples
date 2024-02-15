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
 *   @example CounterAndTimer_CSharp.cs
 *
 *   @brief CounterAndTimer_CSharp.cs shows how to setup a Pulse Width Modulation (PWM)
 *   signal using counters and timers. The camera will output the PWM signal via
 *   strobe, and capture images at a rate defined by the PWM signal as well.
 *   Users should take care to use a PWM signal within the camera's max
 *   framerate (by default, the PWM signal is set to 50 Hz).
 *
 *   Counter and Timer functionality is only available for BFS and Oryx Cameras.
 *   For details on the hardware setup, see our kb article, "Using Counter and
 *   Timer Control";
 * https://www.flir.com/support-center/iis/machine-vision/application-note/using-counter-and-timer-control
 *
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics
 */

using System;
using System.IO;
using System.Collections.Generic;
using SpinnakerNET.GenApi;
using SpinnakerNET;

namespace CounterAndTimer_CSharp
{
    class Program
    {
        // This function configures the camera to setup a Pulse Width Modulation signal using
        // Counter and Timer functionality.  By default, the PWM signal will be set to run at
        // 50hz, with a duty cycle of 70%.
        static int SetupCounterAndTimer(INodeMap nodeMap)
        {
            int result = 0;

            Console.WriteLine("Configuring Pulse Width Modulation signal");

            try
            {
                // Set Counter Selector to Counter 0
                IEnum counterSelector = nodeMap.GetNode<IEnum>("CounterSelector");
                
                if (!counterSelector.IsReadable || !counterSelector.IsWritable)
                {
                    Console.WriteLine("Unable to set Counter Selector (enum retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry counter0 = counterSelector.GetEntryByName("Counter0");
                if (!counter0.IsReadable)
                {
                    Console.WriteLine("Unable to get Counter Selector (entry retrieval). Aborting...");
                    return -1;
                }

                counterSelector.Value = counter0.Value;

                // Set Counter Event Source to MhzTick
                IEnum counterEventSource = nodeMap.GetNode<IEnum>("CounterEventSource");
                if (!counterEventSource.IsReadable || !counterEventSource.IsWritable)
                {
                    Console.WriteLine("Unable to set Counter Event Source (enum retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry counterEventSourceMhzTick = counterEventSource.GetEntryByName("MHzTick");
                if (!counterEventSourceMhzTick.IsReadable)
                {
                    Console.WriteLine("Unable to get Counter Event Source (entry retrieval). Aborting...");
                    return -1;
                }

                counterEventSource.Value = counterEventSourceMhzTick.Value;

                // Set Counter Duration to 14000
                IInteger counterDuration = nodeMap.GetNode<IInteger>("CounterDuration");
                if (!counterDuration.IsReadable || !counterDuration.IsWritable)
                {
                    Console.WriteLine("Unable to set Counter Duration (integer retrieval). Aborting...");
                    return -1;
                }

                counterDuration.Value = 14000;

                // Set Counter Delay to 6000
                IInteger counterDelay = nodeMap.GetNode<IInteger>("CounterDelay");
                if (!counterDelay.IsWritable)
                {
                    Console.WriteLine("Unable to set Counter Delay (integer retrieval). Aborting...");
                    return -1;
                }

                counterDelay.Value = 6000;

                // Determine duty cycle of PWM signal
                long dutyCycle =
                    (long)(counterDuration.Value / (counterDuration.Value + (float) counterDelay.Value) * 100);

                Console.WriteLine("The duty cycle has been set to {0} %", dutyCycle);

                // Determine pulse rate of PWM signal
                long pulseRate = (long)(1000000 / (counterDuration.Value + (float) counterDelay.Value));

                Console.WriteLine("The pulse rate has been set to {0} Hz", pulseRate);

                // Set Counter Trigger Source to Frame Trigger Wait
                IEnum counterTriggerSource = nodeMap.GetNode<IEnum>("CounterTriggerSource");
                if (!counterTriggerSource.IsReadable || !counterTriggerSource.IsWritable)
                {
                    Console.WriteLine("Unable to set Counter Trigger Source (enum retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry counterTriggerSourceFTW = counterTriggerSource.GetEntryByName("FrameTriggerWait");
                if (!counterTriggerSourceFTW.IsReadable)
                {
                    Console.WriteLine("Unable to get Counter Trigger Source (entry retrieval). Aborting...");
                    return -1;
                }

                counterTriggerSource.Value = counterTriggerSourceFTW.Value;

                // Set Counter Trigger Activation to Level High
                IEnum counterTriggerActivation = nodeMap.GetNode<IEnum>("CounterTriggerActivation");
                if (!counterTriggerActivation.IsReadable || !counterTriggerActivation.IsWritable)
                {
                    Console.WriteLine("Unable to set Counter Trigger Activation (enum retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry counterTriggerActivationLH = counterTriggerActivation.GetEntryByName("LevelHigh");
                if (!counterTriggerActivationLH.IsReadable)
                {
                    Console.WriteLine("Unable to get Counter Trigger Activation (entry retrieval). Aborting...");
                    return -1;
                }

                counterTriggerActivation.Value = counterTriggerActivationLH.Value;
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // Configure GPIO to output the PWM signal
        static int ConfigureDigitalIO(INodeMap nodeMap)
        {
            int result = 0;

            string cameraFamilyBFS = "BFS";
            string cameraFamilyOryx = "ORX";

            Console.WriteLine("Configuring GPIO strobe output");

            try
            {
                IString deviceModelName = nodeMap.GetNode<IString>("DeviceModelName");
                if (!deviceModelName.IsReadable)
                {
                    Console.WriteLine("Unable to determine camera family. Aborting...");
                    return -1;
                }

                string cameraModel = deviceModelName.Value;

                // Set Line Selector
                IEnum lineSelector = nodeMap.GetNode<IEnum>("LineSelector");
                if (!lineSelector.IsReadable || !lineSelector.IsWritable)
                {
                    Console.WriteLine("Unable to set Line Selector (enum retrieval). Aborting...");
                    return -1;
                }

                if (cameraModel.Contains(cameraFamilyBFS))
                {
                    IEnumEntry lineSelectorLine1 = lineSelector.GetEntryByName("Line1");
                    if (!lineSelectorLine1.IsReadable)
                    {
                        Console.WriteLine("Unable to get Line Selector (entry retrieval). Aborting...");
                        return -1;
                    }

                    lineSelector.Value = lineSelectorLine1.Value;
                }
                else if (cameraModel.Contains(cameraFamilyOryx))
                {
                    IEnumEntry lineSelectorLine2 = lineSelector.GetEntryByName("Line2");
                    if (!lineSelectorLine2.IsReadable)
                    {
                        Console.WriteLine("Unable to get Line Selector (entry retrieval). Aborting...");
                        return -1;
                    }

                    lineSelector.Value = lineSelectorLine2.Value;

                    // Set Line Mode to output
                    IEnum lineMode = nodeMap.GetNode<IEnum>("LineMode");
                    if (!lineMode.IsWritable)
                    {
                        Console.WriteLine("Unable to set Line Mode (enum retrieval). Aborting...");
                        return -1;
                    }

                    IEnumEntry lineModeOutput = lineMode.GetEntryByName("Output");
                    if (!lineModeOutput.IsReadable)
                    {
                        Console.WriteLine("Unable to get Line Mode (entry retrieval). Aborting...");
                        return -1;
                    }

                    lineMode.Value = lineModeOutput.Value;
                }

                // Set Line Source for Selected Line to Counter 0 Active
                IEnum lineSource = nodeMap.GetNode<IEnum>("LineSource");
                if (!lineSource.IsReadable || !lineSource.IsWritable)
                {
                    Console.WriteLine("Unable to set Line Source (enum retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry lineSourceCounter0Active = lineSource.GetEntryByName("Counter0Active");
                if (!lineSourceCounter0Active.IsReadable)
                {
                    Console.WriteLine("Unable to get Line Source (entry retrieval). Aborting...");
                    return -1;
                }

                lineSource.Value = lineSourceCounter0Active.Value;

                if (cameraModel.Contains(cameraFamilyBFS))
                {
                    // Change Line Selector to Line 2 and enable 3.3V rail
                    IEnumEntry lineSelectorLine2 = lineSelector.GetEntryByName("Line2");
                    if (!lineSelectorLine2.IsReadable)
                    {
                        Console.WriteLine("Unable to get Line Selector (entry retrieval). Aborting...");
                        return -1;
                    }

                    lineSelector.Value = lineSelectorLine2.Value;

                    IBool voltageEnable = nodeMap.GetNode<IBool>("V3_3Enable");
                    if (!voltageEnable.IsWritable)
                    {
                        Console.WriteLine("Unable to set Voltage Enable (boolean retrieval). Aborting...");
                        return -1;
                    }

                    voltageEnable.Value = true;
                }
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        static int ConfigureExposureAndTrigger(INodeMap nodeMap)
        {
            int result = 0;

            Console.WriteLine("Configuring Exposure and Trigger");

            try
            {
                // Turn off auto exposure
                IEnum exposureAuto = nodeMap.GetNode<IEnum>("ExposureAuto");
                if (!exposureAuto.IsReadable || !exposureAuto.IsWritable)
                {
                    Console.WriteLine("Unable to set Exposure Auto (enum retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry exposureAutoOff = exposureAuto.GetEntryByName("Off");
                if (!exposureAutoOff.IsReadable)
                {
                    Console.WriteLine("Unable to get Exposure Auto (entry retrieval). Aborting...");
                    return -1;
                }

                exposureAuto.Value = exposureAutoOff.Value;

                // Set Exposure Time to less than 1/50th of a second (5000 us is used as an example)
                IFloat exposureTime = nodeMap.GetNode<IFloat>("ExposureTime");
                if (!exposureTime.IsWritable)
                {
                    Console.WriteLine("Unable to set Exposure Time (float retrieval). Aborting...");
                    return -1;
                }

                exposureTime.Value = 5000;

                // Ensure trigger mode off
                //
                // *** NOTES ***
                // The trigger must be disabled in order to configure
                IEnum triggerMode = nodeMap.GetNode<IEnum>("TriggerMode");
                if (triggerMode == null || !triggerMode.IsReadable || !triggerMode.IsWritable)
                {
                    Console.WriteLine("Unable to disable trigger mode (enum retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry triggerModeOff = triggerMode.GetEntryByName("Off");
                if (triggerModeOff == null || !triggerModeOff.IsReadable)
                {
                    Console.WriteLine("Unable to disable trigger mode (entry retrieval). Aborting...");
                    return -1;
                }

                triggerMode.Value = triggerModeOff.Value;

                Console.WriteLine("Trigger mode disabled...");

                // Set Trigger Source to Counter 0 Start
                IEnum triggerSource = nodeMap.GetNode<IEnum>("TriggerSource");
                if (triggerSource == null || !triggerSource.IsReadable || !triggerSource.IsWritable)
                {
                    Console.WriteLine("Unable to set trigger mode (enum retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry triggerSourceCounter0Start = triggerSource.GetEntryByName("Counter0Start");
                if (triggerSourceCounter0Start == null || !triggerSourceCounter0Start.IsReadable)
                {
                    Console.WriteLine("Unable to get trigger mode (entry retrieval). Aborting...");
                    return -1;
                }

                triggerSource.Value = triggerSourceCounter0Start.Value;

                // Set Trigger Overlap to Readout
                IEnum triggerOverlap = nodeMap.GetNode<IEnum>("TriggerOverlap");
                if (triggerOverlap == null || !triggerOverlap.IsReadable || !triggerOverlap.IsWritable)
                {
                    Console.WriteLine("Unable to set trigger overlap (enum retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry triggerOverlapRO = triggerOverlap.GetEntryByName("ReadOut");
                if (triggerOverlapRO == null || !triggerOverlapRO.IsReadable)
                {
                    Console.WriteLine("Unable to get trigger overlap (entry retrieval). Aborting...");
                    return -1;
                }

                triggerOverlap.Value = triggerOverlapRO.Value;

                // Turn trigger mode on
                IEnumEntry iTriggerModeOn = triggerMode.GetEntryByName("On");
                if (iTriggerModeOn == null || !iTriggerModeOn.IsReadable)
                {
                    Console.WriteLine("Unable to enable trigger mode (entry retrieval). Aborting...");
                    return -1;
                }

                triggerMode.Value = iTriggerModeOn.Value;

                Console.WriteLine("Trigger mode enabled...");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        static int ResetTrigger(INodeMap nodeMap)
        {
            int result = 0;

            try
            {
                // Turn trigger mode back off
                //
                // *** NOTES ***
                // Once all images have been captured, turn trigger mode back
                // off to restore the camera to a clean state.
                IEnum iTriggerMode = nodeMap.GetNode<IEnum>("TriggerMode");
                if (iTriggerMode == null || !iTriggerMode.IsReadable)
                {
                    Console.WriteLine("Unable to disable trigger mode (enum retrieval). Non-fatal error...");
                    return -1;
                }

                IEnumEntry iTriggerModeOff = iTriggerMode.GetEntryByName("Off");
                if (iTriggerModeOff == null || !iTriggerModeOff.IsReadable)
                {
                    Console.WriteLine("Unable to disable trigger mode (entry retrieval). Non-fatal error...");
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
                if (iAcquisitionMode == null || !iAcquisitionMode.IsWritable)
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

                cam.BeginAcquisition();

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
                const int numImages = 10;

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

                for (int imageCnt = 0; imageCnt < numImages; imageCnt++)
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
                                    String filename = "CounterAndTimer-CSharp-";
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

                // Configure trigger
                err = SetupCounterAndTimer(nodeMap);
                if (err < 0)
                {
                    return err;
                }

                // Configure digital IO
                err = ConfigureDigitalIO(nodeMap);
                if (err < 0)
                {
                    return err;
                }

                // Configure Exposure and Trigger
                err = ConfigureExposureAndTrigger(nodeMap);
                if (err < 0)
                {
                    return err;
                }

                // Acquire images
                result = result | AcquireImages(cam, nodeMap, nodeMapTLDevice);

                // Reset trigger
                result = result | ResetTrigger(nodeMap);

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
