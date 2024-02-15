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
 *  @example Polarization_CSharp.cs
 *
 *  @brief Polarization_CSharp.cs shows how to extract and create images from a source image of
 *  Polarized8 or BayerRGPolarized8 pixel format using methods from the ImageUtilityPolarization,
 *  ImageUtility and ImageUtilityHeatmap classes.
 *  It relies on information provided in the Enumeration, Acquisition, and NodeMapInfo examples.
 *
 *  This example demonstrates some of the methods that can be used to extract polarization quadrant
 *  images and create Stokes', AoLP, and DoLP images from the ImageUtilityPolarization class.
 *  It then demonstrates how to use some of the available methods in the ImageUtility and
 *  ImageUtilityHeatmap classes to create normalized and heatmap images.
 *
 *  Polarization is only available for polarized cameras. For more information
 *  please visit our website;
 * https://www.flir.com/discover/iis/machine-vision/imaging-reflective-surfaces-sonys-first-polarized-sensor
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

namespace Polarization_CSharp
{
    public static class Globals
    {
        public static bool isPixelFormatColor = false;
    }

    class Program
    {
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
                    for (int i = 0; i < category.Children.Length; ++i)
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

        // This function sets the pixel format to a Polarized pixel format, and acquisition mode to single frame.
        static int ConfigureStream(INodeMap nodeMap)
        {
            int result = 0;
            //
            // Set the pixel format to Polarized8 or BayerRGPolarized8
            //
            // *** NOTES ***
            // Methods in the ManagedImageUtilityPolarization class are supported for images of pixel format
            // Polarized8 and BayerRGPolarized8. These formats are only available on the polarized camera.
            // For more in-depth comments on formatting images, see the ImageFormatControl example.

            // Retrieve the enumeration node from the nodemap
            IEnum iPixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");
            if (iPixelFormat != null && iPixelFormat.IsWritable && iPixelFormat.IsReadable)
            {
                // Retrieve the desired entry node from the enumeration node
                IEnumEntry iPixelFormatPolarized8 = iPixelFormat.GetEntryByName("Polarized8");
                IEnumEntry iPixelFormatBayerRGPolarized8 = iPixelFormat.GetEntryByName("BayerRGPolarized8");
                if (iPixelFormatPolarized8 != null && iPixelFormatPolarized8.IsReadable)
                {
                    // Retrieve the integer value from the entry node
                    // Set integer as new value for enumeration node
                    iPixelFormat.Value = iPixelFormatPolarized8.Value;

                    Globals.isPixelFormatColor = false;

                    Console.WriteLine("Pixel format set to {0} ...\n", iPixelFormatPolarized8.DisplayName);
                }
                else if (iPixelFormatBayerRGPolarized8 != null && iPixelFormatBayerRGPolarized8.IsReadable)
                {
                    // Retrieve the integer value from the entry node
                    long pixelFormatBayerRGPolarized8 = iPixelFormatBayerRGPolarized8.Value;

                    // Set integer as new value for enumeration node
                    iPixelFormat.Value = pixelFormatBayerRGPolarized8;
                    Globals.isPixelFormatColor = true;

                    Console.WriteLine("Pixel format set to {0}...", iPixelFormatBayerRGPolarized8.DisplayName);
                }
                else
                {
                    // Methods in the ManagedImageUtilityPolarization class are supported for images of
                    // polarized pixel formats only.
                    Console.WriteLine(
                        "Pixel format Polarized8 or BayerRGPolarized8 not available (entry retrieval). Aborting...");
                    Console.WriteLine();
                    result = -1;
                }
            }
            else
            {
                Console.WriteLine("Pixel format not available (enum retrieval). Aborting...");
                result = -1;
            }

            // Set acquisition mode to single frame
            IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");
            if (iAcquisitionMode == null || !iAcquisitionMode.IsWritable || !iAcquisitionMode.IsReadable)
            {
                Console.WriteLine("Unable to set acquisition mode to single frame (enum retrieval). Aborting...");

                result = -1;
            }

            // Retrieve entry node from enumeration node
            IEnumEntry iAcquisitionModeSingleFrame = iAcquisitionMode.GetEntryByName("SingleFrame");
            if (iAcquisitionModeSingleFrame == null || !iAcquisitionModeSingleFrame.IsReadable)
            {
                Console.WriteLine("Unable to set acquisition mode to single frame (entry retrieval). Aborting...");
                Console.WriteLine();
                result = -1;
            }

            // Retrieve integer value from entry node
            iAcquisitionMode.Value = iAcquisitionModeSingleFrame.Value;

            // Set integer value from entry node as new value of enumeration node
            // iAcquisitionMode.SetIntValue(acquisitionModeSingleFrame);

            Console.WriteLine("Acquisition mode set to single frame...");
            Console.WriteLine();

            return result;
        }

