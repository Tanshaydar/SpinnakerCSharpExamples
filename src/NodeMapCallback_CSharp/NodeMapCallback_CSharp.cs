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
 *  @example NodeMapCallback_CSharp.cs
 *
 *  @brief NodeMapCallback_CSharp.cs shows how to use GenICam-defined callbacks.
 *  It relies on information provided in the Enumeration_CSharp,
 *  Acquisition_CSharp, and NodeMapInfo_CSharp examples. As callbacks are very
 *  similar to events, it may be a good idea to explore this example prior to
 *  tackling the events examples.
 *
 *  This example focuses on creating, registering, using, and unregistering
 *  callbacks. A callback requires a function signature, which allows it to be
 *  registered to and access a node. Events, while slightly more complex,
 *  follow this same pattern.
 *
 *  Once comfortable with NodeMapCallback_CSharp, we suggest checking out any
 *  of the events examples: DeviceEvents_CSharp, EnumerationEvents_CSharp,
 *  ImageEvents_CSharp, or Logging_CSharp.
 *  
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics  
 */

using System;
using System.Collections.Generic;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace NodeMapCallback_CSharp
{
    class Program
    {
        // This is the first of two callback functions. Notice the function
        // signature. This callback function will be registered to the height
        // node.
        void OnHeightNodeUpdate(INode node)
        {
            IInteger iHeight = (IInteger) node;

            Console.WriteLine("Height callback message:");
            Console.WriteLine("\tHeight changed to {0}...\n", iHeight.Value);
        }

        // This is the second of two callback functions. Notice that despite
        // different names, everything else is exactly the same as the first.
        // This callback function will be registered to the gain node.
        void OnGainNodeUpdate(INode node)
        {
            IFloat iGain = (IFloat) node;

            Console.WriteLine("Gain callback message:");
            Console.WriteLine("\tGain changed to {0}...\n", iGain.Value);
        }

        // This function prepares the example by disabling automatic gain,
        // creating the callbacks, and registering them to their respective
        // nodes.
        int ConfigureCallbacks(INodeMap nodeMap)
        {
            int result = 0;

            Console.WriteLine("\n*** CONFIGURING CALLBACKS ***\n");

            try
            {
                //
                // Turn off automatic gain
                //
                // *** NOTES ***
                // Automatic gain prevents the manual configuration of gain
                // times and needs to be turned off for this example.
                //
                // *** LATER ***
                // Automatic gain is turned off at the end of the example in
                // order to restore the camera to its default state.
                //
                IEnum iGainAuto = nodeMap.GetNode<IEnum>("GainAuto");
                if (iGainAuto == null || !iGainAuto.IsWritable || !iGainAuto.IsReadable)
                {
                    Console.WriteLine("Unable to disable automatic gain (enum retrieval). Aborting...\n");
                    return -1;
                }

                IEnumEntry iGainAutoOff = iGainAuto.GetEntryByName("Off");
                if (iGainAutoOff == null || !iGainAutoOff.IsReadable)
                {
                    Console.WriteLine("Unable to disable automatic gain (entry retrieval). Aborting...\n");
                    return -1;
                }

                iGainAuto.Value = iGainAutoOff.Value;

                Console.WriteLine("Automatic gain disabled...");

                //
                // Register callback to height node
                //
                // *** NOTES ***
                // Callbacks need to be registered to nodes, which should be
                // writable if the callback is to ever be triggered. Notice
                // that callback registration returns an integer - this integer
                // is important at the end of the example for deregistration.
                //
                // *** LATER ***
                // Each callback needs to be unregistered individually before
                // releasing the system or an exception will be thrown.
                //
                IInteger iHeight = nodeMap.GetNode<IInteger>("Height");
                if (iHeight == null || !iHeight.IsWritable)
                {
                    Console.WriteLine("Unable to register height callback. Aborting...\n");
                    return -1;
                }

                iHeight.Updated += new NodeEventHandler(OnHeightNodeUpdate);

                Console.WriteLine("Height callback registered...");

                //
                // Register callback to gain node
                //
                // *** NOTES ***
                // Depending on the specific goal of the function, it can be
                // important to notice the node type that a callback is
                // registered to. Notice in the callback functions above that
                // the callback registered to height casts its node as an
                // integer whereas the callback registered to gain casts as a
                // float.
                //
                // *** LATER ***
                // Each callback needs to be unregistered individually before
                // releasing the system or an exception will be thrown.
                //
                IFloat iGain = nodeMap.GetNode<IFloat>("Gain");
                if (iGain == null || !iGain.IsWritable)
                {
                    Console.WriteLine("Unable to register gain callback. Aborting...\n");
                    return -1;
                }

                iGain.Updated += new NodeEventHandler(OnGainNodeUpdate);

                Console.WriteLine("Gain callback registered...\n");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function demonstrates the triggering of the nodemap callbacks.
        // First it changes height, which executes the callback registered to
        // the height node, and then it changes gain, which executes the
        // callback registered to the gain node.
        int ChangeHeightAndGain(INodeMap nodeMap)
        {
            int result = 0;

            Console.WriteLine("\n*** CHANGE HEIGHT & GAIN ***\n");

            try
            {
                //
                // Change height to trigger height callback
                //
                // *** NOTES ***
                // Notice that changing the height only triggers the callback
                // function registered to the height node.
                //
                IInteger iHeight = nodeMap.GetNode<IInteger>("Height");
                if (iHeight == null || !iHeight.IsWritable)
                {
                    Console.WriteLine("Unable to retrieve height. Aborting...\n");
                    return -1;
                }

                Console.WriteLine("Regular function message:");
                Console.WriteLine("\tHeight about to be changed to {0}...\n", iHeight.Max);

                iHeight.Value = iHeight.Max;

                //
                // Change gain to trigger gain callback
                //
                // *** NOTES ***
                // The same is true of changing the gain node; changing a node
                // will only ever trigger the callback function (or functions)
                // currently registered to it.
                //
                IFloat iGain = nodeMap.GetNode<IFloat>("Gain");
                if (iGain == null || !iGain.IsWritable)
                {
                    Console.WriteLine("Unable to retrieve gain. Aborting...\n");
                    return -1;
                }

                Console.WriteLine("Regular function message:");
                Console.WriteLine("\tGain about to be changed to {0}...\n", iGain.Max / 2.0);

                iGain.Value = iGain.Max / 2;
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function cleans up the example by deregistering the callbacks
        // and turning automatic gain back on.
        int ResetCallbacks(INodeMap nodeMap)
        {
            int result = 0;

            try
            {
                //
                // Deregister callbacks
                //
                // *** NOTES ***
                // It is important to deregister each callback function from
                // each node that it is registered to.
                //
                IInteger iHeight = nodeMap.GetNode<IInteger>("Height");
                if (iHeight == null || !iHeight.IsWritable)
                {
                    Console.WriteLine("Unable to unregister height callback. Aborting...\n");
                    return -1;
                }

                iHeight.Updated -= new NodeEventHandler(OnHeightNodeUpdate);

                Console.WriteLine("Height callback unregistered...");

                IFloat iGain = nodeMap.GetNode<IFloat>("Gain");
                if (iGain == null || !iGain.IsWritable)
                {
                    Console.WriteLine("Unable to unregister gain callback. Aborting...\n");
                    return -1;
                }

                iGain.Updated -= new NodeEventHandler(OnGainNodeUpdate);

                Console.WriteLine("Gain callback unregistered...");

                //
                // Turn automatic gain back on
                //
                // *** NOTES ***
                // Automatic gain is turned on in order to return the camera to
                // its default state.
                //
                IEnum iGainAuto = nodeMap.GetNode<IEnum>("GainAuto");
                if (iGainAuto == null || !iGainAuto.IsWritable || !iGainAuto.IsReadable)
                {
                    Console.WriteLine("Unable to enable automatic gain (enum retrieval). Aborting...\n");
                    return -1;
                }

                IEnumEntry iGainAutoContinuous = iGainAuto.GetEntryByName("Continuous");
                if (iGainAutoContinuous == null || !iGainAutoContinuous.IsReadable)
                {
                    Console.WriteLine("Unable to enable automatic gain (entry retrieval). Aborting...\n");
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

                // Configure callbacks
                err = ConfigureCallbacks(nodeMap);
                if (err < 0)
                {
                    return err;
                }

                // Acquire images
                result = result | ChangeHeightAndGain(nodeMap);

                // Reset callbacks
                result = result | ResetCallbacks(nodeMap);

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
