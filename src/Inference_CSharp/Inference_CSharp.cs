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
 *  @example Inference_CSharp.cs
 *
 *  @brief Inference_CSharp.cs shows how to perform the following:
 *  - Upload custom inference neural networks to the camera (DDR or Flash)
 *  - Inject sample test image
 *  - Enable/Configure chunk data
 *  - Enable/Configure trigger inference ready sync
 *  - Acquire images
 *  - Display inference data from acquired image chunk data
 *  - Disable previously configured camera configurations
 *
 *  Inference is only available for Firefly deep learning cameras.
 *  See the related content section on the Firefly DL product page for relevant
 *  documentation.
 *
 *  https://www.flir.com/products/firefly-dl/
 *
 *  It can also be helpful to familiarize yourself with the ChunkData example.
 *  
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics
 */

using SpinnakerNET;
using SpinnakerNET.GenApi;
using System;
using System.IO;

namespace Inference_CSharp
{
    class Program
    {
        // Use the following enum and global constant to select whether inference network
        // type is Detection or Classification.
        enum InferenceNetworkType
        {
            /**
             * This network determines the  most likely class given a set of predetermined,
             * trained options. Object detection can also provide a location within the
             * image (in the form of a "bounding box" surrounding the class), and can
             * detect multiple objects.
             */
            Detection,
            /**
             * This network determines the best option from a list of predetermined options;
             * the camera gives a percentage that determines the likelihood of the currently
             * perceived image being one of the classes it has been trained to recognize.
             */
            Classification
        }

        static InferenceNetworkType chosenInferenceNetworkType = InferenceNetworkType.Detection;

        // Use the following enum and global constant to select whether uploaded inference
        // network and injected image should be written to camera flash or DDR
        enum FileUploadPersistence
        {
            FLASH, // Slower upload but data persists after power cycling the camera
            DDR    // Faster upload but data clears after power cycling the camera
        }
        ;

        const FileUploadPersistence chosenFileUploadPersistence = FileUploadPersistence.DDR;

        // The example provides two existing custom networks that can be uploaded
        // on to the camera to demonstrate classification and detection capabilities.
        // "Network_Classification" file is created with Tensorflow using a mobilenet
        // neural network for classifying flowers.
        // "Network_Detection" file is created with Caffe using mobilenet SSD network
        // for people object detection.
        // Note: Make sure these files exist on the system and are accessible by the example
        string networkFilePath =
            (chosenInferenceNetworkType == InferenceNetworkType.Classification ? "Network_Classification"
             : "Network_Detection");

        // The example provides two raw images that can be injected into the camera
        // to demonstrate camera inference classification and detection capabilities. Jpeg
        // representation of the raw images can be found packaged with the example with
        // the names "Injected_Image_Classification_Daisy.jpg" and "Injected_Image_Detection_Aeroplane.jpg".
        // Note: Make sure these files exist on the system and are accessible by the example
        string injectedImageFilePath =
            (chosenInferenceNetworkType == InferenceNetworkType.Classification ? "Injected_Image_Classification.raw"
             : "Injected_Image_Detection.raw");

        // The injected images have different ROI sizes so the camera needs to be
        // configured to the appropriate width and height to match the injected image
        static long injectedImageWidth = (chosenInferenceNetworkType == InferenceNetworkType.Classification ? 640 : 720);
        static long injectedImageHeight =
            (chosenInferenceNetworkType == InferenceNetworkType.Classification ? 400 : 540);

        // The sample classification inference network file was trained with the following
        // data set labels
        // Note: This list should match the list of labels used during the training
        //       stage of the network file
        static string[] labelClassification = {"daisy", "dandelion", "roses", "sunflowers", "tulips"};

        // The sample detection inference network file was trained with the following
        // data set labels
        // Note: This list should match the list of labels used during the training
        //       stage of the network file
        static string[] labelDetection = {
            "background", "aeroplane", "bicycle",     "bird",  "boat",        "bottle", "bus",
            "car",        "cat",       "chair",       "cow",   "diningtable", "dog",    "horse",
            "motorbike",  "person",    "pottedplant", "sheep", "sofa",        "train",  "monitor"};

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

        // This function executes a file delete operation on the camera.
        static int CameraDeleteFile(INodeMap nodeMap)
        {
            int result = 0;

            IInteger iFileSize = nodeMap.GetNode<IInteger>("FileSize");
            if (iFileSize == null || !iFileSize.IsReadable)
            {
                Console.WriteLine("Unable to query FileSize. Aborting...");
                return -1;
            }

            if (iFileSize.Value == 0)
            {
                // No file uploaded yet, skip delete
                Console.WriteLine("No files found, skipping file deletion.");
                return 0;
            }

            Console.WriteLine("Deleting file...");
            try
            {
                // Get FileOperationSelector
                IEnum iFileSelectorNode = nodeMap.GetNode<IEnum>("FileOperationSelector");
                if (iFileSelectorNode == null || !iFileSelectorNode.IsWritable || !iFileSelectorNode.IsReadable)
                {
                    Console.WriteLine("Unable to configure FileOperationSelector. Aborting...");
                    return -1;
                }

                IEnumEntry iFileOperationDelete = iFileSelectorNode.GetEntryByName("Delete");
                if (iFileOperationDelete == null || !iFileOperationDelete.IsReadable)
                {
                    Console.WriteLine("Unable to configure FileOperationSelector Delete. Aborting...");
                    return -1;
                }

                iFileSelectorNode.Value = iFileOperationDelete.Symbolic;

                ICommand iFileOperationExecute = nodeMap.GetNode<ICommand>("FileOperationExecute");
                if (iFileOperationExecute == null || !iFileOperationExecute.IsWritable)
                {
                    Console.WriteLine("Unable to configure FileOperationExecute. Aborting...");
                    return -1;
                }

                iFileOperationExecute.Execute();

                IEnum iFileOperationStatus = nodeMap.GetNode<IEnum>("FileOperationStatus");
                if (iFileOperationStatus == null || !iFileOperationStatus.IsReadable)
                {
                    Console.WriteLine("Unable to query FileOperationStatus. Aborting...");
                    return -1;
                }

                IEnumEntry iFileOperationStatusSuccess = iFileOperationStatus.GetEntryByName("Success");
                if (iFileOperationStatusSuccess == null || !iFileOperationStatusSuccess.IsReadable)
                {
                    Console.WriteLine("Unable to query FileOperationStatus Success. Aborting...");
                    return -1;
                }

                if (iFileOperationStatus.Value != iFileOperationStatusSuccess.Value)
                {
                    Console.WriteLine(
                        "Failed to delete file!  File Operation Status : {0}.",
                        iFileOperationStatus.Symbolics[iFileOperationStatus.Value]);
                    return -1;
                }
            }
            catch (SpinnakerException e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.Message);
                result = -1;
            }

