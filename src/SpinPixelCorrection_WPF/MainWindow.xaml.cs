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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using SpinnakerPixelCorrection;

namespace PixelCorrection_Spinnaker_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Global variables
        ManagedSystem system = new ManagedSystem();
        PixelCorrection PCGlobal = new PixelCorrection();
        DefectPixelList PCDefectPixelListGlobal = new DefectPixelList();
        CameraInformation PCCamInfoGlobal = new CameraInformation();

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Title = string.Format("SpinPixelCorrection Utility");

            this.listView.DataContext = new DefectPixel();
            // Sets the listview_camera to take input from the camera
            this.listView_cameras.DataContext = new CameraInformation();

            // Set default values
            textBox_exposure_time.Text = PCGlobal.ExposureTimeDefault.ToString();
            textBox_gain.Text = PCGlobal.GainDefault.ToString();
            textBox_temperature.Text = PCGlobal.TemperatureDefault.ToString();
            textBox_threshold.Text = PCGlobal.ThresholdDefault.ToString();
            textBox_x_coord.Text = "0";
            textBox_y_coord.Text = "0";

            updateCamListView();
        }

        /// <summary>
        /// Event handler to Window Loaded event.
        /// </summary>
        /// <param name="sender">Window</param>
        /// <param name="e">RoutedEventArgs</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Update pixel list.
        /// </summary>
        public void updateListView()
        {
            listView.Items.Clear();
            try
            {
                for (int i = 0; i < PCDefectPixelListGlobal.Count; i++)
                {
                    listView.Items.Add(PCDefectPixelListGlobal[i]);
                }
            }
            catch (SpinnakerException ex)
            {
                MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
            }
        }

        /// <summary>
        /// Update Camera list
        /// </summary>
        public void updateCamListView()
        {
            // Clear all previous cameras
            PCCamInfoGlobal.CamManagedCamera = null;
            foreach(CameraInformation camInfo in listView_cameras.Items)
            {
                camInfo.CamManagedCamera.Dispose();
            }
            listView_cameras.Items.Clear();

            ManagedCameraList camList = system.GetCameras();

            // Finish if there are no cameras
            if (camList.Count == 0)
            {
                // Clear camera list before releasing system
                camList.Clear();
            }
            foreach(IManagedCamera managedCamera in camList)
            {
                try
                {
                    // Retrieve TL device nodemap and get device info
                    INodeMap nodeMapTLDevice = managedCamera.GetTLDeviceNodeMap();

                    // Initialize camera
                    managedCamera.Init();

                    // Retrieve GenICam nodemap
                    INodeMap nodeMap = managedCamera.GetNodeMap();

                    // Determine if pixel correction applied...
                    IBool iDefectCorrectStaticEnable = nodeMap.GetNode<IBool>("DefectCorrectStaticEnable");

                    // Only add camera if it is Gen3 camera
                    if (iDefectCorrectStaticEnable != null)
                    {
                        string deviceDefectCorrectEnable = iDefectCorrectStaticEnable.Value.ToString();
                        string deviceModelName = "";
                        string deviceSerialNumber = "";
                        string deviceType = "";

                        IString iDeviceModelName = nodeMapTLDevice.GetNode<IString>("DeviceModelName");

                        if (iDeviceModelName != null && iDeviceModelName.IsReadable)
                        {
                            deviceModelName = iDeviceModelName.Value;
                        }

                        IString iDeviceSerialNumber = nodeMapTLDevice.GetNode<IString>("DeviceSerialNumber");

                        if (iDeviceSerialNumber != null && iDeviceSerialNumber.IsReadable)
                        {
                            deviceSerialNumber = iDeviceSerialNumber.Value;
                        }

                        IEnum iDeviceType = nodeMapTLDevice.GetNode<IEnum>("DeviceType");

                        if (iDeviceType != null && iDeviceType.IsReadable)
                        {
                            deviceType = iDeviceType.Value;
                        }

                        // Deinit camera before we add it to the ListView
                        managedCamera.DeInit();

                        listView_cameras.Items.Add(new CameraInformation(){
                            CamManagedCamera = managedCamera,
                            DeviceModelName = deviceModelName,
                            DeviceSerialNumber = deviceSerialNumber,
                            DeviceDefectCorrectEnable = deviceDefectCorrectEnable,
                            DeviceType = deviceType,
                        });
                    }
                    else
                    {
                        managedCamera.DeInit();
                    }
                }
                catch (SpinnakerException ex)
                {
                    MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
                }
            }

            // Select the first camera in the list
            if (listView_cameras.Items.Count > 0)
            {
                listView_cameras.SelectedIndex = 0;
            }

            camList.Clear();
        }

        /// <summary>
        /// Change camera selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView_cameras_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Deinitialize the previously selected camera
            if (PCCamInfoGlobal.CamManagedCamera != null)
            {
                PCCamInfoGlobal.CamManagedCamera.DeInit();
            }

            PixelCorrection.PixelCorrectionError result = PixelCorrection.PixelCorrectionError.NO_ERROR;
            // Same thing as updateListView but will set all values to zero afterwards because the gain threshold values
            // are not stored. They don't make sense to store.
            listView.Items.Clear();
            try
            {
                if (listView_cameras.SelectedItem != null)
                {

                    // Set selected camera to camlist
                    CameraInformation camInfo = (CameraInformation) listView_cameras.SelectedItem;
                    PCCamInfoGlobal.CamManagedCamera = camInfo.CamManagedCamera;
                    PCCamInfoGlobal.CamManagedCamera.Init();

                    PCCamInfoGlobal.CamNodeMap = PCCamInfoGlobal.CamManagedCamera.GetNodeMap();

                    result = PCGlobal.GetAllPixelValues(ref PCCamInfoGlobal, ref PCDefectPixelListGlobal);
                    if (result == PixelCorrection.PixelCorrectionError.INTERNAL_ERROR_READING_NODES)
                    {
                        MessageBox.Show("There is internal errors reading nodes");
                    }

                    for (int i = 0; i < PCGlobal.NumDefectPixel; i++)
                    {
                        listView.SelectedIndex = i;
                        listView.Items.Add(PCDefectPixelListGlobal[i]);
                    }
                }
            }
            catch (SpinnakerException ex)
            {
                MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
            }
        }

        /// <summary>
        /// Apply Filter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_apply_filter_Click(object sender, RoutedEventArgs e)
        {
            PixelCorrection.PixelCorrectionError result = PixelCorrection.PixelCorrectionError.NO_ERROR;
            result = (isNumerical() == 0) ? PixelCorrection.PixelCorrectionError.NO_ERROR
                                          : PixelCorrection.PixelCorrectionError.INTERNAL_ERROR;
            try
            {
                if (result != PixelCorrection.PixelCorrectionError.INTERNAL_ERROR_READING_NODES)
                {
                    result = PCGlobal.validateParameters(
                        ref PCCamInfoGlobal,
                        float.Parse(textBox_gain.Text),
                        float.Parse(textBox_exposure_time.Text),
                        float.Parse(textBox_temperature.Text),
                        long.Parse(textBox_threshold.Text));

                    if (result == PixelCorrection.PixelCorrectionError.NO_ERROR)
                    {
                        result = PCGlobal.ApplyThreshold(
                            ref PCCamInfoGlobal,
                            double.Parse(textBox_gain.Text),
                            double.Parse(textBox_exposure_time.Text),
                            double.Parse(textBox_threshold.Text),
                            ref PCDefectPixelListGlobal);

                        if (result == PixelCorrection.PixelCorrectionError.NO_ERROR)
                        {
                            MessageBox.Show("Threshold complete did not find any pixels within threshold");
                        }
                        if (result == PixelCorrection.PixelCorrectionError.LESS_THAN_255_DEFECTIVE_PIXELS_FOUND)
                        {
                            MessageBox.Show("Threshold complete less than 255 defective pixels...");
                        }
                        if (result == PixelCorrection.PixelCorrectionError.TOO_MANY_DEFECTIVE_PIXELS)
                        {
                            MessageBox.Show("Threshold incomplete, too many defective pixels");
                        }
                        if (result == PixelCorrection.PixelCorrectionError.INTERNAL_ERROR_READING_NODES)
                        {
                            MessageBox.Show("There is internal errors");
                        }
                        updateListView();
                    }
                    else
                    {
                        if (result == PixelCorrection.PixelCorrectionError.GAIN_OUT_OF_RANGE)
                        {
                            MessageBox.Show("Gain is set too high or too low");
                        }
                        if (result == PixelCorrection.PixelCorrectionError.EXPOSURE_OUT_OF_RANGE)
                        {
                            MessageBox.Show("Expsoure time is set too high or too low");
                        }
                        if (result == PixelCorrection.PixelCorrectionError.TEMPERATURE_OUT_OF_RANGE)
                        {
                            MessageBox.Show("Temperature is set too low");
                        }
                        if (result == PixelCorrection.PixelCorrectionError.THRESHOLD_OUT_OF_RANGE)
                        {
                            MessageBox.Show("threshold is set too high or too low");
                        }
                        if (result == PixelCorrection.PixelCorrectionError.PIXEL_SET_ALREADY_EXISTING)
                        {
                            MessageBox.Show(
                                "Pixel set to add is identical to one already in the pixel correction utility");
                        }
                        if (result == PixelCorrection.PixelCorrectionError.INTERNAL_ERROR_READING_NODES)
                        {
                            MessageBox.Show("There is internal errors");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Non numerical value entered");
                }
            }
            catch (SpinnakerException ex)
            {
                MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
            }
        }

        /// <summary>
        /// Restore it to defaults
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_restore_defaults_Click(object sender, RoutedEventArgs e)
        {
            PixelCorrection.PixelCorrectionError result = PixelCorrection.PixelCorrectionError.NO_ERROR;
            try
            {
                result = PCGlobal.Restore(ref PCCamInfoGlobal, ref PCDefectPixelListGlobal);
                if (result == PixelCorrection.PixelCorrectionError.INTERNAL_ERROR_READING_NODES)
                {
                    MessageBox.Show("There is internal errors");
                }
            }
            catch (SpinnakerException ex)
            {
                MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
            }

            textBox_exposure_time.Text = PCGlobal.ExposureTimeDefault.ToString();
            textBox_gain.Text = PCGlobal.GainDefault.ToString();
            textBox_temperature.Text = PCGlobal.TemperatureDefault.ToString();
            textBox_threshold.Text = PCGlobal.ThresholdDefault.ToString();
            updateListView();
        }

        /// <summary>
        /// Add pixel to the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_add_Click(object sender, RoutedEventArgs e)
        {
            PixelCorrection.PixelCorrectionError result = PixelCorrection.PixelCorrectionError.NO_ERROR;
            try
            {
                // If we cannot convert the input, just set the coordinates as invalid
                long x_coord = long.TryParse(textBox_x_coord.Text, out x_coord) ? x_coord : -1;
                long y_coord = long.TryParse(textBox_y_coord.Text, out y_coord) ? y_coord : -1;

                result = PCGlobal.Add(ref PCCamInfoGlobal, ref PCDefectPixelListGlobal, x_coord, y_coord, 0);
                if (result == PixelCorrection.PixelCorrectionError.INTERNAL_ERROR_READING_NODES)
                {
                    MessageBox.Show("There is internal errors");
                }
                if (result == PixelCorrection.PixelCorrectionError.X_COORDINATE_OUT_OF_RANGE)
                {
                    MessageBox.Show("X coordinate is out of range");
                }
                if (result == PixelCorrection.PixelCorrectionError.Y_COORDINATE_OUT_OF_RANGE)
                {
                    MessageBox.Show("Y coordinate is out of range");
                }
                if (result == PixelCorrection.PixelCorrectionError.PIXEL_SET_ALREADY_EXISTING)
                {
                    MessageBox.Show("Pixel set to add is identical to one already in the pixel correction utility");
                }

                updateListView();
                textBox_x_coord.Text = "0";
                textBox_y_coord.Text = "0";
            }
            catch (SpinnakerException ex)
            {
                MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
            }
        }

        /// <summary>
        /// Remove the pixel from the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_remove_Click(object sender, RoutedEventArgs e)
        {
            PixelCorrection.PixelCorrectionError result = PixelCorrection.PixelCorrectionError.NO_ERROR;
            try
            {
                int selectedIdx = listView.Items.IndexOf(listView.SelectedItem);
                if (selectedIdx >= 0)
                {
                    result = PCGlobal.Remove(
                        ref PCCamInfoGlobal,
                        ref PCDefectPixelListGlobal,
                        listView.Items.IndexOf(listView.SelectedItem));

                    if (result == PixelCorrection.PixelCorrectionError.INTERNAL_ERROR_READING_NODES)
                    {
                        MessageBox.Show("There is internal errors");
                    }
                }
                else
                {
                    MessageBox.Show("No pixel selected");
                }

                updateListView();
            }
            catch (SpinnakerException ex)
            {
                MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
            }
        }

        /// <summary>
        /// Choose correction method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_correction_method_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).ContextMenu.IsEnabled = true;
            (sender as Button).ContextMenu.PlacementTarget = (sender as Button);
            (sender as Button).ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            (sender as Button).ContextMenu.IsOpen = true;
        }

        /// <summary>
        /// Correcton method average chosen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void correction_method_contextmenu_average_Click(object sender, RoutedEventArgs e)
        {
            PixelCorrection.PixelCorrectionError result = PixelCorrection.PixelCorrectionError.NO_ERROR;
            try
            {
                label_description.Content =
                    "Average: The adjacent 8 pixels are averaged to define the the defective pixel's value";
                button_correction_method.Content = "Average";

                // Enable available correction method
                IEnum iCorrectionMode = PCCamInfoGlobal.CamNodeMap.GetNode<IEnum>("DefectCorrectionMode");
                if (iCorrectionMode == null || !iCorrectionMode.IsReadable)
                {
                    MessageBox.Show("There is internal errors");
                }
                IEnumEntry iCorrectionModeZero = iCorrectionMode.GetEntryByName("Average");
                if (iCorrectionModeZero.IsImplemented != false)
                {
                    result = PCGlobal.CorrectionMethod(ref PCCamInfoGlobal, "Average");
                    if (result == PixelCorrection.PixelCorrectionError.INTERNAL_ERROR_READING_NODES)
                    {
                        MessageBox.Show("There is internal errors");
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Defect Pixel Correction method \"Average\" is not available for this camera. Please try different method.");
                }
            }
            catch (SpinnakerException ex)
            {
                MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
            }
        }

        /// <summary>
        /// Correction method zero chosen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void correction_method_contextmenu_zero_Click(object sender, RoutedEventArgs e)
        {
            PixelCorrection.PixelCorrectionError result = PixelCorrection.PixelCorrectionError.NO_ERROR;
            try
            {
                label_description.Content = "Zero: The defective pixel's value is set to zero";
                button_correction_method.Content = "Zero";

                // Enable available correction method
                IEnum iCorrectionMode = PCCamInfoGlobal.CamNodeMap.GetNode<IEnum>("DefectCorrectionMode");
                if (iCorrectionMode == null || !iCorrectionMode.IsReadable)
                {
                    MessageBox.Show("There is internal errors");
                }
                IEnumEntry iCorrectionModeZero = iCorrectionMode.GetEntryByName("Zero");
                if (iCorrectionModeZero.IsImplemented != false)
                {
                    result = PCGlobal.CorrectionMethod(ref PCCamInfoGlobal, "Zero");
                    if (result == PixelCorrection.PixelCorrectionError.INTERNAL_ERROR_READING_NODES)
                    {
                        MessageBox.Show("There is internal errors");
                    }
                }
                else
                {
                    // Restore back to default value
                    button_correction_method.Content = "Average";
                    MessageBox.Show(
                        "Defect Pixel Correction method \"Zero\" is not available for this camera. Please try different method.");
                }
            }
            catch (SpinnakerException ex)
            {
                MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
            }
        }

        /// <summary>
        /// Correcton method highlight chosen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void correction_method_contextmenu_highlight_Click(object sender, RoutedEventArgs e)
        {
            PixelCorrection.PixelCorrectionError result = PixelCorrection.PixelCorrectionError.NO_ERROR;
            try
            {
                label_description.Content =
                    "Highlight: The defective pixel's value is set to max pixel value (eg 255 for 8bit pixel format). This is generally used for debugging processes or can be used for frontlight applications (microscopes)";
                button_correction_method.Content = "Highlight";

                // Enable available correction method
                IEnum iCorrectionMode = PCCamInfoGlobal.CamNodeMap.GetNode<IEnum>("DefectCorrectionMode");
                if (iCorrectionMode == null || !iCorrectionMode.IsReadable)
                {
                    MessageBox.Show("There is internal errors");
                }
                IEnumEntry iCorrectionModeZero = iCorrectionMode.GetEntryByName("Highlight");
                if (iCorrectionModeZero.IsImplemented != false)
                {
                    result = PCGlobal.CorrectionMethod(ref PCCamInfoGlobal, "Highlight");
                    if (result == PixelCorrection.PixelCorrectionError.INTERNAL_ERROR_READING_NODES)
                    {
                        MessageBox.Show("There is internal errors");
                    }
                }
                else
                {
                    // Restore back to default value
                    button_correction_method.Content = "Average";
                    MessageBox.Show(
                        "Defect Pixel Correction method \"Highlight\" is not available for this camera. Please try different method.");
                }
            }
            catch (SpinnakerException ex)
            {
                MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
            }
        }

        /// <summary>
        /// Correcton method default chosen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void correction_method_contextmenu_default_Click(object sender, RoutedEventArgs e)
        {
            PixelCorrection.PixelCorrectionError result = PixelCorrection.PixelCorrectionError.NO_ERROR;
            try
            {
                label_description.Content =
                    "Correction Method: Select the algorithm used to determine the defective pixel value";
                button_correction_method.Content = "Correction Method";

                // Enable available correction method
                IEnum iCorrectionMode = PCCamInfoGlobal.CamNodeMap.GetNode<IEnum>("DefectCorrectionMode");
                if (iCorrectionMode == null || !iCorrectionMode.IsReadable)
                {
                    MessageBox.Show("There is internal errors");
                }
                IEnumEntry iCorrectionModeZero = iCorrectionMode.GetEntryByName("Average");
                if (iCorrectionModeZero.IsImplemented != false)
                {
                    result = PCGlobal.CorrectionMethod(ref PCCamInfoGlobal, "Average");
                    if (result == PixelCorrection.PixelCorrectionError.INTERNAL_ERROR_READING_NODES)
                    {
                        MessageBox.Show("There is internal errors");
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Defect Pixel Correction method \"Average\" is not available for this camera. Please try different method.");
                }
            }
            catch (SpinnakerException ex)
            {
                MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
            }
        }

        /// <summary>
        /// Check if values are numerical
        /// </summary>
        private int isNumerical()
        {
            int result = 1;
            double tempVar = 0;
            int tempVarInt = 0;
            // Ensure a camera is selected
            if (listView_cameras.SelectedItems.Count < 0)
            {
                result = -1;
            }

            // Ensure gain between min/max and is a number
            if (!double.TryParse(textBox_gain.Text, out tempVar))
            {
                result = -1;
            }

            // Ensure exposure time between min/max
            if (!double.TryParse(textBox_exposure_time.Text, out tempVar))
            {
                result = -1;
            }

            // Ensure the threshold value isn't above pixel format size.
            // Right now I am setting this as a constant.
            if (!int.TryParse(textBox_threshold.Text, out tempVarInt))
            {
                result = -1;
            }

            return result;
        }

        /// <summary>
        /// Recan the bus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_rescan_bus_Click(object sender, RoutedEventArgs e)
        {
            updateCamListView();
        }

#region WINDOW_EVENTS
        /// <summary>
        /// Close the main window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, EventArgs e)
        {
            ManagedCameraList camList = system.GetCameras();
            foreach(IManagedCamera managedCamera in camList) using(managedCamera)
            {
                try
                {
                    managedCamera.DeInit();
                }
                catch (SpinnakerException ex)
                {
                    MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
                }
            }
            camList.Clear();
            system.Dispose();
            Application.Current.Shutdown();
        }
#endregion

        /// <summary>
        /// Apply correction method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_correction_apply_Click(object sender, RoutedEventArgs e)
        {
            PixelCorrection.PixelCorrectionError result = PixelCorrection.PixelCorrectionError.NO_ERROR;
            try
            {
                result = PCGlobal.CorrectionApply(ref PCCamInfoGlobal);
                if (result == PixelCorrection.PixelCorrectionError.INTERNAL_ERROR_READING_NODES)
                {
                    MessageBox.Show("There is internal errors");
                }
            }
            catch (SpinnakerException ex)
            {
                MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
            }
        }

        /// <summary>
        /// Save pixel table
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_table_save_Click(object sender, RoutedEventArgs e)
        {
            PixelCorrection.PixelCorrectionError result = PixelCorrection.PixelCorrectionError.NO_ERROR;
            try
            {
                result = PCGlobal.DefectTableSave(ref PCCamInfoGlobal);
                if (result == PixelCorrection.PixelCorrectionError.INTERNAL_ERROR_READING_NODES)
                {
                    MessageBox.Show("There is internal errors");
                }
            }
            catch (SpinnakerException ex)
            {
                MessageBox.Show(String.Format("Error: {0}", ex.Message), "Error");
            }
        }
    }
}