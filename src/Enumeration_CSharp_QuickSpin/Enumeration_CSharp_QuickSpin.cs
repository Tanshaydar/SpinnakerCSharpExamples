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
 *	@example Enumeration_CSharp_QuickSpin.cs
 *
 *  @brief Enumeration_CSharp_QuickSpin.cs shows how to enumerate interfaces
 *  and cameras using the QuickSpin API. QuickSpin is a subset of the Spinnaker
 *  library that allows for simpler node access and control. This is a great
 *  example to start learning about QuickSpin.
 *
 *	This example introduces the preparation, use, and cleanup of the system
 *	object, interface and camera lists, interfaces, and cameras. It also
 *	touches on retrieving information from pre-fetched nodes using QuickSpin.
 *	Retrieving node information is the only portion of the example that
 *	differs from Enumeration_CSharp.
 *
 *  A much wider range of topics is covered in the full Spinnaker examples than
 *  in the QuickSpin ones. There are only enough QuickSpin examples to
 *  demonstrate node access and to get started with the API; please see full
 *  Spinnaker examples for further or specific knowledge on a topic.
 *
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics
 */

using System;
using System.Collections.Generic;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace Enumeration_CSharp_QuickSpin
{
    class Program
    {
        // This function queries an interface for its cameras and then prints
        // out some device information.
        int QueryInterface(IManagedInterface managedInterface)
        {
            int result = 0;

            try
            {
                //
                // Print interface display name
                //
                // *** NOTES ***
                // QuickSpin allows for the retrieval of interface information
                // directly from an interface. Because interface information is
                // made available on the transport layer, camera initialization
                // is not required.
                //
                if (managedInterface.TLInterface.InterfaceDisplayName != null &&
                    managedInterface.TLInterface.InterfaceDisplayName.IsReadable)
                {
                    string interfaceDisplayName = managedInterface.TLInterface.InterfaceDisplayName.Value;

                    Console.WriteLine("{0}", interfaceDisplayName);
                }
                else
                {
                    Console.WriteLine("Interface display name not readable");
                }

                //
                // Update list of cameras on the interface
                //
                // *** NOTES ***
                // Updating the camera list on each interface is especially
                // important if there have been any device arrivals or removals
                // since accessing the camera list.
                //
                managedInterface.UpdateCameras();

                //
                // Retrieve list of cameras from the interface
                //
                // *** NOTES ***
                // Camera lists are retrieved from interfaces or the system
                // object. Camera lists received from the system are constituted
                // of all available cameras. Iterating through the cameras can
                // be accomplished with a foreach loop, which will dispose of
                // each camera appropriately. Individual cameras can be accessed
                // using an index.
                //
                // *** LATER ***
                // Camera lists must be disposed of manually. This must be done
                // prior to releasing the system and while still in scope.
                //
                ManagedCameraList camList = managedInterface.GetCameras();

                // Return if no cameras detected
                if (camList.Count == 0)
                {
                    Console.WriteLine("\tNo devices detected.\n");
                    return 0;
                }

                //
                // Print device vendor and model name for each camera on the
                // interface
                //
                // *** NOTES ***
                // Foreach loops and using statements are an eloquent way of
                // accessing interfaces, cameras, and images in Spinnaker. These
                // objects all implement IDisposable, so any object created
                // outside of a foreach loop or using statement must be disposed
                // of manually.
                //
                foreach(IManagedCamera cam in camList)
                {
                    Console.Write("\tDevice {0} ", cam.TLDevice.DeviceSerialNumber);

                    //
                    // Print device vendor name and device model name
                    //
                    // *** NOTES ***
                    // In QuickSpin, accessing nodes does not require first
                    // retrieving a nodemap. Instead, GenICam nodes
                    // are made available directly through the camera, and
                    // transport layer nodes are made available through the
                    // camera's TLDevice and TLStream properties.
                    //
                    // Most camera interaction happens through the GenICam,
                    // which requires the device to be initialized. Often simple
                    // reads, like the ones below, can be accomplished at the
                    // transport layer, which does not require initialization;
                    // please see NodeMapInfo_QuickSpin for additional
                    // information on this topic.
                    //
                    // Availability and readability/writability should be
                    // checked prior to interacting with nodes. In C#,
                    // availability is ensured by checking for null. Readability
                    // and writability are ensured by checking their respective
                    // properties.
                    //
                    if (cam.TLDevice.DeviceVendorName != null && cam.TLDevice.DeviceVendorName.IsReadable)
                    {
                        string deviceVendorName = cam.TLDevice.DeviceVendorName.Value;

                        Console.Write("{0} ", deviceVendorName);
                    }

                    if (cam.TLDevice.DeviceModelName != null && cam.TLDevice.DeviceModelName.IsReadable)
                    {
                        string deviceModelName = cam.TLDevice.DeviceModelName.Value;

                        Console.WriteLine("{0}\n", deviceModelName);
                    }
                }

                //
                // Clear camera list before losing scope
                //
                // *** NOTES ***
                // Camera lists must be cleared before losing scope in order to
                // ensure that references are appropriately broken before
                // releasing the system object.
                //
                camList.Clear();
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // Example entry points; this function sets up the system and retrieves
        // interfaces to feed into the example.
        static int Main(string[] args)
        {
            int result = 0;

            Program program = new Program();

            //
            // Retrieve singleton reference to system object
            //
            // *** NOTES ***
            // Everything originates from the system. Notice that it is
            // implemented as a singleton object, making it impossible to have
            // more than one system.
            //
            // *** LATER ***
            // The system object should be cleared prior to program completion.
            // If not released explicitly, it will release itself automatically.
            //
            ManagedSystem system = new ManagedSystem();

            // Print out current library version
            LibraryVersion spinVersion = system.GetLibraryVersion();
            Console.WriteLine(
                "Spinnaker library version: {0}.{1}.{2}.{3}\n\n",
                spinVersion.major,
                spinVersion.minor,
                spinVersion.type,
                spinVersion.build);

            //
            // Retrieve list of interfaces from the system
            //
            // *** NOTES ***
            // Interface lists are retrieved from the system object. Iterating
            // through all interfaces can be accomplished with a foreach loop,
            // which will dispose of each interface appropriately. Individual
            // interfaces can be accessed using an index.
            //
            // *** LATER ***
            // Interface lists must be disposed of manually. This must be done
            // prior to releasing the system and while still in scope.
            //
            ManagedInterfaceList interfaceList = system.GetInterfaces();

            Console.WriteLine("Number of interfaces detected: {0}\n", interfaceList.Count);

            //
            // Retrieve list of cameras from the system
            //
            // *** NOTES ***
            // Camera lists are retrieved from interfaces or the system object.
            // Camera lists received from an interface are constituted of only
            // the cameras connected to that interface. Iterating through the
            // cameras can be accomplished with a foreach loop, which will
            // dispose of each camera appropriately. Individual cameras can be
            // accessed using an index.
            //
            // *** LATER ***
            // Camera lists must be disposed of manually. This must be done
            // prior to releasing the system and while still in scope.
            //
            ManagedCameraList camList = system.GetCameras();

            Console.WriteLine("Number of cameras detected: {0}\n", camList.Count);

            // Finish if there are no cameras
            if (camList.Count == 0 || interfaceList.Count == 0)
            {
                // Clear camera list before releasing system
                camList.Clear();

                // Clear interface list before releasing system
                interfaceList.Clear();

                // Release system
                system.Dispose();

                Console.WriteLine("Not enough cameras!");
                Console.WriteLine("Done! Press Enter to exit...");
                Console.ReadLine();

                return -1;
            }

            Console.WriteLine("\n*** QUERYING INTERFACES ***\n");

            //
            // Run example on each camera
            //
            // *** NOTES ***
            // Managed interfaces will need to be disposed of after use in order
            // to fully clean up. Using-statements help ensure that this is taken
            // care of; otherwise, interfaces can be disposed of manually by
            // calling Dispose().
            //
            foreach(IManagedInterface managedInterface in interfaceList) using(managedInterface)
            {
                try
                {
                    // Run example
                    result = result | program.QueryInterface(managedInterface);
                }
                catch (SpinnakerException ex)
                {
                    Console.WriteLine("Error: {0}", ex.Message);
                    result = -1;
                }
            }

            // Dispose of cameras
            foreach(IManagedCamera cam in camList)
            {
                cam.Dispose();
            }

            //
            // Clear camera list before releasing system
            //
            // *** NOTES ***
            // Camera lists are not shared pointers and do not automatically
            // clean themselves up and break their own references. Therefore,
            // this must be done manually. The same is true of interface lists.
            //
            camList.Clear();

            //
            // Clear interface list before releasing system
            //
            // *** NOTES ***
            // Camera lists are not shared pointers and do not automatically
            // clean themselves up and break their own references. Therefore,
            // this must be done manually. The same is true of interface lists.
            //
            interfaceList.Clear();

            //
            // Release system
            //
            // *** NOTES ***
            // The system should be released, but if it is not, it will do so
            // itself. It is often at the release of the system (whether manual
            // or automatic) that unbroken references and still registered events
            // will throw an exception.
            //
            system.Dispose();

            Console.WriteLine("\nDone! Press Enter to exit...");
            Console.ReadLine();

            return result;
        }
    }
}
