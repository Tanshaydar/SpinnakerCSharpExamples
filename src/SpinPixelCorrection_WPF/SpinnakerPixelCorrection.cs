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

using System;
using System.Collections.Generic;
using System.Linq;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace SpinnakerPixelCorrection
{
    /// <summary>
    /// Class for the camera enumerated
    /// </summary>
    public class CameraInformation
    {
        // String parameters stored and camera pointer
        private string _deviceSerialNumber;
        private string _deviceModelName;
        private string _deviceType;
        private string _deviceDefectCorrectEnable;
        private INodeMap nodeMap;
        private IManagedCamera managedCamera;

        public string DeviceSerialNumber
        {
            get
            {
                return _deviceSerialNumber;
            }
            set
            {
                _deviceSerialNumber = value;
            }
        }
        public string DeviceModelName
        {
            get
            {
                return _deviceModelName;
            }
            set
            {
                _deviceModelName = value;
            }
        }
        public string DeviceType
        {
            get
            {
                return _deviceType;
            }
            set
            {
                _deviceType = value;
            }
        }
        public string DeviceDefectCorrectEnable
        {
            get
            {
                return _deviceDefectCorrectEnable;
            }
            set
            {
                _deviceDefectCorrectEnable = value;
            }
        }
        public INodeMap CamNodeMap
        {
            get
            {
                return nodeMap;
            }
            set
            {
                nodeMap = value;
            }
        }
        public IManagedCamera CamManagedCamera
        {
            get
            {
                return managedCamera;
            }
            set
            {
                managedCamera = value;
            }
        }

        ~CameraInformation()
        {
        }
    }

    /// <summary>
    /// Class for a single defect pixel
    /// </summary>
    public class DefectPixel
    {
        private long _defectTablePixelCount; // change this to defect pixel value...
        private long _defectTableIndex;
        private long _defectYCoord;
        private long _defectXCoord;
        public long DefectTablePixelCount
        {
            get
            {
                return _defectTablePixelCount;
            }
            set
            {
                _defectTablePixelCount = value;
            }
        }
        public long DefectTableIndex
        {
            get
            {
                return _defectTableIndex;
            }
            set
            {
                _defectTableIndex = value;
            }
        }
        public long DefectYCoord
        {
            get
            {
                return _defectYCoord;
            }
            set
            {
                _defectYCoord = value;
            }
        }
        public long DefectXCoord
        {
            get
            {
                return _defectXCoord;
            }
            set
            {
                _defectXCoord = value;
            }
        }

        ~DefectPixel()
        {
        }
    }

    /// <summary>
    /// Array of defect pixels collected from GenIcam pixel correction node
    /// </summary>
    public class DefectPixelList : List<DefectPixel>
    {
        /// <summary>
        /// Delete selected item.
        /// </summary>
        /// <param name="defectTableIndex"></param>
        public void Remove(long defectTableIndex)
        {
            this.RemoveAt((int) defectTableIndex - 1);
        }

        /// <summary>
        /// Remove all pixels
        /// </summary>
        public void Remove()
        {
            for (int i = 0; i < this.Count; i++)
            {
                this.RemoveAt(i);
            }
        }

        /// <summary>
        /// Add pixel at given index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="defectPixelListPixelCount"></param>
        /// <param name="defectPixelListDefectXCoord"></param>
        /// <param name="defectPixelListDefectYCoord"></param>
        public void Set(
            int index,
            ref long defectPixelListPixelCount,
            ref long defectPixelListDefectXCoord,
            ref long defectPixelListDefectYCoord)
        {
            defectPixelListPixelCount = this[index].DefectTablePixelCount;
            defectPixelListDefectXCoord = this[index].DefectXCoord;
            defectPixelListDefectYCoord = this[index].DefectYCoord;
        }
    }

    /// <summary>
    /// Pixel Correction class
    /// </summary>
    public class PixelCorrection
    {
        // Default thresholding parameters
        private const double _gainDefault = 0.65;
        private const double _exposureTimeDefault = 133.33;
        private const double _thresholdDefault = 40;
        private const double _temperatureDefault = 25;

        // Other constants
        private int _maxPixelCount = 255;

        // Thresholding parameters variables
        private double _gain;
        private double _exposureTime;
        private double _threshold;
        private double _temperature;

        // Other Variables
        private int _numDefectPixel;

        public double GainDefault
        {
            get
            {
                return _gainDefault;
            }
        }
        public double ExposureTimeDefault
        {
            get
            {
                return _exposureTimeDefault;
            }
        }
        public double ThresholdDefault
        {
            get
            {
                return _thresholdDefault;
            }
        }
        public double TemperatureDefault
        {
            get
            {
                return _temperatureDefault;
            }
        }

        public int MaxPixelCount
        {
            get
            {
                return _maxPixelCount;
            }
        }

        public double Gain
        {
            get
            {
                return _gain;
            }
            set
            {
                _gain = value;
            }
        }
        public double ExposureTime
        {
            get
            {
                return _exposureTime;
            }
            set
            {
                _exposureTime = value;
            }
        }
        public double Threshold
        {
            get
            {
                return _threshold;
            }
            set
            {
                _threshold = value;
            }
        }
        public double Temperature
        {
            get
            {
                return _temperature;
            }
            set
            {
                _temperature = value;
            }
        }

        public int NumDefectPixel
        {
            get
            {
                return _numDefectPixel;
            }
            set
            {
                _numDefectPixel = value;
            }
        }

        public enum PixelCorrectionError {
            NO_ERROR = 0,
            INTERNAL_ERROR,
            INTERNAL_ERROR_READING_NODES,
            GAIN_OUT_OF_RANGE,
            EXPOSURE_OUT_OF_RANGE,
            TEMPERATURE_OUT_OF_RANGE,
            THRESHOLD_OUT_OF_RANGE,
            X_COORDINATE_OUT_OF_RANGE,
            Y_COORDINATE_OUT_OF_RANGE,
            PIXEL_SET_ALREADY_EXISTING,
            NO_DEFECTIVE_PIXELS_FOUND,
            LESS_THAN_255_DEFECTIVE_PIXELS_FOUND,
            TOO_MANY_DEFECTIVE_PIXELS
        }

        /// <summary>
        /// Apply given threshold
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="gainToSet"></param>
        /// <param name="exposureTimeToSet"></param>
        /// <param name="thresholdToSet"></param>
        /// <param name="defectPixelList"></param>
        public PixelCorrectionError ApplyThreshold(
            ref CameraInformation camera,
            double gainToSet,
            double exposureTimeToSet,
            double thresholdToSet,
            ref DefectPixelList defectPixelList)
        {
            PixelCorrectionError result = 0;

            // Set gain and exposure time value on camera
            // Remove from automatic mode
            IEnum iGainAuto = camera.CamNodeMap.GetNode<IEnum>("GainAuto");
            if (iGainAuto == null || !iGainAuto.IsReadable || !iGainAuto.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            IEnumEntry iGainAutoOff = iGainAuto.GetEntryByName("Off");
            if (iGainAutoOff == null || !iGainAutoOff.IsReadable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }
            iGainAuto.Value = iGainAutoOff.Value;

            IFloat iGain = camera.CamNodeMap.GetNode<IFloat>("Gain");
            if (iGain == null || !iGain.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }
            // Set gain
            iGain.Value = gainToSet;

            IEnum iExposureAuto = camera.CamNodeMap.GetNode<IEnum>("ExposureAuto");
            if (iExposureAuto == null || !iExposureAuto.IsReadable || !iExposureAuto.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            IEnumEntry iExposureAutoOff = iExposureAuto.GetEntryByName("Off");
            if (iExposureAutoOff == null || !iExposureAutoOff.IsReadable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }
            iExposureAuto.Value = iExposureAutoOff.Value;

            IFloat iExposureTime = camera.CamNodeMap.GetNode<IFloat>("ExposureTime");
            if (iExposureTime == null || !iExposureTime.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            // Set exposure time
            iExposureTime.Value = exposureTimeToSet;

            defectPixelList.Clear();

            // Set num pixels to zero in camera
            IInteger iDefectTablePixelCount = camera.CamNodeMap.GetNode<IInteger>("DefectTablePixelCount");
            if (iDefectTablePixelCount == null || !iDefectTablePixelCount.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }
            iDefectTablePixelCount.Value = 0;

            // Take an image
            IEnum iAcquisitionMode = camera.CamNodeMap.GetNode<IEnum>("AcquisitionMode");
            if (iAcquisitionMode == null || !iAcquisitionMode.IsReadable || !iAcquisitionMode.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            IEnumEntry iAcquisitionModeSingleFrame = iAcquisitionMode.GetEntryByName("SingleFrame");
            if (iAcquisitionModeSingleFrame == null || !iAcquisitionModeSingleFrame.IsReadable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");
            if (iAcquisitionModeContinuous == null || !iAcquisitionModeContinuous.IsReadable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            iAcquisitionMode.Value = iAcquisitionModeSingleFrame.Value;

            camera.CamManagedCamera.BeginAcquisition();

            IManagedImage rawImage = camera.CamManagedCamera.GetNextImage();
            if (rawImage.IsIncomplete)
            {
                Console.WriteLine("Image incomplete with image status {0}...", rawImage.ImageStatus);
            }
            else
            {
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
                using(IManagedImage convertedImage = processor.Convert(rawImage, PixelFormatEnums.Mono8))
                {
                    byte[] imagehandle = convertedImage.ManagedData;
                    for (int i = 0; i < convertedImage.Height * convertedImage.Width; i++)
                    {
                        byte pixelValue = imagehandle.ElementAt(i);
                        if ((int) pixelValue > (int) thresholdToSet && defectPixelList.Count < _maxPixelCount)
                        {
                            Add(ref camera,
                                ref defectPixelList,
                                (i % (int) convertedImage.Width),
                                (int)(i / convertedImage.Width),
                                (int) pixelValue);
                            result = PixelCorrectionError.LESS_THAN_255_DEFECTIVE_PIXELS_FOUND;
                        }
                        if (defectPixelList.Count >= _maxPixelCount)
                        {
                            camera.CamManagedCamera.EndAcquisition();
                            iAcquisitionMode.Value = iAcquisitionModeContinuous.Value;
                            Restore(ref camera, ref defectPixelList);
                            return PixelCorrectionError.TOO_MANY_DEFECTIVE_PIXELS;
                        }
                    }
                }
            }

            // Put camera back to continous mode
            camera.CamManagedCamera.EndAcquisition();
            iAcquisitionMode.Value = iAcquisitionModeContinuous.Value;

            return result;
        }

        /// <summary>
        /// Restore to the default pixel correction settings
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="defectPixelList"></param>
        public PixelCorrectionError Restore(ref CameraInformation camera, ref DefectPixelList defectPixelList)
        {
            ICommand IDefectTableFactoryRestore = camera.CamNodeMap.GetNode<ICommand>("DefectTableFactoryRestore");
            if (IDefectTableFactoryRestore == null || !IDefectTableFactoryRestore.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }
            else
            {
                IDefectTableFactoryRestore.Execute();
            }

            // Populate the pixel list
            defectPixelList.Clear();
            GetAllPixelValues(ref camera, ref defectPixelList);
            return PixelCorrectionError.NO_ERROR;
        }

        /// <summary>
        /// Add defect pixel to the list
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="defectPixelList"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        public PixelCorrectionError Add(
            ref CameraInformation camera,
            ref DefectPixelList defectPixelList,
            long x,
            long y,
            int value)
        {
            IInteger iDefectTablePixelCount = camera.CamNodeMap.GetNode<IInteger>("DefectTablePixelCount");
            if (iDefectTablePixelCount == null || !iDefectTablePixelCount.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }
            iDefectTablePixelCount.Value++;

            IInteger iDefectTableIndex = camera.CamNodeMap.GetNode<IInteger>("DefectTableIndex");
            if (iDefectTableIndex == null || !iDefectTableIndex.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }
            iDefectTableIndex.Value =
                iDefectTablePixelCount.Value - 1; // total number of pixels is one more than max index

            IInteger iSensorWidth = camera.CamNodeMap.GetNode<IInteger>("SensorWidth");
            if (iSensorWidth == null || !iSensorWidth.IsReadable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            IInteger iDefectXCoord = camera.CamNodeMap.GetNode<IInteger>("DefectTableCoordinateX");
            if (iDefectXCoord == null || !iDefectXCoord.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            // Ensure X coordinate not out of range
            if (x > iSensorWidth.Value || x < 0)
            {
                return PixelCorrectionError.X_COORDINATE_OUT_OF_RANGE;
            }

            iDefectXCoord.Value = x;

            IInteger iSensorHeight = camera.CamNodeMap.GetNode<IInteger>("SensorHeight");
            if (iSensorHeight == null || !iSensorHeight.IsReadable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            IInteger iDefectYCoord = camera.CamNodeMap.GetNode<IInteger>("DefectTableCoordinateY");
            if (iDefectYCoord == null || !iDefectYCoord.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            // Ensure Y coordinate not out of range
            if (y > iSensorHeight.Value || y < 0)
            {
                return PixelCorrectionError.Y_COORDINATE_OUT_OF_RANGE;
            }

            iDefectYCoord.Value = y;

            // Check to make sure there are no identical pixels in pixel list.
            foreach(DefectPixel defectPixelToCheck in defectPixelList)
            {
                if (defectPixelToCheck.DefectXCoord == x && defectPixelToCheck.DefectYCoord == y)
                {
                    return PixelCorrectionError.PIXEL_SET_ALREADY_EXISTING;
                }
            }

            defectPixelList.Add(new DefectPixel(){
                DefectTablePixelCount = value,
                DefectTableIndex = iDefectTablePixelCount.Value - 1,
                DefectXCoord = x,
                DefectYCoord = y,
            });

            ICommand IDefectTableSave = camera.CamNodeMap.GetNode<ICommand>("DefectTableSave");
            if (IDefectTableSave == null || !IDefectTableSave.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }
            else
            {
                IDefectTableSave.Execute();
            }
            return 0;
        }

        /// <summary>
        /// Remove pixel from the list
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="defectPixelList"></param>
        /// <param name="index"></param>
        public PixelCorrectionError Remove(ref CameraInformation camera, ref DefectPixelList defectPixelList, int index)
        {
            IInteger iDefectTablePixelCount = camera.CamNodeMap.GetNode<IInteger>("DefectTablePixelCount");
            if (iDefectTablePixelCount == null || !iDefectTablePixelCount.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            IInteger iDefectTableIndex = camera.CamNodeMap.GetNode<IInteger>("DefectTableIndex");
            if (iDefectTableIndex == null || !iDefectTableIndex.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            for (int i = index + 1; i < iDefectTablePixelCount.Value; i++)
            {
                iDefectTableIndex.Value = i;
                IInteger iDefectXCoord = camera.CamNodeMap.GetNode<IInteger>("DefectTableCoordinateX");
                if (iDefectXCoord == null || !iDefectXCoord.IsReadable || !iDefectXCoord.IsWritable)
                {
                    return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
                }

                IInteger iDefectYCoord = camera.CamNodeMap.GetNode<IInteger>("DefectTableCoordinateY");
                if (iDefectYCoord == null || !iDefectYCoord.IsReadable || !iDefectYCoord.IsWritable)
                {
                    return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
                }

                long tempx = iDefectXCoord.Value;
                long tempy = iDefectYCoord.Value;
                iDefectTableIndex.Value--;
                iDefectXCoord.Value = tempx;
                iDefectYCoord.Value = tempy;
            }
            iDefectTablePixelCount.Value--;
            // Save changes
            ICommand IDefectTableSave = camera.CamNodeMap.GetNode<ICommand>("DefectTableSave");
            if (IDefectTableSave == null || !IDefectTableSave.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }
            IDefectTableSave.Execute();

            // Remove pixel from pixel list
            defectPixelList.RemoveAt(index);
            return PixelCorrectionError.NO_ERROR;
        }

        /// <summary>
        /// Removes all pixels populated in defect pixel list and repopulates with what is found in the camera nodemap
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="defectPixelList"></param>
        public PixelCorrectionError GetAllPixelValues(ref CameraInformation camera, ref DefectPixelList defectPixelList)
        {
            IBool iDefectCorrectStaticEnable = camera.CamNodeMap.GetNode<IBool>("DefectCorrectStaticEnable");
            if (iDefectCorrectStaticEnable == null || !iDefectCorrectStaticEnable.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }
            iDefectCorrectStaticEnable.Value = true;

            // Store number of defective pixels in PixelCorrection.DefectPixel.defectTablePixelCount
            IInteger iDefectTablePixelCount = camera.CamNodeMap.GetNode<IInteger>("DefectTablePixelCount");
            if (iDefectTablePixelCount == null || !iDefectTablePixelCount.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            // Store the value of the number of defective pixels in this class.
            this.NumDefectPixel = (int) iDefectTablePixelCount.Value;
            // Defect pixel index. Used to determine which pixel to look at.
            // Other nodes reference this node to determine where in the register to lookup
            IInteger iDefectTableIndex = camera.CamNodeMap.GetNode<IInteger>("DefectTableIndex");
            if (iDefectTableIndex == null)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            defectPixelList.Clear();
            for (int i = 0; i < NumDefectPixel; i++)
            // Repopulate pixel list
            {
                // Update the table index.
                iDefectTableIndex.Value = i;

                IInteger iDefectXCoord = camera.CamNodeMap.GetNode<IInteger>("DefectTableCoordinateX");
                if (iDefectXCoord == null || !iDefectXCoord.IsReadable)
                {
                    return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
                }

                IInteger iDefectYCoord = camera.CamNodeMap.GetNode<IInteger>("DefectTableCoordinateY");
                if (iDefectYCoord == null || !iDefectYCoord.IsReadable)
                {
                    return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
                }
                int tempPixelValue = 0;
                defectPixelList.Add(new DefectPixel(){DefectTablePixelCount = tempPixelValue,
                                                      DefectXCoord = iDefectXCoord.Value,
                                                      DefectYCoord = iDefectYCoord.Value});
            }
            return PixelCorrectionError.NO_ERROR;
        }

        /// <summary>
        /// Set correction mode
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="correctionMode"></param>
        public PixelCorrectionError CorrectionMethod(ref CameraInformation camera, String correctionMode)
        {
            IEnum iDefectCorrectionMode = camera.CamNodeMap.GetNode<IEnum>("DefectCorrectionMode");
            if (iDefectCorrectionMode == null || !iDefectCorrectionMode.IsWritable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }
            iDefectCorrectionMode.Value = correctionMode;

            return PixelCorrectionError.NO_ERROR;
        }

        /// <summary>
        /// Save pixel defect table
        /// </summary>
        /// <param name="camera"></param>
        public PixelCorrectionError DefectTableSave(ref CameraInformation camera)
        {
            // Save setttings
            ICommand iDefectTableSave = camera.CamNodeMap.GetNode<ICommand>("DefectTableSave");
            if (iDefectTableSave == null)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }
            iDefectTableSave.Execute();

            return PixelCorrectionError.NO_ERROR;
        }

        /// <summary>
        /// Apply correction method
        /// </summary>
        /// <param name="camera"></param>
        public PixelCorrectionError CorrectionApply(ref CameraInformation camera)
        {
            ICommand iCorrectionApply = camera.CamNodeMap.GetNode<ICommand>("DefectTableApply");
            if (iCorrectionApply == null)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }
            iCorrectionApply.Execute();

            return PixelCorrectionError.NO_ERROR;
        }

        /// <summary>
        /// Checks the following:
        /// - gain is a numerical and within max/min
        /// - threshold is a numerical and within max/min
        /// - x,y is a numerical
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="defectPixelListToCheck"></param>
        /// <param name="gainToCheck"></param>
        /// <param name="exposureTimeToCheck"></param>
        /// <param name="thresholdToCheck"></param>
        /// <param name="XToCheck"></param>
        /// <param name="YToCheck"></param>
        public PixelCorrectionError validateParameters(
            ref CameraInformation camera,
            float gainToCheck,
            float exposureTimeToCheck,
            float temperatureToCheck,
            long thresholdToCheck)
        {
            IFloat iGain = camera.CamNodeMap.GetNode<IFloat>("Gain");
            if (iGain == null || !iGain.IsReadable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            IFloat iExposureTime = camera.CamNodeMap.GetNode<IFloat>("ExposureTime");
            if (iExposureTime == null || !iExposureTime.IsReadable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            IFloat iTemperature = camera.CamNodeMap.GetNode<IFloat>("DeviceTemperature");
            if (iTemperature == null || !iTemperature.IsReadable)
            {
                return PixelCorrectionError.INTERNAL_ERROR_READING_NODES;
            }

            // Ensure gain between min/max and is a number
            if (gainToCheck > iGain.Max || gainToCheck < iGain.Min)
            {
                return PixelCorrectionError.GAIN_OUT_OF_RANGE;
            }

            // Ensure exposure time between min/max
            if (exposureTimeToCheck > iExposureTime.Max || exposureTimeToCheck < iExposureTime.Min)
            {
                return PixelCorrectionError.EXPOSURE_OUT_OF_RANGE;
            }

            // Ensure device temeprature is lower than given temperature
            if (iTemperature.Value > temperatureToCheck)
            {
                return PixelCorrectionError.TEMPERATURE_OUT_OF_RANGE;
            }

            // Ensure threshold value is non negative and less than max
            if (thresholdToCheck < 0 || thresholdToCheck > 255)
            {
                return PixelCorrectionError.THRESHOLD_OUT_OF_RANGE;
            }
            return PixelCorrectionError.NO_ERROR;
        }
    }
}