            return result;
        }

        // This function executes file open/write on the camera, sets the uploaded file persistence
        // and attempts to set FileAccessLength to FileAccessBufferNode length to speed up the write.
        static int CameraOpenFile(INodeMap nodeMap)
        {
            int result = 0;

            Console.WriteLine("Opening file for writing...");
            try
            {
                IEnum iFileOperationSelector = nodeMap.GetNode<IEnum>("FileOperationSelector");
                if (iFileOperationSelector == null || !iFileOperationSelector.IsWritable || !iFileOperationSelector.IsReadable)
                {
                    Console.WriteLine("Unable to configure FileOperationSelector. Aborting...");
                    return -1;
                }

                IEnumEntry iFileOperationOpen = iFileOperationSelector.GetEntryByName("Open");
                if (iFileOperationOpen == null || !iFileOperationOpen.IsReadable)
                {
                    Console.WriteLine("Unable to configure FileOperationSelector Open. Aborting...");
                    return -1;
                }

                iFileOperationSelector.Value = iFileOperationOpen.Symbolic;

                IEnum iFileOpenMode = nodeMap.GetNode<IEnum>("FileOpenMode");
                if (iFileOpenMode == null || !iFileOpenMode.IsWritable || !iFileOpenMode.IsReadable)
                {
                    Console.WriteLine("Unable to configure FileOpenMode. Aborting...");
                    return -1;
                }

                IEnumEntry iFileFileOpenModeWrite = iFileOpenMode.GetEntryByName("Write");
                if (iFileFileOpenModeWrite == null || !iFileFileOpenModeWrite.IsReadable)
                {
                    Console.WriteLine("Unable to query FileOperationSelector Write. Aborting...");
                    return -1;
                }

                iFileOpenMode.Value = iFileFileOpenModeWrite.Symbolic;

                ICommand iFileOperationExecute = nodeMap.GetNode<ICommand>("FileOperationExecute");
                if (iFileOperationExecute == null || !iFileOperationExecute.IsWritable)
                {
                    Console.WriteLine("Unable to execute FileOperationExecute - Write. Aborting...");
                    return -1;
                }

                iFileOperationExecute.Execute();

                IEnum iFileOperationStatus = nodeMap.GetNode<IEnum>("FileOperationStatus");
                if (iFileOperationStatus == null || !iFileOperationStatus.IsReadable)
                {
                    Console.WriteLine("Unable to query iFileOperationStatus. Aborting...");
                    return -1;
                }

                IEnumEntry iFileOperationStatusSuccess = iFileOperationStatus.GetEntryByName("Success");
                if (iFileOperationStatusSuccess == null || !iFileOperationStatusSuccess.IsReadable)
                {
                    Console.WriteLine("Unable to query iFileOperationStatus Success. Aborting...");
                    return -1;
                }

                if (iFileOperationStatus.Value != iFileOperationStatusSuccess.Value)
                {
                    Console.WriteLine("Failed to open file for writing!");
                    return -1;
                }

                IBool iFileWriteToFlash = nodeMap.GetNode<IBool>("FileWriteToFlash");
                if (iFileWriteToFlash == null || !iFileWriteToFlash.IsWritable || !iFileWriteToFlash.IsReadable)
                {
                    Console.WriteLine("Unable to configure FileWriteToFlash. Aborting...");
                    return -1;
                }

                iFileWriteToFlash.Value = (chosenFileUploadPersistence == FileUploadPersistence.FLASH);
                Console.WriteLine("FileWriteToFlash is set to {0}.", iFileWriteToFlash.Value);

                IInteger iFileAccessLength = nodeMap.GetNode<IInteger>("FileAccessLength");
                if (iFileAccessLength == null || !iFileAccessLength.IsWritable)
                {
                    Console.WriteLine("Unable to configure FileAccessLength. Aborting...");
                    return -1;
                }

                IRegister iFileAccessBuffer = nodeMap.GetNode<IRegister>("FileAccessBuffer");
                if (iFileAccessBuffer == null || !iFileAccessBuffer.IsReadable)
                {
                    Console.WriteLine("Unable to query iFileAccessBuffer. Aborting...");
                    return -1;
                }

                if (iFileAccessLength.Value < iFileAccessBuffer.Length)
                {
                    try
                    {
                        iFileAccessLength.Value = iFileAccessBuffer.Length;
                    }
                    catch (SpinnakerException e)
                    {
                        Console.WriteLine("Unable to set FileAccessLength to FileAccessBuffer length: {0}.", e.Message);
                    }
                }

                // Set File Access Offset to zero
                IInteger iFileAccessOffset = nodeMap.GetNode<IInteger>("FileAccessOffset");
                if (iFileAccessOffset == null || !iFileAccessOffset.IsWritable)
                {
                    Console.WriteLine("Unable to configure FileWriteToFlash. Aborting...");
                    return -1;
                }

                iFileAccessOffset.Value = 0;
            }
            catch (SpinnakerException e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.Message);
                result = -1;
            }