        // This function saves an image and prints some information.
        // The serial number will be prepended to the filename if it is not empty.
        static int SaveImage(IManagedImage pImage, string filename, string serialNumber)
        {
            int result = 0;
            try
            {
                string fullFilename;
                if (serialNumber != null)
                {
                    // Prepend the filename with the serial number
                    fullFilename = serialNumber + "-" + filename;
                }
                else
                {
                    fullFilename = filename;
                }

                // Save the image and print image info
                pImage.Save(fullFilename);
                Console.WriteLine("Image saved at {0}...", fullFilename);
                Console.WriteLine(
                    "Width = {0}, height = {1}, pixel format = {2}", pImage.Width, pImage.Height, pImage.PixelFormat);
                Console.WriteLine();
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function returns a string of the specified polarization quadrant appendage.
        static string GetQuadFileNameAppendage(PolarizationQuadrant quadrant)
        {
            switch (quadrant)
            {
                case PolarizationQuadrant.QUADRANT_I0:
                    return "I0";
                case PolarizationQuadrant.QUADRANT_I45:
                    return "I45";
                case PolarizationQuadrant.QUADRANT_I90:
                    return "I90";
                case PolarizationQuadrant.QUADRANT_I135:
                    return "I135";
                default:
                    return "UNKNOWN_QUAD";
            }
        }

        // This function creates and saves a heatmap image using the ImageUtilityHeatmap class.
        // The function demonstrates setting the heatmap gradient and range.
        static int CreateHeatmapImages(IManagedImage mono8Image, string baseFilename, string deviceSerialNumber)
        {
            int result = 0;
            try
            {
                //
                // Set the heatmap color gradient and range.
                //
                // *** NOTES ***
                // By default the heatmap gradient will be set from HEATMAP_BLACK to HEATMAP_WHITE, and the
                // range from 0 to 100 percent radiance. Changes to the heatmap can be visualized in SpinView
                // using the 'Configure Heatmap Gradient' tool when streaming with any heatmap polarization
                // algorithm applied.
                // (ex. Heatmap (AoLP)). Below are the optional functions available to modify the heatmap.
                //
                ManagedImageUtilityHeatmap.SetHeatmapColorGradient(
                    HeatmapColor.HEATMAP_BLACK, HeatmapColor.HEATMAP_WHITE);

                //
                // *** NOTES ***
                // The heatmap can be manipulated to focus on a portion of the calculated range (from 0 to 100%).
                // The radiance of the heatmap describes the percent linear polarization for DoLP images, the
                // degree of linear polarization for AoLP images (from -90 to 90), and the percent radiance for
                // Stokes' parameters. Note that AoLP angles need to be expressed as a percentage of the maximum
                // range (-90 to 90) before being used as inputs to this function. In SpinView the percent is
                // shown in brackets in the range slider tool tip.
                // Converting from the range of (-90 to 90) deg to (0 to 100) percent is shown:
                //     degrees = (percent / 100) * 180 - 90
                //     percent = (degrees + 90) * 100 / 180
                //
                ManagedImageUtilityHeatmap.SetHeatmapRange(0, 100);

                // Create a heatmap image and save it
                //
                // *** NOTES ***
                // Creating heatmap images is not exclusive to polarized cameras.
                // Any image of pixel format Mono8 or Mono16 can be used to create a heatmap image.
                //
                var heatmapImage = ManagedImageUtilityHeatmap.CreateHeatmap(mono8Image);
                SaveImage(heatmapImage, (baseFilename + "_Heatmap.jpg"), deviceSerialNumber);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }
            return result;
        }

        // This function extracts polarization quadrant images using the ManagedImageUtilityPolarization class.
        // It then calls helper function CreateHeatmapImages on all monochrome polarization quadrant images.
        static int ExtractAndSavePolarQuadImages(IManagedImage pRawPolarizedImage, string deviceSerialNumber)
        {
            int result = 0;
            try
            {
                // Define an array of polarization quadrant enums to use in ExtractPolarQuadrant method
                PolarizationQuadrant[] polarizationQuadEnums = {
                    PolarizationQuadrant.QUADRANT_I0,
                    PolarizationQuadrant.QUADRANT_I45,
                    PolarizationQuadrant.QUADRANT_I90,
                    PolarizationQuadrant.QUADRANT_I135,
                };

                foreach(PolarizationQuadrant quadrant in polarizationQuadEnums)
                {
                    // Save a string that describes the image being saved
                    string quadrantName = "Quadrant_" + GetQuadFileNameAppendage(quadrant);

                    // Extract the polarization quadrant image and save it
                    //
                    // *** NOTES ***
                    // Polarization quadrant images are unaltered source data extracted into images that
                    // represent all pixels with a polarizing filter of the specified orientation.
                    // i.e. 0 deg polarization = QUADRANT_I0.
                    // This means that each extracted image will be a quarter the size of the source image,
                    // as each type of polarizing filter covers a fourth of the sensors photodiodes.
                    // Polarization quadrant images are extracted as Mono8 and BayerRG8 for monochrome and
                    // color cameras respectively.
                    //
                    IManagedImage polarizationQuadImage =
                        ManagedImageUtilityPolarization.ExtractPolarQuadrant(pRawPolarizedImage, quadrant);
                    SaveImage(polarizationQuadImage, (quadrantName + ".jpg"), deviceSerialNumber);

                    // Save heatmap images for each Mono8 polarization quadrant images.
                    if (!Globals.isPixelFormatColor)
                    {
                        CreateHeatmapImages(polarizationQuadImage, quadrantName, deviceSerialNumber);
                    }
                }
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }
            return result;
        }

        // This function creates and saves an image with reduced glare using the ManagedImageUtilityPolarization class.
        static int CreateAndSaveGlareReducedImage(IManagedImage pRawPolarizedImage, string deviceSerialNumber)
        {
            int result = 0;
            try
            {
                // Create a glare reduced image and save it
                //
                // *** NOTES ***
                // When unpolarized light is incident upon a dielectric surface, the reflected portion of the light
                // is partially polarized according to Brewster's law. Selecting the filtered pixel that most
                // effectively blocks this polarized light in each pixel quadrant reduces glare in the overall image.
                // Since one pixel is selected from each 2x2 polarized pixel quadrant the resulting image will be a
                // quarter of the raw image's resolution.
                //
                var glareReducedImage = ManagedImageUtilityPolarization.CreateGlareReduced(pRawPolarizedImage);

                SaveImage(glareReducedImage, "Glare_Reduced.jpg", deviceSerialNumber);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }
            return result;
        }

        // This function creates and saves a normalized image using the ManagedImageUtility class.
        // Monochrome and color images are normalized to Pixelformat_Mono8 and PixelFormat_RGB8 respectively
        static int CreateAndSaveNormalizedImage(
            IManagedImage imageToNormalize,
            string baseFilename,
            string deviceSerialNumber,
            ManagedImageUtility.SourceDataRange srcDataRange = ManagedImageUtility.SourceDataRange.IMAGE_DATA_RANGE)
        {
            int result = 0;
            try
            {
                // Create a normalized image
                //
                // *** NOTES ***
                // Creating normalized images is not exclusive to polarized cameras!
                // Any image with image data (pixel format) of type of char, short, or float can be used to
                // create a normalized image.               //

                var normalizedImage = ManagedImageUtility.CreateNormalized(
                    imageToNormalize, Globals.isPixelFormatColor ? PixelFormatEnums.RGB8
                    : PixelFormatEnums.Mono8, srcDataRange);
                SaveImage(normalizedImage, (baseFilename + "_Normalized.jpg"), deviceSerialNumber);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }
            return result;
        }

        // This function creates and saves raw and normalized Stokes' images using the
        // ManagedImageUtilityPolarization class.
        static int CreateAndSaveStokesImages(IManagedImage pRawPolarizedImage, string deviceSerialNumber)
        {
            int result = 0;
            try
            {
                // Create Stokes' images using the appropriate function calls
                //
                // *** NOTES ***
                // Stokes' images add (S0) or subtract (S1, S2) polarization quadrant images. Therefore
                // each created image is a quarter the size of the source image.
                //
                // The algorithms are as follows:
                // S0 = I0  + I90  : The overall intensity of light
                // S1 = I0  - I90  : The difference in intensity accepted through the polarizers at 0 and 90
                //                   to the horizontal
                // S2 = I45 - I135 : The difference in intensity accepted through the polarizers at 45 and -45
                //                   to the horizontal
                //
                // The calculated Stokes' values can range from, 0 (S0) or -255 (S1, S2), to 510 and thus are
                // stored with pixel formats Mono16s or RGB16s, for monochrome and color cameras respectively.
                // These formats can only be saved using a raw file extension.
                //
                var stokesS0Image = ManagedImageUtilityPolarization.CreateStokesS0(pRawPolarizedImage);
                var stokesS1Image = ManagedImageUtilityPolarization.CreateStokesS1(pRawPolarizedImage);
                var stokesS2Image = ManagedImageUtilityPolarization.CreateStokesS2(pRawPolarizedImage);

                IManagedImage[] stokesImages = {stokesS0Image, stokesS1Image, stokesS2Image};

                // Save a stokes Appendage to create a descriptive filename
                Int64 stokesAppendage = 0;

                // Loop through raw Stokes' images, saving a raw and normalized copy
                foreach(IManagedImage stokesImage in stokesImages)
                {
                    string stokesName = "Stokes_S" + stokesAppendage.ToString();
                    stokesAppendage++;

                    // Save the raw Stokes' images
                    SaveImage(stokesImage, (stokesName + ".raw"), deviceSerialNumber);

                    // Create and save a normalized Stokes' image
                    CreateAndSaveNormalizedImage(
                        stokesImage,
                        stokesName,
                        deviceSerialNumber,
                        ManagedImageUtility.SourceDataRange.ABSOLUTE_DATA_RANGE);
                }
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }
            return result;
        }

        // This function creates and saves raw and normalized AoLP and DoLP images using the
        // ManagedImageUtilityPolarization class.
        static int CreateAndSaveAolpDolpImages(IManagedImage pRawPolarizedImage, string deviceSerialNumber)
        {
            int result = 0;
            try
            {
                // Create and save AoLP and DoLP images using the appropriate function calls
                //
                // *** NOTES ***
                // The Angle of Linear Polarization, AoLP, and Degree of Linear Polarization, DoLP, are calculated
                // using Stokes' values. Therefore each created image is a quarter the size of the source image.
                //
                // The algorithms are as follows:
                // DoLP = ((S1pow(2) + S2pow(2))pow(1/2)) / S0 : The fraction of incident light intensity in
                //                                               the linear polarization states
                // AoLP = (1/2)* arctan( S2 / S1)              : The angle at which linearly polarized light
                //                                               oscillates with respect to a reference axis
                //
                // The calculated AoLP will range from -90 deg to 90 deg and DoLP values will range from 0 to 1
                // (float). Therefore the images are stored with pixel formats Mono32f or RGB32f, for monochrome
                // and color cameras respectively. These formats can only be saved using a raw file extension.
                //
                var aolpImage = ManagedImageUtilityPolarization.CreateAolp(pRawPolarizedImage);
                SaveImage(aolpImage, "AoLP.raw", deviceSerialNumber);

                var dolpImage = ManagedImageUtilityPolarization.CreateDolp(pRawPolarizedImage);
                SaveImage(dolpImage, "DoLP.raw", deviceSerialNumber);

                // Create and save normalized AoLP and DoLP images
                CreateAndSaveNormalizedImage(
                    aolpImage, "AoLP", deviceSerialNumber, ManagedImageUtility.SourceDataRange.ABSOLUTE_DATA_RANGE);
                CreateAndSaveNormalizedImage(
                    dolpImage, "DoLP", deviceSerialNumber, ManagedImageUtility.SourceDataRange.ABSOLUTE_DATA_RANGE);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }
            return result;
        }

        // This function acquires and saves 10 images from a device.
        static int AcquireImages(IManagedCamera cam, INodeMap nodeMap, INodeMap nodeMapTLDevice)
        {
            int result = 0;

            Console.WriteLine("\n*** IMAGE ACQUISITION ***\n");

            try
            {
                // Begin acquiring images
                cam.BeginAcquisition();

                Console.WriteLine("Acquiring an image from the polarized camera...");

                // Retrieve device serial number for filename
                string deviceSerialNumber = "";
                string filename = "Raw_Polarized_Image.jpg";

                IString IStringSerial = nodeMapTLDevice.GetNode<IString>("DeviceSerialNumber");
                if (IStringSerial != null && IStringSerial.IsReadable)
                {
                    deviceSerialNumber = IStringSerial.Value;

                    Console.WriteLine("Device serial number retrieved as {0}...", deviceSerialNumber);
                }
                Console.WriteLine();

                // Retrieve the received raw image
                using(IManagedImage pRawPolarizedImage = cam.GetNextImage(1000))
                {
                    // Ensure image completion
                    if (pRawPolarizedImage.IsIncomplete)
                    {
                        Console.WriteLine("Image incomplete with image status {0}...", pRawPolarizedImage.ImageStatus);
                    }
                    else
                    {
                        // Save a polarized reference image
                        //
                        // *** NOTES ***
                        // SaveImage prepends the serial number to the filename and save the image
                        //
                        result = result | SaveImage(pRawPolarizedImage, filename, deviceSerialNumber);

                        // Extract and save all polarization quadrants and create heatmap images for all
                        // monochrome images
                        result = result | ExtractAndSavePolarQuadImages(pRawPolarizedImage, deviceSerialNumber);

                        // Create and save raw and normalized Stokes' images
                        result = result | CreateAndSaveStokesImages(pRawPolarizedImage, deviceSerialNumber);

                        // Create and save raw and normalized AoLP and DoLP images
                        result = result | CreateAndSaveAolpDolpImages(pRawPolarizedImage, deviceSerialNumber);

                        // Create and save an image with a simple glare reduction applied
                        result = result | CreateAndSaveGlareReducedImage(pRawPolarizedImage, deviceSerialNumber);
                    }
                }
                Console.WriteLine();
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            // End acquisition
            cam.EndAcquisition();
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
                if (ConfigureStream(nodeMap) != -1)
                {
                    // Acquire images
                    result = result | AcquireImages(cam, nodeMap, nodeMapTLDevice);
                }
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
            // varmatically.
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
