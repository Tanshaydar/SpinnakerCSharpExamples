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
 *  @example Sequencer_CSharp.cs
 *
 *  @brief Sequencer_CSharp.cs shows how to use the sequencer to grab images
 *  with various settings. It relies on information provided in the
 *  Enumeration_CSharp, Acquisition_CSharp, and NodeMapInfo_CSharp examples.
 *
 *  It can also be helpful to familiarize yourself with the
 *  ImageFormatControl_CSharp and Exposure_CSharp examples as these examples
 *  provide a strong introduction to camera customization.
 *
 *  The sequencer is another very powerful tool that can be used to create and
 *  store multiple sets of customized image settings. A very useful application
 *  of the sequencer is creating high dynamic range images.
 *
 *  This example is probably the most complex and definitely the longest. As
 *  such, the configuration has been split between three functions. The first
 *  prepares the camera to set the sequences, the second sets the settings for
 *  a single state (it is run five times), and the third configures the
 *  camera to use the sequencer when it acquires images.
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

namespace Sequencer_CSharp
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

        // This function prepares the sequencer to accept custom configurations
        // by ensuring sequencer mode is off (this is a requirement to the
        // enabling of sequencer configuration mode), disabling automatic gain
        // and exposure, and turning sequencer configuration mode on.
        int ConfigureSequencerPartOne(INodeMap nodeMap)
        {
            int result = 0;

            Console.WriteLine("\n*** CONFIGURING SEQUENCER ***\n");

            try
            {
                //
                // Ensure sequencer is off for configuration
                //
                // *** NOTES ***
                // In order to configure a new sequence, sequencer
                // configuration mode needs to be turned on. To do this,
                // sequencer mode must be disabled. However, simply disabling
                // sequencer mode might throw an exception if the current
                // sequence is an invalid configuration.
                //
                // Thus, in order to ensure that sequencer mode is disabled, we
                // first check whether the current sequence is valid. If it
                // isn't, then we know that sequencer mode is off and we can
                // move on; if it is, then we can manually disable sequencer
                // mode.
                //
                // Also note that sequencer configuration mode needs to be off
                // in order to manually disable sequencer mode. It should be
                // off by default, so the example skips checking this.
                //
                // Validate sequencer configuration
                IEnum iSequencerConfigurationValid = nodeMap.GetNode<IEnum>("SequencerConfigurationValid");
                if (iSequencerConfigurationValid == null || !iSequencerConfigurationValid.IsReadable)
                {
                    PrintRetrieveNodeFailure("node", "SequencerConfigurationValid");
                    return -1;
                }

                IEnumEntry iSequencerConfigurationValidYes = iSequencerConfigurationValid.GetEntryByName("Yes");
                if (iSequencerConfigurationValidYes == null || !iSequencerConfigurationValidYes.IsReadable)
                {
                    PrintRetrieveNodeFailure("entry", "SequencerConfigurationValid 'Yes'");
                    return -1;
                }

                // If valid, disable sequencer mode; otherwise, do nothing
                IEnum iSequencerMode = nodeMap.GetNode<IEnum>("SequencerMode");
                if (iSequencerConfigurationValid.Value == iSequencerConfigurationValidYes.Value)
                {
                    if (iSequencerMode == null || !iSequencerMode.IsWritable || !iSequencerMode.IsReadable)
                    {
                        PrintRetrieveNodeFailure("node", "SequencerMode");
                        return -1;
                    }

                    IEnumEntry iSequencerModeOff = iSequencerMode.GetEntryByName("Off");
                    if (iSequencerModeOff == null || !iSequencerModeOff.IsReadable)
                    {
                        PrintRetrieveNodeFailure("entry", "SequencerMode 'Off'");
                        return -1;
                    }

                    iSequencerMode.Value = iSequencerModeOff.Value;
                }

                Console.WriteLine("Sequencer mode disabled...");

                //
                // Turn off automatic exposure
                //
                // *** NOTES ***
                // Automatic exposure prevents the manual configuration of
                // exposure times and needs to be turned off for this example.
                //
                // *** LATER ***
                // Automatic exposure is turned back on at the end of the
                // example in order to restore the camera to its default state.
                //
                IEnum iExposureAuto = nodeMap.GetNode<IEnum>("ExposureAuto");
                if (iExposureAuto == null || !iExposureAuto.IsWritable || !iExposureAuto.IsReadable)
                {
                    PrintRetrieveNodeFailure("node", "ExposureAuto");
                    return -1;
                }

                IEnumEntry iExposureAutoOff = iExposureAuto.GetEntryByName("Off");
                if (iExposureAutoOff == null || !iExposureAutoOff.IsReadable)
                {
                    PrintRetrieveNodeFailure("entry", "ExposureAuto 'Off'");
                    return -1;
                }

                iExposureAuto.Value = iExposureAutoOff.Value;

                Console.WriteLine("Automatic exposure disabled...");

                //
                // Turn off automatic gain
                //
                // *** NOTES ***
                // Automatic gain prevents the manual configuration of gain and
                // needs to be turned off for this example.
                //
                // *** LATER ***
                // Automatic gain is turned back on at the end of the example
                // in order to restore the camera to its default state.
                //
                IEnum iGainAuto = nodeMap.GetNode<IEnum>("GainAuto");
                if (iGainAuto == null || !iGainAuto.IsWritable || !iGainAuto.IsReadable)
                {
                    PrintRetrieveNodeFailure("node", "GainAuto");
                    return -1;
                }

                IEnumEntry iGainAutoOff = iGainAuto.GetEntryByName("Off");
                if (iGainAutoOff == null || !iGainAutoOff.IsReadable)
                {
                    PrintRetrieveNodeFailure("entry", "GainAuto 'Off'");
                    return -1;
                }

                iGainAuto.Value = iGainAutoOff.Value;

                Console.WriteLine("Autoamtic gain disabled...");

                //
                // Turn configuration mode on
                //
                // *** NOTES ***
                // Once sequencer mode is off, enabling sequencer configuration
                // mode allows for the setting of individual sequences.
                //
                // *** LATER ***
                // Before sequencer mode is turned back on, sequencer
                // configuration mode must be turned off.
                //
                IEnum iSequencerConfigurationMode = nodeMap.GetNode<IEnum>("SequencerConfigurationMode");
                if (iSequencerConfigurationMode == null || !iSequencerConfigurationMode.IsWritable || !iSequencerConfigurationMode.IsReadable)
                {
                    PrintRetrieveNodeFailure("node", "SequencerConfigurationMode");
                    return -1;
                }

                IEnumEntry iSequencerConfigurationModeOn = iSequencerConfigurationMode.GetEntryByName("On");
                if (iSequencerConfigurationModeOn == null || !iSequencerConfigurationModeOn.IsReadable)
                {
                    PrintRetrieveNodeFailure("entry", "SequencerConfigurationMode 'On'");
                    return -1;
                }

                iSequencerConfigurationMode.Value = iSequencerConfigurationModeOn.Value;

                Console.WriteLine("Sequencer configuration mode enabled...\n");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function sets a single state. It sets the sequence number,
        // applies custom settings, selects the trigger type and next state
        // number, and saves the state. The custom values that are applied are
        // all calculated in the function that calls this one, RunSingleCamera().
        int SetSingleSequence(
            INodeMap nodeMap,
            int sequenceNumber,
            long widthToSet,
            long heightToSet,
            double exposureTimeToSet,
            double gainToSet)
        {
            int result = 0;

            try
            {
                //
                // Select the current sequence
                //
                // *** NOTES ***
                // Select the index of the sequence to be set.
                //
                // *** LATER ***
                // The next state - i.e. the state to be linked to -
                // also needs to be set before saving the current state.
                //
                IInteger iSequencerSetSelector = nodeMap.GetNode<IInteger>("SequencerSetSelector");
                if (iSequencerSetSelector == null || !iSequencerSetSelector.IsWritable)
                {
                    Console.WriteLine("Unable to select state. Aborting...\n");
                    return -1;
                }

                iSequencerSetSelector.Value = sequenceNumber;

                Console.WriteLine("Setting state {0}...", iSequencerSetSelector.Value);

                //
                // Set desired settings for the current state
                //
                // *** NOTES ***
                // Width, height, exposure time, and gain are set in this
                // example. If the sequencer isn't working properly, it may be
                // important to ensure that each feature is enabled on the
                // sequencer. Features are enabled by default, so this is not
                // explored in this example.
                //
                // Changing the height and width for the sequencer is not
                // available for all camera models.
                //
                // Set width; width recorded in pixels
                IInteger iWidth = nodeMap.GetNode<IInteger>("Width");
                if (iWidth != null && iWidth.IsWritable && iWidth.IsReadable)
                {
                    if (widthToSet % iWidth.Increment != 0)
                    {
                        widthToSet = (widthToSet / iWidth.Increment) * iWidth.Increment;
                    }

                    iWidth.Value = widthToSet;

                    Console.WriteLine("\tWidth set to {0}...", iWidth.Value);
                }
                else
                {
                    Console.WriteLine(
                        "\tUnable to set width; width for sequencer not available on all camera models...");
                }

                // Set height; height recorded in pixels
                IInteger iHeight = nodeMap.GetNode<IInteger>("Height");
                if (iHeight != null && iHeight.IsWritable && iHeight.IsReadable)
                {
                    if (heightToSet % iHeight.Increment != 0)
                    {
                        heightToSet = (heightToSet / iHeight.Increment) * iHeight.Increment;
                    }

                    iHeight.Value = heightToSet;

                    Console.WriteLine("\tHeight set to {0}...", iHeight.Value);
                }
                else
                {
                    Console.WriteLine(
                        "\tUnable to set height; height for sequencer not available on all camera models...");
                }

                // Set exposure time; exposure time recorded in microseconds
                IFloat iExposureTime = nodeMap.GetNode<IFloat>("ExposureTime");
                if (iExposureTime == null || !iExposureTime.IsWritable)
                {
                    Console.WriteLine("Unable to set exposure time. Aborting...\n");
                    return -1;
                }

                iExposureTime.Value = exposureTimeToSet;

                Console.WriteLine("\tExposure time set to {0}...", iExposureTime.Value);

                // Set gain; gain recorded in decibels
                IFloat iGain = nodeMap.GetNode<IFloat>("Gain");
                if (iGain == null || !iGain.IsWritable)
                {
                    Console.WriteLine("Unable to set gain. Aborting...\n");
                    return -1;
                }

                iGain.Value = gainToSet;

                Console.WriteLine("\tGain set to {0}...", iGain.Value);

                //
                // Set the trigger type for the current state
                //
                // *** NOTES ***
                // It is a requirement of every state to have its trigger
                // source set. The trigger source refers to the moment when the
                // sequencer changes from one state to the next.
                //
                IEnum iSequencerTriggerSource = nodeMap.GetNode<IEnum>("SequencerTriggerSource");
                if (iSequencerTriggerSource == null || !iSequencerTriggerSource.IsWritable || !iSequencerTriggerSource.IsReadable)
                {
                    Console.WriteLine("Unable to set trigger source (enum retrieval). Aborting...\n");
                    return -1;
                }

                IEnumEntry iSequencerTriggerSourceFrameStart = iSequencerTriggerSource.GetEntryByName("FrameStart");
                if (iSequencerTriggerSourceFrameStart == null || !iSequencerTriggerSourceFrameStart.IsReadable)
                {
                    Console.WriteLine("Unable to set trigger source (entry retrieval). Aborting...\n");
                    return -1;
                }

                iSequencerTriggerSource.Value = iSequencerTriggerSourceFrameStart.Value;

                Console.WriteLine("\tTrigger source set...");

                //
                // Set the next state in the sequence
                //
                // *** NOTES ***
                // When setting the next state in the sequence, ensure it does
                // not exceed the maximum and that the states loop appropriately.
                //
                const int finalSequenceIndex = 4;

                IInteger iSequencerSetNext = nodeMap.GetNode<IInteger>("SequencerSetNext");
                if (iSequencerSetNext == null || !iSequencerSetNext.IsWritable)
                {
                    Console.WriteLine("Unable to set next state. Aborting...\n");
                    return -1;
                }

                if (sequenceNumber == finalSequenceIndex)
                {
                    iSequencerSetNext.Value = 0;
                }
                else
                {
                    iSequencerSetNext.Value = sequenceNumber + 1;
                }

                Console.WriteLine("\tNext state set to {0}...", iSequencerSetNext.Value);

                //
                // Save current state
                //
                // *** NOTES ***
                // Once all appropriate settings have been configured, make
                // sure to save the state to the sequence. Notice that these
                // settings will be lost when the camera is power-cycled.
                //
                ICommand iSequencerSetSave = nodeMap.GetNode<ICommand>("SequencerSetSave");
                if (iSequencerSetSave == null || !iSequencerSetSave.IsWritable)
                {
                    Console.WriteLine("Unable to save state. Aborting...\n");
                    return -1;
                }

                iSequencerSetSave.Execute();

                Console.WriteLine("\tState {0} saved...\n", sequenceNumber);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // Now that the states have all been set, this function prepares the
        // camera to use the sequencer for acquiring images.
        int ConfigureSequencerPartTwo(INodeMap nodeMap)
        {
            int result = 0;

            try
            {
                //
                // Turn configuration mode off
                //
                // *** NOTES ***
                // Once all desired states have been set, turn sequencer
                // configuration mode off in order to turn sequencer mode on.
                //
                IEnum iSequencerConfigurationMode = nodeMap.GetNode<IEnum>("SequencerConfigurationMode");
                if (iSequencerConfigurationMode == null || !iSequencerConfigurationMode.IsWritable || !iSequencerConfigurationMode.IsReadable)
                {
                    PrintRetrieveNodeFailure("node", "SequencerConfigurationMode");
                    return -1;
                }

                IEnumEntry iSequencerConfigurationModeOff = iSequencerConfigurationMode.GetEntryByName("Off");
                if (iSequencerConfigurationModeOff == null || !iSequencerConfigurationModeOff.IsReadable)
                {
                    PrintRetrieveNodeFailure("entry", "SequencerConfigurationMode 'Off'");
                    return -1;
                }

                iSequencerConfigurationMode.Value = iSequencerConfigurationModeOff.Value;

                Console.WriteLine("Sequencer configuration mode disabled...");

                //
                // Turn sequencer mode on
                //
                // *** NOTES ***
                // After sequencer mode has been turned on, the camera will
                // begin using the saved states in the order that they were set.
                //
                // *** LATER ***
                // Once all images have been captured, disable the sequencer
                // in order to restore the camera to its initial state.
                //
                IEnum iSequencerMode = nodeMap.GetNode<IEnum>("SequencerMode");
                if (iSequencerMode == null || !iSequencerMode.IsWritable || !iSequencerMode.IsReadable)
                {
                    PrintRetrieveNodeFailure("node", "SequencerMode");
                    return -1;
                }

                IEnumEntry iSequencerModeOn = iSequencerMode.GetEntryByName("On");
                if (iSequencerModeOn == null || !iSequencerModeOn.IsReadable)
                {
                    PrintRetrieveNodeFailure("entry", "SequencerMode 'On'");
                    return -1;
                }

                iSequencerMode.Value = iSequencerModeOn.Value;

                Console.WriteLine("Sequencer mode enabled...");

                //
                // Validate sequencer settings
                //
                // *** NOTES ***
                // Once all states have been set, it is a good idea to
                // validate them. Although this node cannot ensure that the
                // states has been set up correctly, it does ensure that the
                // states have been set up in such a way that the camera can
                // function.
                //
                IEnum iSequencerConfigurationValid = nodeMap.GetNode<IEnum>("SequencerConfigurationValid");
                if (iSequencerConfigurationValid == null || !iSequencerConfigurationValid.IsReadable)
                {
                    PrintRetrieveNodeFailure("node", "SequencerConfigurationValid");
                    return -1;
                }

                IEnumEntry iSequencerConfigurationValidYes = iSequencerConfigurationValid.GetEntryByName("Yes");
                if (iSequencerConfigurationValidYes == null || !iSequencerConfigurationValidYes.IsReadable)
                {
                    PrintRetrieveNodeFailure("entry", "SequencerConfigurationValid 'Yes'");
                    return -1;
                }

                if (iSequencerConfigurationValid.Value != iSequencerConfigurationValidYes.Value)
                {
                    Console.WriteLine("Sequencer configuration not valid. Aborting...\n");
                    return -1;
                }

                Console.WriteLine("Sequencer valid.\n");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function restores the camera to its default state by turning
        // sequencer mode off and re-enabling automatic exposure and gain.
        int ResetSequencer(INodeMap nodeMap)
        {
            int result = 0;

            try
            {
                //
                // Turn sequencer mode back off
                //
                // *** NOTES ***
                // Between uses, it is best to disable the sequencer until it
                // is once again required.
                //
                IEnum iSequencerMode = nodeMap.GetNode<IEnum>("SequencerMode");
                if (iSequencerMode == null || !iSequencerMode.IsWritable || !iSequencerMode.IsReadable)
                {
                    PrintRetrieveNodeFailure("node", "SequencerMode");
                    return -1;
                }

                IEnumEntry iSequencerModeOff = iSequencerMode.GetEntryByName("Off");
                if (iSequencerModeOff == null || !iSequencerModeOff.IsReadable)
                {
                    PrintRetrieveNodeFailure("entry", "SequencerMode 'Off'");
                    return -1;
                }

                iSequencerMode.Value = iSequencerModeOff.Value;

                Console.WriteLine("Sequencer mode disabled...");

                //
                // Turn automatic exposure back on
                //
                // *** NOTES ***
                // It is recommended to have automatic exposure enabled
                // whenever manual exposure settings are not required.
                //
                IEnum iExposureAuto = nodeMap.GetNode<IEnum>("ExposureAuto");
                if (iExposureAuto == null || !iExposureAuto.IsWritable || !iExposureAuto.IsReadable)
                {
                    PrintRetrieveNodeFailure("node", "ExposureAuto");
                    return -1;
                }

                IEnumEntry iExposureAutoContinuous = iExposureAuto.GetEntryByName("Continuous");
                if (iExposureAutoContinuous == null || !iExposureAutoContinuous.IsReadable)
                {
                    PrintRetrieveNodeFailure("entry", "ExposureAuto Continuous");
                    return -1;
                }

                iExposureAuto.Value = iExposureAutoContinuous.Value;

                Console.WriteLine("Automatic exposure enabled...");

                //
                // Turn automatic gain back on
                //
                // *** NOTES ***
                // It is recommended to have automatic gain enabled whenever
                // manual gain settings are not required.
                //
                IEnum iGainAuto = nodeMap.GetNode<IEnum>("GainAuto");
                if (iGainAuto == null || !iGainAuto.IsWritable || !iGainAuto.IsReadable)
                {
                    PrintRetrieveNodeFailure("node", "GainAuto");
                    return -1;
                }

                IEnumEntry iGainAutoContinuous = iGainAuto.GetEntryByName("Continuous");
                if (iGainAutoContinuous == null || !iGainAutoContinuous.IsReadable)
                {
                    PrintRetrieveNodeFailure("entry", "GainAuto Continuous");
                    return -1;
                }

                iGainAuto.Value = iGainAutoContinuous.Value;

                Console.WriteLine("Automatic gain enabled...\n");
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
        static int AcquireImages(IManagedCamera cam, INodeMap nodeMap, INodeMap nodeMapTLDevice, ulong timeout)
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
                        "Unable to set acquisition mode to continuous (enum entry retrieval). Aborting...\n");
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
                        // Retrieve next received image and ensure image
                        // completion
                        using(IManagedImage rawImage = cam.GetNextImage(timeout))
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
                                    String filename = "Sequencer-CSharp-";
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

        // This function acts very similarly to the RunSingleCamera() functions
        // of other examples, except that the values for the states are also
        // calculated here; please see NodeMapInfo_CSharp example for more
        // in-depth comments on setting up cameras.
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

                // Configure sequencer to be ready to set sequences
                err = ConfigureSequencerPartOne(nodeMap);
                if (err < 0)
                {
                    return err;
                }

                //
                // Set states
                //
                // *** NOTES ***
                // In the following section, the sequencer values are
                // calculated. This section does not appear in the
                // configuration, as the values calculated are somewhat
                // arbitrary: width and height are both set to 25% of their
                // maximum values, incrementing by 10%; exposure time is set to
                // its minimum, also incrementing by 10% of its maximum; and
                // gain is set to its minimum, incrementing by 2% of its
                // maximum.
                //
                const int NumSequences = 5;

                // Retrieve maximum width
                IInteger iWidth = nodeMap.GetNode<IInteger>("Width");
                if (iWidth == null || !iWidth.IsReadable)
                {
                    Console.WriteLine("Unable to retrieve maximum width. Aborting...\n");
                    return -1;
                }

                long widthMax = iWidth.Max;

                // Retrieve maximum height
                IInteger iHeight = nodeMap.GetNode<IInteger>("Height");
                if (iHeight == null || !iHeight.IsReadable)
                {
                    Console.WriteLine("Unable to retrieve maximum height. Aborting...\n");
                    return -1;
                }

                long heightMax = iHeight.Max;

                // Retrieve maximum exposure time
                const double ExposureTimeMaxToSet = 2000000;

                IFloat iExposureTime = nodeMap.GetNode<IFloat>("ExposureTime");
                if (iExposureTime == null || !iExposureTime.IsReadable)
                {
                    Console.WriteLine("Unable to retrieve maximum exposure time. Aborting...\n");
                    return -1;
                }

                double exposureTimeMax = iExposureTime.Max;

                if (exposureTimeMax > ExposureTimeMaxToSet)
                {
                    exposureTimeMax = ExposureTimeMaxToSet;
                }

                // Retrieve maximum gain
                IFloat iGain = nodeMap.GetNode<IFloat>("Gain");
                if (iGain == null || !iGain.IsReadable)
                {
                    Console.WriteLine("Unable to retrieve maximum gain. Aborting...\n");
                    return -1;
                }

                double gainMax = iGain.Max;

                // Set initial values
                long widthToSet = widthMax / 4;
                long heightToSet = heightMax / 4;
                double exposureTimeToSet = iExposureTime.Min;
                double gainToSet = iGain.Min;

                for (int sequenceNumber = 0; sequenceNumber < NumSequences; sequenceNumber++)
                {
                    err = SetSingleSequence(
                        nodeMap, sequenceNumber, widthToSet, heightToSet, exposureTimeToSet, gainToSet);
                    if (err < 0)
                    {
                        return err;
                    }

                    // Increment values
                    widthToSet += widthMax / 10;
                    heightToSet += heightMax / 10;
                    exposureTimeToSet += exposureTimeMax / 10.0;
                    gainToSet += gainMax / 50.0;
                }

                // Calculate appropriate acquisition grab timeout window based on exposure time
                // Note: exposureTimeToSet is in microseconds and needs to be converted to milliseconds
                ulong timeout = ((ulong) exposureTimeToSet / 1000) + 1000;

                // Configure sequencer to acquire images
                err = ConfigureSequencerPartTwo(nodeMap);
                if (err < 0)
                {
                    return err;
                }

                // Acquire images
                result = result | AcquireImages(cam, nodeMap, nodeMapTLDevice, timeout);

                // Reset sequencer
                result = result | ResetSequencer(nodeMap);

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