            return result;
        }

        // This function executes a file close operation on the camera.
        static int CameraCloseFile(INodeMap nodeMap)
        {
            int result = 0;

            Console.WriteLine("Closing file...");
            try
            {
                IEnum iFileOperationSelector = nodeMap.GetNode<IEnum>("FileOperationSelector");
                if (iFileOperationSelector == null || !iFileOperationSelector.IsWritable || !iFileOperationSelector.IsReadable)
                {
                    Console.WriteLine("Unable to configure FileOperationSelector. Aborting...");
                    return -1;
                }

                IEnumEntry iFileOperationClose = iFileOperationSelector.GetEntryByName("Close");
                if (iFileOperationClose == null || !iFileOperationClose.IsReadable)
                {
                    Console.WriteLine("Unable to query FileOperationSelector Close. Aborting...");
                    return -1;
                }

                iFileOperationSelector.Value = iFileOperationClose.Symbolic;

                ICommand iFileOperationExecute = nodeMap.GetNode<ICommand>("FileOperationExecute");
                if (iFileOperationExecute == null || !iFileOperationExecute.IsWritable)
                {
                    Console.WriteLine("Unable to execute File Close. Aborting...");
                    return -1;
                }

                iFileOperationExecute.Execute();

                IEnum iFileOperationStatus = nodeMap.GetNode<IEnum>("FileOperationStatus");
                if (iFileOperationStatus == null || !iFileOperationStatus.IsReadable)
                {
                    Console.WriteLine("Unable to query FileOperationStatus. Aborting...");
                    return -1;
                }

                IEnumEntry iFileOperationStatusSuccess = iFileOperationStatus.GetEntryByName("Success");
                if (iFileOperationStatusSuccess == null || !iFileOperationStatus.IsReadable)
                {
                    Console.WriteLine("Unable to query FileOperationStatus - Success. Aborting...");
                    return -1;
                }

                if (iFileOperationStatus.Value != iFileOperationStatusSuccess.Value)
                {
                    Console.Write("Failed to write to file!");
                    return -1;
                }
            }
            catch (SpinnakerException e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.Message);
                result = -1;
            }

            return result;
        }

        static int UploadFileToCamera(INodeMap nodeMap, string fileSelectorEntryName, string filePath)
        {
            int result = 0;
            Console.WriteLine("\n*** CONFIGURING FILE SELECTOR ***");

            try
            {
                IEnum iFileSelector = nodeMap.GetNode<IEnum>("FileSelector");
                if (iFileSelector == null || !iFileSelector.IsWritable || !iFileSelector.IsReadable)
                {
                    Console.WriteLine("Unable to configure FileSelector. Aborting...");
                    return -1;
                }

                IEnumEntry iInferenceSelectorEntry = iFileSelector.GetEntryByName(fileSelectorEntryName);
                if (iInferenceSelectorEntry == null || !iInferenceSelectorEntry.IsReadable)
                {
                    Console.WriteLine("Unable to query FileSelector entry {0}. Aborting...", fileSelectorEntryName);
                    return -1;
                }

                // Set file selector
                Console.WriteLine("Setting FileSelector to {0}...", iInferenceSelectorEntry.Symbolic);
                iFileSelector.Value = iInferenceSelectorEntry.Symbolic;

                // Delete file on camera before writing in case camera runs out of space
                if (CameraDeleteFile(nodeMap) != 0)
                {
                    Console.WriteLine(
                        "Failed to delete existing file for selector entry {0}. Aborting...", iInferenceSelectorEntry);
                    return -1;
                }

                // Open file on camera for write.
                if (CameraOpenFile(nodeMap) != 0)
                {
                    // File may not be closed properly last time
                    // Close and re-open again
                    if (CameraCloseFile(nodeMap) != 0)
                    {
                        // It fails to close the file.  Abort!
                        Console.WriteLine("Problem opening file node.  Aborting...");
                        return -1;
                    }

                    // File was closed.  Retry writing the file again.
                    if (CameraOpenFile(nodeMap) != 0)
                    {
                        // Fails again.  Abort!
                        Console.WriteLine("Problem opening file node.  Aborting...");
                        return -1;
                    }
                }

                IInteger iFileAccessLength = nodeMap.GetNode<IInteger>("FileAccessLength");
                if (iFileAccessLength == null || !iFileAccessLength.IsWritable)
                {
                    Console.WriteLine("Unable to query FileAccessLength. Aborting...");
                    return -1;
                }

                IInteger iFileAccessOffset = nodeMap.GetNode<IInteger>("FileAccessOffset");
                if (iFileAccessOffset == null || !iFileAccessOffset.IsReadable)
                {
                    Console.WriteLine("Unable to query FileAccessOffset. Aborting...");
                    return -1;
                }

                IInteger iFileOperationResult = nodeMap.GetNode<IInteger>("FileOperationResult");
                if (iFileOperationResult == null || !iFileOperationResult.IsReadable)
                {
                    Console.WriteLine("Unable to query FileOperationResult. Aborting...");
                    return -1;
                }

                IRegister iFileAccessBuffer = nodeMap.GetNode<IRegister>("FileAccessBuffer");
                if (iFileAccessBuffer == null || !iFileAccessBuffer.IsWritable)
                {
                    Console.WriteLine("Unable to configure FileAccessBuffer. Aborting...");
                    return -1;
                }

                // Read file to memory
                byte[] fileBytes = File.ReadAllBytes(filePath);

                // Compute number of write operations required
                long totalBytesToWrite = fileBytes.LongLength;
                long intermediateBufferSize = iFileAccessLength.Value;
                long writeIterations = (totalBytesToWrite / intermediateBufferSize) +
                                       (totalBytesToWrite % intermediateBufferSize == 0 ? 0 : 1);

                if (totalBytesToWrite == 0)
                {
                    Console.WriteLine("Empty Image. No data will be written to camera. Aborting...");
                    return -1;
                }

                Console.WriteLine("Starting uploading \"{0}\" to device", filePath);
                Console.WriteLine("Total Bytes to write : {0}", totalBytesToWrite);
                Console.WriteLine("FileAccessLength : {0}", intermediateBufferSize);
                Console.WriteLine("Write Iterations : {0}", writeIterations);

                long index = 0;
                long bytesLeftToWrite = totalBytesToWrite;
                long totalBytesWritten = 0;
                bool paddingRequired = false;
                long numPaddings = 0;

                IEnum iFileOperationSelector = nodeMap.GetNode<IEnum>("FileOperationSelector");
                if (iFileOperationSelector == null || !iFileOperationSelector.IsWritable)
                {
                    Console.WriteLine("Unable to configure FileOperationSelector. Aborting...");
                    return -1;
                }

                ICommand iFileOperationExecute = nodeMap.GetNode<ICommand>("FileOperationExecute");
                if (iFileOperationExecute == null || !iFileOperationExecute.IsWritable)
                {
                    Console.WriteLine("Unable to configure FileOperationExecute. Aborting...");
                    return -1;
                }

                IEnum iFileOperationStatus = nodeMap.GetNode<IEnum>("FileOperationStatus");
                if (iFileOperationStatus == null || !iFileOperationStatus.IsReadable)
                {
                    Console.WriteLine("Unable to query FileOperationStatus. Aborting...");
                    return -1;
                }

                IEnumEntry iFileOperationSuccess = iFileOperationStatus.GetEntryByName("Success");
                if (iFileOperationSuccess == null || !iFileOperationSuccess.IsReadable)
                {
                    Console.WriteLine("Unable to query FileOperationStatus Success. Aborting...");
                    return -1;
                }

                for (long i = 0; i < writeIterations; i++)
                {
                    if (intermediateBufferSize > bytesLeftToWrite)
                    {
                        // Check for multiple of 4 bytes
                        long remainder = bytesLeftToWrite % 4;
                        if (remainder != 0)
                        {
                            paddingRequired = true;
                            numPaddings = 4 - remainder;
                        }
                    }

                    // Extract bytes from data array
                    byte[] tempData = new byte
                        [intermediateBufferSize <= bytesLeftToWrite ?(int)
                            intermediateBufferSize:(int)(bytesLeftToWrite + numPaddings)];

                    Array.Copy(fileBytes,
                               index,
                               tempData,
                               0,
                               intermediateBufferSize <= bytesLeftToWrite ?(int) intermediateBufferSize
                               : (int) bytesLeftToWrite);

                    if (paddingRequired)
                    {
                        // Fill padded bytes
                        for (long j = 0; j < numPaddings; j++)
                        {
                            tempData[bytesLeftToWrite + j] = 0xFF;
                        }
                    }

                    // Update index for next write iteration
                    index = index + (intermediateBufferSize <= bytesLeftToWrite ? intermediateBufferSize
                                     : bytesLeftToWrite);

                    // Write to AccessBufferNode
                    iFileAccessBuffer.Write(tempData);

                    if (intermediateBufferSize > bytesLeftToWrite)
                    {
                        // Update file Access Length Node;
                        // otherwise, garbage data outside the range
                        // would be written to device.
                        iFileAccessLength.Value = bytesLeftToWrite;
                    }

                    // Do Write command
                    IEnumEntry iFileOperationWrite = iFileOperationSelector.GetEntryByName("Write");
                    if (iFileOperationWrite == null || !iFileOperationWrite.IsReadable)
                    {
                        Console.WriteLine("Unable to configure FileOperationSelector Write. Aborting...");
                        return -1;
                    }

                    iFileOperationSelector.Value = iFileOperationWrite.Symbolic;
                    iFileOperationExecute.Execute();

                    if (iFileOperationStatus.Value != iFileOperationSuccess.Value)
                    {
                        Console.WriteLine(
                            "Failed to upload file!  File Operation Status : {0}.",
                            iFileOperationStatus.Symbolics[iFileOperationStatus.Value]);
                        return -1;
                    }

                    // Verify size of bytes written
                    long sizeWritten = iFileOperationResult.Value;

                    // Log current file access offset
                    // Keep track of total bytes written
                    totalBytesWritten += sizeWritten;

                    // Keep track of bytes left to write
                    bytesLeftToWrite = fileBytes.Length - totalBytesWritten;

                    Console.Write("Progress: {0}%\r", (i * 100 / writeIterations));
                }

                Console.Write("Writing complete");

                // Clear progress message from console
                Console.WriteLine("\r");

                // Close the file
                Console.WriteLine("Closing file...");
                IEnumEntry iFileOperationClose = iFileOperationSelector.GetEntryByName("Close");
                if (iFileOperationClose == null || !iFileOperationClose.IsReadable)
                {
                    Console.WriteLine("Unable to query FileOperation - Close. Aborting...");
                    return -1;
                }

                iFileOperationSelector.Value = iFileOperationClose.Symbolic;
                iFileOperationExecute.Execute();

                if (iFileOperationStatus.Value != iFileOperationSuccess.Value)
                {
                    Console.WriteLine(
                        "Failed to write file!  File Operation Status : {0}.",
                        iFileOperationStatus.Symbolics[iFileOperationStatus.Value]);
                    return -1;
                }

                StringReg iInterfaceNetworkName = nodeMap.GetNode<StringReg>("InferenceNetworkName");
                if (iInterfaceNetworkName == null || !iInterfaceNetworkName.IsWritable)
                {
                    Console.WriteLine("Unable to write to InferenceNetworkName. Aborting...");
                    return -1;
                }

                iInterfaceNetworkName.Value = chosenInferenceNetworkType == InferenceNetworkType.Detection ?
                                                                            "Network_Detection"
                    : "Network_Classification";
            }
            catch (SpinnakerException e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.Message);
                result = -1;
            }

            return result;
        }

        // This function deletes the file uploaded to the camera given the selected
        // file selector entry.
        static int DeleteFileOnCamera(INodeMap nodeMap, string fileSelectorEntryName)
        {
            Console.WriteLine("\n*** CLEANING UP FILE SELECTOR ***");

            IEnum ptrFileSelector = nodeMap.GetNode<IEnum>("FileSelector");
            if (!ptrFileSelector.IsWritable || !ptrFileSelector.IsReadable)
            {
                Console.WriteLine("Unable to configure FileSelector. Aborting...");
                return -1;
            }

            IEnumEntry ptrInferenceSelectorEntry = ptrFileSelector.GetEntryByName(fileSelectorEntryName);
            if (!ptrInferenceSelectorEntry.IsReadable)
            {
                Console.WriteLine("Unable to query FileSelector entry {0}. Aborting...", fileSelectorEntryName);
                return -1;
            }

            // Set file selector to entry
            Console.WriteLine("Setting FileSelector to  {0}", ptrInferenceSelectorEntry.Symbolic);

            ptrFileSelector.Value = ptrInferenceSelectorEntry.Value;

            // Delete file on camera before writing in case camera runs out of space
            if (CameraDeleteFile(nodeMap) != 0)
            {
                Console.WriteLine(
                    "Failed to delete existing file for selector entry {0}. Aborting...", ptrInferenceSelectorEntry);
                return -1;
            }

            return 0;
        }

        // This function enables or disables the given chunk data type based on
        // the specified entry name.
        static int SetChunkEnable(INodeMap nodeMap, string entryName, bool enable)
        {
            int result = 0;
            IEnum iChunkSelector = nodeMap.GetNode<IEnum>("ChunkSelector");

            IEnumEntry iEntry = iChunkSelector.GetEntryByName(entryName);
            if (iEntry == null || !iEntry.IsReadable)
            {
                return -1;
            }

            IBool iChunkEnable = nodeMap.GetNode<IBool>("ChunkEnable");
            if (iChunkEnable == null)
            {
                Console.WriteLine("{0} not available", entryName);
                return -1;
            }
            if (enable)
            {
                if (iChunkEnable.Value)
                {
                    Console.WriteLine("{0} enabled", entryName);
                }
                else if (iChunkEnable.IsWritable)
                {
                    iChunkEnable.Value = true;
                    Console.WriteLine("{0} enabled", entryName);
                }
                else
                {
                    Console.WriteLine("{0} not writable", entryName);
                    result = -1;
                }
            }
            else
            {
                if (!iChunkEnable.Value)
                {
                    Console.WriteLine("{0} disabled", entryName);
                }
                else if (iChunkEnable.IsWritable)
                {
                    iChunkEnable.Value = false;
                    Console.WriteLine("{0} disabled", entryName);
                }
                else
                {
                    Console.WriteLine("{0} not writable", entryName);
                    result = -1;
                }
            }

            return result;
        }

        // This function configures the camera to add chunk data to each image.
        // It does this by enabling each type of chunk data after enabling
        // chunk data mode. When chunk data mode is turned on, the data is made
        // available in both the nodemap and each image.
        static int ConfigureChunkData(INodeMap nodeMap)
        {
            int result = 0;

            Console.WriteLine("\n*** CONFIGURING CHUNK DATA ***");

            try
            {

                // Activate chunk mode
                //
                // *** NOTES ***
                // Once enabled, chunk data will be available at the end of the
                // payload of every image captured until it is disabled. Chunk
                // data can also be retrieved from the nodemap.
                //
                IBool iChunkModeActive = nodeMap.GetNode<IBool>("ChunkModeActive");
                if (iChunkModeActive == null || !iChunkModeActive.IsWritable)
                {
                    Console.WriteLine("Cannot active chunk mode. Aborting...");
                    return -1;
                }

                iChunkModeActive.Value = true;

                Console.WriteLine("Chunk mode activated...");

                // Enable inference related chunks in chunk data

                // Enable chunk data inference Frame Id
                result = SetChunkEnable(nodeMap, "InferenceFrameId", true);
                if (result == -1)
                {
                    Console.WriteLine("Unable to enable Inference Frame Id chunk data.  Aborting...");
                    return result;
                }

                if (chosenInferenceNetworkType == InferenceNetworkType.Detection)
                {
                    // Enable chunk data inference bounding box for Detection
                    result = SetChunkEnable(nodeMap, "InferenceBoundingBoxResult", true);
                    if (result == -1)
                    {
                        Console.WriteLine("Unable to enable Inference Bounding Box chunk data.  Aborting...");
                        return result;
                    }
                }
                else
                {
                    // Enable chunk data inference result for Classification
                    result = SetChunkEnable(nodeMap, "InferenceResult", true);
                    if (result == -1)
                    {
                        Console.WriteLine("Unable to enable Inference Result chunk data.  Aborting...");
                        return result;
                    }

                    // Enable chunk data inference confidence for Classification
                    result = SetChunkEnable(nodeMap, "InferenceConfidence", true);
                    if (result == -1)
                    {
                        Console.WriteLine("Unable to enable Inference Confidence chunk data.  Aborting...");
                        return result;
                    }
                }

                Console.WriteLine();
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function disables each type of chunk data before disabling chunk data mode.
        static int DisableChunkData(INodeMap nodeMap)
        {
            Console.WriteLine("*** DISABLING CHUNK DATA ***");

            int result = 0;

            try
            {
                result = SetChunkEnable(nodeMap, "InferenceFrameId", false);
                if (result == -1)
                {
                    Console.WriteLine("Unable to disable Inference Frame Id chunk data. Aborting...");
                    return result;
                }

                if (chosenInferenceNetworkType == InferenceNetworkType.Detection)
                {
                    // Disable chunk data for Detection
                    result = SetChunkEnable(nodeMap, "InferenceBoundingBoxResult", false);
                    if (result == -1)
                    {
                        Console.WriteLine("Unable to disable Inference Bounding Box chunk data.  Aborting...");
                        return result;
                    }
                }
                else
                {
                    // Disable chunk data for Classification
                    result = SetChunkEnable(nodeMap, "InferenceResult", false);
                    if (result == -1)
                    {
                        Console.WriteLine("Unable to disable Inference Result chunk data.  Aborting...");
                        return result;
                    }

                    result = SetChunkEnable(nodeMap, "InferenceConfidence", false);
                    if (result == -1)
                    {
                        Console.WriteLine("Unable to disable Inference Confidence chunk data.  Aborting...");
                        return result;
                    }
                }

                // Deactivate ChunkMode
                IBool iChunkModeActive = nodeMap.GetNode<IBool>("ChunkModeActive");
                if (iChunkModeActive == null || !iChunkModeActive.IsWritable)
                {
                    Console.WriteLine("Cannot deactivate chunk mode. Aborting...");
                    result = -1;
                }

                iChunkModeActive.Value = false;

                Console.WriteLine("Chunk mode deactivated...");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function displays the inference-related chunk data from the image.
        static int DisplayChunkData(IManagedImage managedImage)
        {
            int result = 0;

            Console.WriteLine("Printing chunk data from image...");

            try
            {
                //
                // Retrieve chunk data from image
                //
                // *** NOTES ***
                // When retrieving chunk data from an image, the data is stored
                // in a a ChunkData object and accessed with getter functions.
                //
                ManagedChunkData managedChunkData = managedImage.ChunkData;

                long inferenceFrameID = managedChunkData.FrameID;
                Console.WriteLine("\tInference frame ID: {0}", inferenceFrameID);

                if (chosenInferenceNetworkType == InferenceNetworkType.Detection)
                {
                    ManagedInferenceBoundingBoxResult boxResult = managedChunkData.InferenceBoundingBoxResult;
                    Console.WriteLine("\tInference bounding box result");

                    short boxCount = boxResult.BoxCount;
                    if (boxCount == 0)
                    {
                        Console.WriteLine("\tNo bounding box");
                    }

                    for (short i = 0; i < boxResult.BoxCount; ++i)
                    {
                        ManagedInferenceBoundingBox box = boxResult.get_BoxAt(i);
                        switch (box.BoxType)
                        {
                            case InferenceBoundingBoxType.RECTANGLE:
                                Console.WriteLine(
                                    "\t\tBox[{0}]: Class {1} ({2}) - {3}% - Rectangle (X={4}, Y={5}, W={6}, H={7})",
                                    i + 1,
                                    box.ClassId,
                                    (box.ClassId < labelDetection.Length ? labelDetection[box.ClassId]
                                     : "N/A"),
                                    box.Confidence * 100,
                                    box.RectangleTopLeftX,
                                    box.RectangleTopLeftY,
                                    box.RectangleBottomRightX - box.RectangleTopLeftX,
                                    box.RectangleBottomRightY - box.RectangleTopLeftY);
                                break;
                            case InferenceBoundingBoxType.CIRCLE:
                                Console.WriteLine(
                                    "\t\tBox[{0}]: Class {1} ({2}) - {3}% - Rectangle (X={4}, Y={5}, W={6}, H={7})",
                                    i + 1,
                                    box.ClassId,
                                    box.Confidence * 100,
                                    box.CircleCenterX,
                                    box.CircleCenterY,
                                    box.CircleRadius);
                                break;
                            case InferenceBoundingBoxType.ROTATED_RECTANGLE:
                                Console.WriteLine(
                                    "\t\tBox[{0}]: Class {1} ({2}) - {3}% - Rectangle (X={4}, Y={5}, W={6}, H={7})",
                                    i + 1,
                                    box.ClassId,
                                    (box.ClassId < labelDetection.Length ? labelDetection[box.ClassId]
                                     : "N/A"),
                                    box.Confidence * 100,
                                    box.RotatedRectangleTopLeftX,
                                    box.RotatedRectangleTopLeftY,
                                    box.RotatedRectangleBottomRightX,
                                    box.RotatedRectangleBottomRightY,
                                    box.RotatedRectangleRotationAngle);
                                break;
                            default:
                                Console.WriteLine(
                                    "\t\tBox[{0}]: Class {1} - {2} Unknown bounding box type (not supported)",
                                    i,
                                    box.ClassId,
                                    box.Confidence * 100);
                                break;
                        }
                    }
                }
                else
                {
                    long inferenceResult = managedChunkData.InferenceResult;
                    Console.WriteLine(
                        "\tInference result: {0} ({1})",
                        inferenceResult,
                        (inferenceResult < labelClassification.Length? labelClassification[(int) inferenceResult]
                         : "N/A"));

                    double inferenceConfidence = managedChunkData.InferenceConfidence;
                    Console.WriteLine("\tInference confidence: {0} %", inferenceConfidence * 100);
                }
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function disables trigger mode on the camera.
        int DisableTrigger(INodeMap nodeMap)
        {
            int result = 0;
            Console.WriteLine("\n*** DISABLING TRIGGER ***");

            try
            {
                // Configure TriggerMode
                IEnum iTriggerMode = nodeMap.GetNode<IEnum>("TriggerMode");
                if (iTriggerMode == null || !iTriggerMode.IsWritable || !iTriggerMode.IsReadable)
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

                Console.WriteLine("Configure TriggerMode to {0}", iTriggerModeOff.Symbolic);
                iTriggerMode.Value = iTriggerModeOff.Symbolic;
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function configures camera to run in "inference sync" trigger mode.
        // of the chosen trigger.
        int ConfigureTrigger(INodeMap nodeMap)
        {
            int result = 0;

            Console.WriteLine("\n*** CONFIGURING TRIGGER ***");

            try
            {
                // Set TriggerSelector to FrameStart
                IEnum iTriggerSelector = nodeMap.GetNode<IEnum>("TriggerSelector");
                if (iTriggerSelector == null || !iTriggerSelector.IsWritable || !iTriggerSelector.IsReadable)
                {
                    Console.WriteLine("Unable to get or set trigger selector (enum retrieval). Aborting...");
                    return -1;
                }

                // Set trigger mode to software
                IEnumEntry iTriggerSelectorFrameStarts = iTriggerSelector.GetEntryByName("FrameStart");
                if (iTriggerSelectorFrameStarts == null || !iTriggerSelectorFrameStarts.IsReadable)
                {
                    Console.WriteLine("Unable to get software trigger selector (entry retrieval). Aborting...");
                    return -1;
                }

                iTriggerSelector.Value = iTriggerSelectorFrameStarts.Symbolic;

                Console.WriteLine("Trigger selector set to frame start...");

                // Configure TriggerSource
                IEnum iTriggerSource = nodeMap.GetNode<IEnum>("TriggerSource");
                if (iTriggerSource == null || !iTriggerSource.IsWritable || iTriggerSource.IsReadable)
                {
                    Console.WriteLine("Unable to get or set trigger mode (enum retrieval). Aborting...");
                    return -1;
                }

                IEnumEntry iTriggerSourceSoftware = iTriggerSource.GetEntryByName("InferenceReady");
                if (iTriggerSourceSoftware == null || !iTriggerSourceSoftware.IsReadable)
                {
                    Console.WriteLine("Unable to get software trigger mode (entry retrieval). Aborting...");
                    return -1;
                }

                iTriggerSource.Value = iTriggerSourceSoftware.Symbolic;

                Console.WriteLine("Configuring TriggerSource to {0}", iTriggerSourceSoftware.Symbolic);

                // Configure TriggerMode
                IEnum iTriggerMode = nodeMap.GetNode<IEnum>("TriggerMode");
                if (!iTriggerMode.IsWritable || !iTriggerMode.IsReadable)
                {
                    Console.WriteLine("Unable to configure TriggerMode. Aborting...");
                    return -1;
                }

                IEnumEntry iTriggerModeOn = iTriggerMode.GetEntryByName("On");
                if (iTriggerModeOn == null || !iTriggerModeOn.IsReadable)
                {
                    Console.WriteLine("Unable to enable trigger mode (entry retrieval). Aborting...");
                    return -1;
                }

                Console.WriteLine("Configuring TriggerMode to {0}", iTriggerModeOn.Symbolic);
                iTriggerMode.Value = iTriggerModeOn.Symbolic;
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function enables/disables inference on the camera and configures the inference network type
        static int ConfigureInference(INodeMap nodeMap, bool isEnabled)
        {
            int result = 0;

            if (isEnabled)
            {
                Console.WriteLine(
                    "\n*** CONFIGURING INFERENCE ( {0} ) ***",
                    ((chosenInferenceNetworkType == InferenceNetworkType.Detection) ? "DETECTION" : "CLASSIFICATION"));
            }
            else
            {
                Console.WriteLine("\n*** DISABLING INFERENCE ***");
            }

            try
            {
                if (isEnabled)
                {
                    // Set Network Type to Detection
                    IEnum ptrInferenceNetworkTypeSelector = nodeMap.GetNode<IEnum>("InferenceNetworkTypeSelector");
                    if (!ptrInferenceNetworkTypeSelector.IsWritable || !ptrInferenceNetworkTypeSelector.IsReadable)
                    {
                        Console.WriteLine("Unable to query InferenceNetworkTypeSelector. Aborting...");
                        return -1;
                    }

                    string networkTypeString =
                        (chosenInferenceNetworkType == InferenceNetworkType.Detection) ? "Detection" : "Classification";

                    // Retrieve entry node from enumeration node
                    IEnumEntry ptrInferenceNetworkType =
                        ptrInferenceNetworkTypeSelector.GetEntryByName(networkTypeString);
                    if (!ptrInferenceNetworkType.IsReadable)
                    {
                        Console.WriteLine(
                            "Unable to set inference network type to {0}(entry retrieval). Aborting...",
                            networkTypeString);
                        return -1;
                    }

                    ptrInferenceNetworkTypeSelector.Value = ptrInferenceNetworkType.Symbolic;

                    Console.WriteLine("Inference network type set to {0}...", networkTypeString);
                }

                // Enable/Disable inference
                Console.WriteLine("{0} inference...", isEnabled ? "Enabling" : "Disabling");
                IBool ptrInferenceEnable = nodeMap.GetNode<IBool>("InferenceEnable");
                if (!ptrInferenceEnable.IsWritable)
                {
                    Console.WriteLine("Unable to enable inference. Aborting...");
                    return -1;
                }

                ptrInferenceEnable.Value = isEnabled;
                Console.WriteLine("Inference {0}", (isEnabled ? "enabled..." : "disabled..."));
            }
            catch (SpinnakerException e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.Message);
                result = -1;
            }

            return result;
        }

        // This function configures camera test pattern to make use of the injected test image for inference
        static int ConfigureTestPattern(INodeMap nodeMap, bool isEnabled)
        {
            int result = 0;

            if (isEnabled)
            {
                Console.WriteLine("\n*** CONFIGURING TEST PATTERN ***");
            }
            else
            {
                Console.WriteLine("\n*** DISABLING TEST PATTERN ***");
            }

            try
            {
                // Set TestPatternGeneratorSelector to PipelineStart
                IEnum iTestPatternGeneratorSelector = nodeMap.GetNode<IEnum>("TestPatternGeneratorSelector");

                if (isEnabled)
                {
                    IEnumEntry iTestPatternGeneratorPipelineStart =
                        iTestPatternGeneratorSelector.GetEntryByName("PipelineStart");
                    iTestPatternGeneratorSelector.Value = iTestPatternGeneratorPipelineStart.Symbolic;
                    Console.WriteLine(
                        "TestPatternGeneratorSelector set to {0}...", iTestPatternGeneratorPipelineStart.Symbolic);
                }
                else
                {
                    IEnumEntry iTestPatternGeneratorSensor = iTestPatternGeneratorSelector.GetEntryByName("Sensor");
                    iTestPatternGeneratorSelector.Value = iTestPatternGeneratorSensor.Symbolic;
                    Console.WriteLine(
                        "TestPatternGeneratorSelector set to {0}...", iTestPatternGeneratorSensor.Symbolic);
                }

                // Set TestPattern to InjectedImage
                IEnum iTestPattern = nodeMap.GetNode<IEnum>("TestPattern");

                if (isEnabled)
                {
                    IEnumEntry iTestPatternInjectedImage = iTestPattern.GetEntryByName("InjectedImage");
                    iTestPattern.Value = iTestPatternInjectedImage.Symbolic;
                    Console.WriteLine("TestPattern set to {0}...", iTestPatternInjectedImage.Symbolic);
                }
                else
                {
                    IEnumEntry iTestPatternOff = iTestPattern.GetEntryByName("Off");
                    iTestPattern.Value = iTestPatternOff.Symbolic;
                    Console.WriteLine("TestPattern set to {0}...", iTestPatternOff.Symbolic);
                }

                if (isEnabled)
                {
                    // The inject images have different ROI sizes so camera needs to be configured to the appropriate
                    // injected width and height
                    IInteger iInjectedWidth = nodeMap.GetNode<IInteger>("InjectedWidth");
                    iInjectedWidth.Value = injectedImageWidth;

                    IInteger iInjectedHeight = nodeMap.GetNode<IInteger>("InjectedHeight");
                    iInjectedHeight.Value = injectedImageHeight;
                }
            }
            catch (SpinnakerException e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.Message);
                result = -1;
            }

            return result;
        }

        // This function acquires and saves 10 images from a device; please see
        // Acquisition_CSharp example for more in-depth comments on acquiring images.
        static int AcquireImages(IManagedCamera cam, INodeMap nodeMap, INodeMap nodeMapTLDevice)
        {
            int result = 0;

            Console.WriteLine("*** IMAGE ACQUISITION ***");

            try
            {
                // Set acquisition mode to continuous
                IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");

                IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");

                iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;

                Console.WriteLine("Acquisition mode set to continuous...");

                // Begin acquiring images
                cam.BeginAcquisition();

                Console.WriteLine("Acquiring images...");

                // Retrieve device serial number for filename
                string deviceSerialNumber = "";

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
                                    "Grabbed image {0}, width = {1}, height = {1}",
                                    imageCnt,
                                    rawImage.Width,
                                    rawImage.Height);

                                // Convert image to mono 8
                                using(
                                    IManagedImage convertedImage = processor.Convert(rawImage, PixelFormatEnums.Mono8))
                                {
                                    // Create unique file name
                                    string filename = "Inference-CSharp-";
                                    if (deviceSerialNumber != "")
                                    {
                                        filename = filename + deviceSerialNumber + "-";
                                    }
                                    filename = filename + imageCnt + ".jpg";

                                    // Save image
                                    convertedImage.Save(filename);

                                    Console.WriteLine("Image saved at {0}", filename);

                                    // Display chunk data
                                    result = DisplayChunkData(rawImage);
                                }
                            }
                        }
                        Console.WriteLine();
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

            try
            {
                // Retrieve TL device nodemap and print device information
                INodeMap nodeMapTLDevice = cam.GetTLDeviceNodeMap();

                result = PrintDeviceInfo(nodeMapTLDevice);

                // Initialize camera
                cam.Init();

                // Retrieve GenICam nodemap
                INodeMap nodeMap = cam.GetNodeMap();

                // Check to make sure camera supports inference
                Console.WriteLine("Checking camera inference support...");
                IBool ptrInferenceEnable = nodeMap.GetNode<IBool>("InferenceEnable");
                if (ptrInferenceEnable == null || !ptrInferenceEnable.IsWritable)
                {
                    Console.WriteLine("Inference is not supported on this camera. Aborting...");
                    return -1;
                }

                // Upload custom inference network onto the camera
                // The inference network file is in a movidius specific neural network format.
                // Uploading the network to the camera allows for "inference on the edge" where
                // camera can apply deep learning on a live stream. Refer to "Getting Started
                // with Firefly-DL" for information on how to create your own custom inference
                // network files using pre-existing neural network.
                result = UploadFileToCamera(nodeMap, "InferenceNetwork", networkFilePath);
                if (result < 0)
                {
                    return result;
                }

                // Upload injected test image
                // Instead of applying deep learning on a live stream, the camera can be
                // tested with an injected test image.
                result = UploadFileToCamera(nodeMap, "InjectedImage", injectedImageFilePath);
                if (result < 0)
                {
                    return result;
                }

                // Configure inference
                result = ConfigureInference(nodeMap, true);
                if (result < 0)
                {
                    return result;
                }

                // Configure test pattern to make use of the injected image
                result = ConfigureTestPattern(nodeMap, true);
                if (result < 0)
                {
                    return result;
                }

                // Configure trigger
                // When enabling inference results via chunk data, the results that accompany a frame
                // will likely not be the frame that inference was run on. In order to guarantee that
                // the chunk inference results always correspond to the frame that they are sent with,
                // the camera needs to be put into the "inference sync" trigger mode.
                // Note: Enabling this setting will limit frame rate so that every frame contains new
                //       inference dataset. To not limit the frame rate, you can enable InferenceFrameID
                //       chunk data to help determine which frame is associated with a particular
                //       inference data.
                result = ConfigureTrigger(nodeMap);
                if (result < 0)
                {
                    return result;
                }

                // Configure chunk data
                result = ConfigureChunkData(nodeMap);
                if (result < 0)
                {
                    return result;
                }

                // Acquire images and display chunk data
                result = result | AcquireImages(cam, nodeMap, nodeMapTLDevice);

                // Disable chunk data
                result = DisableChunkData(nodeMap);
                if (result < 0)
                {
                    return result;
                }

                // Disable trigger
                result = DisableTrigger(nodeMap);
                if (result < 0)
                {
                    return result;
                }

                // Disable test pattern
                result = ConfigureTestPattern(nodeMap, false);
                if (result < 0)
                {
                    return result;
                }

                // Disable inference
                result = ConfigureInference(nodeMap, false);
                if (result < 0)
                {
                    return result;
                }

                // Clear injected test image
                result = DeleteFileOnCamera(nodeMap, "InjectedImage"); //?
                if (result < 0)
                {
                    return result;
                }

                // Clear uploaded inference network
                result = DeleteFileOnCamera(nodeMap, "InferenceNetwork");
                if (result < 0)
                {
                    return result;
                }

                //  Deinitialize camera
                cam.DeInit();
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        private static int Main(string[] args)
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

            int numCameras = camList.Count;

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