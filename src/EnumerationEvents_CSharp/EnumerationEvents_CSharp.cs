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
 *  @example EnumerationEvents_CSharp.cs
 *
 *  @brief EnumerationEvents_CSharp.cs explores arrival and removal events on
 *  interfaces and the system. It relies on information provided in the
 *  Enumeration_CSharp, Acquisition_CSharp, and NodeMapInfo_CSharp examples.
 *
 *  It can also be helpful to familiarize yourself with the
 *  NodeMapCallback_CSharp example, as nodemap callbacks follow the same
 *  general procedure as events, but with a few less steps.
 *
 *  This example creates a user-defined class, InterfaceEventListener. It allows
 *  the user to define any properties, parameters, and the event itself while
 *  the parent class, ManagedInterfaceEventHandler, allows the child class to interface
 *  with the Spinnaker SDK.
 *
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics
 */

using System;
using System.Collections.Generic;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using System.Threading;

namespace EnumerationEvents_CSharp
{
    class Program
    {
        // This class defines the properties and methods for device arrivals and removals
        // on an interface. Take special note of the signatures of the OnDeviceArrival()
        // and OnDeviceRemoval() methods. Also, enumeration event listeners must inherit
        // from ManagedInterfaceEventHandler whether they are to be registered to the system
        // or an interface.
        class InterfaceEventListener : ManagedInterfaceEventHandler
        {
            private bool registerToSystem;
            private string interfaceID;
            private IManagedInterface managedInterface;
            private IManagedSystem managedSystem;

            //
            // Set the constructor
            //
            // *** NOTES ***
            // When constructing a generic InterfaceEventListener to be registered to the system,
            // the handler will not have knowledge of which interface triggered the event callbacks.
            // On the other hand, InterfaceEventListener does not need knowledge about the system if we
            // are constructing it to be registered to a specific interface.
            //
            public InterfaceEventListener(string ifaceID, IManagedInterface manIface)
            {
                interfaceID = ifaceID;
                managedInterface = manIface;
                registerToSystem = false;
            }

            public InterfaceEventListener(IManagedSystem manSystem)
            {
                managedSystem = manSystem;
                registerToSystem = true;
                interfaceID = "";
            }

            // This method defines the arrival event callback on an interface. It prints out
            // the device serial number of the camera arriving and the interface ID.
            // The argument is the serial number of the camera that triggered the arrival event.
            // If the event handler was constructed to be registered to the system as a generic
            // InterfaceEventListener, then we just retrieve the number of cameras
            // currently connected on the system and print it out.
            protected override void OnDeviceArrival(IManagedCamera camera)
            {
                if (registerToSystem)
                {
                    int deviceCount = managedSystem.GetCameras().Count;
                    PrintGenericHandlerMessage(deviceCount);
                }
                else
                {
                    Console.WriteLine("Interface event listener:");
                    Console.WriteLine("\tDevice {0} has arrived on interface {1}.\n", camera.TLDevice.DeviceSerialNumber, interfaceID);
                }
            }

            // This method defines the removal event callback on an interface. It prints out the
            // device serial number of the camera being removed and the interface ID.
            // The argument is the serial number of the camera that triggered the removal event.
            // If the event handler was constructed to be registered to the system as a generic
            // InterfaceEventListener, then we just retrieve the number of cameras
            // currently connected on the system and print it out.
            protected override void OnDeviceRemoval(IManagedCamera camera)
            {
                if (registerToSystem)
                {
                    int deviceCount = managedSystem.GetCameras().Count;
                    PrintGenericHandlerMessage(deviceCount);
                }
                else
                {
                    Console.WriteLine("Interface event listener:");
                    Console.WriteLine("\tDevice {0} was removed from interface {1}.\n", camera.TLDevice.DeviceSerialNumber, interfaceID);
                }
            }

            // Helper function to print the number of devices on an interface event registered to the system.
            private void PrintGenericHandlerMessage(int deviceCount)
            {
                Console.WriteLine("Generic interface event handler:");
                bool singular = deviceCount == 1;
                Console.WriteLine(
                    "\tThere {0} {1} {2} on the system.\n",
                    (singular ? "is"
                     : "are"),
                    deviceCount,
                    (singular ? "device"
                     : "devices"));
            }

            // This method returns the interface ID that the interface event handler is bound to.
            public string GetInterfaceId()
            {
                return interfaceID;
            }
        }

        // This class defines the properties and methods of the system event listener that handles
        // interface arrival and removal events on the system. Take special note of the signatures
        // of the OnInterfaceArrival() and OnInterfaceRemoval() methods.
        // Interface enumeration event handlers must inherit from ManagedSystemEventHandler.
        class SystemEventListener : ManagedSystemEventHandler
        {
            private ManagedSystem system;
            private readonly Mutex eventListenersMutex;
            private List<InterfaceEventListener>interfaceEventListeners;
            private InterfaceEventListener interfaceEventListenerOnSystem;

            public SystemEventListener(ManagedSystem sys)
            {
                system = sys;
                eventListenersMutex = new Mutex();
                interfaceEventListeners = new List<InterfaceEventListener>();
            }

            // This method defines the interface arrival event callback on the system.
            // It first prints the ID of the arriving interface, then
            // registers the interface event on the newly arrived interface.
            //
            // *** NOTES ***
            // Only arrival events for GEV interfaces are currently supported.
            protected override void OnInterfaceArrival(IManagedInterface iface)
            {
                Console.WriteLine("System event handler:");
                Console.WriteLine("\tInterface '{0}' has arrived on the system.\n", iface.TLInterface.InterfaceID);

                // UpdateInterfaceList() only updates newly arrived or newly removed interfaces.
                // In particular, after this call:
                //
                // - Any pre-existing interfaces will still be valid.
                // - Newly removed interfaces will be invalid.
                //
                // *** NOTES ***
                // - Invalid interfaces will be re-validated if the interface comes back (arrives) with the same
                // interface ID. If the interface ID changes, you can use the pointer populated by this callback or 
                // you must get the new interface object from the updated interface list in order to access this interface.
                //
                // - Interface indices used to access an interface with GetInterfaces() may change after updating the
                // interface list. The interface at a particular index cannot be expected to remain at that index after
                // calling UpdateInterfaceList().
                system.UpdateInterfaceList();
                
                // Select interface
                INodeMap nodeMap = iface.GetTLNodeMap();

                IString interfaceIDNode = nodeMap.GetNode<IString>("InterfaceID");

                ManagedCameraList cameraList = iface.GetCameras();
                int numCameras = cameraList.Count;
                for (uint camIdx = 0; camIdx < numCameras; camIdx++)
                {
                    IManagedCamera manCamera = cameraList.GetByIndex(camIdx);
                    INodeMap nodeMapTLDevice = manCamera.GetTLDeviceNodeMap();
                    IString deviceSerialNode = nodeMapTLDevice.GetNode<IString>("DeviceSerialNumber");
                    if (deviceSerialNode != null && deviceSerialNode.IsReadable)
                    {
                        string deviceSerialNumber = deviceSerialNode.Value;
                        Console.WriteLine(
                            "\tDevice {0} is connected to interface '{1}'.", deviceSerialNumber, iface.TLInterface.InterfaceID);
                    }
                }

                // Create interface event
                {
                    eventListenersMutex.WaitOne();

                    try
                    {
                        InterfaceEventListener interfaceEventListener =
                            new InterfaceEventListener(iface.TLInterface.InterfaceID, iface);
                        interfaceEventListeners.Add(interfaceEventListener);

                        // Register interface event listener
                        iface.RegisterEventHandler(interfaceEventListener);
                        Console.WriteLine("Event handler registered to interface '{0}'...\n", iface.TLInterface.InterfaceID);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            "Error registering interface event handler to '{0}': {1}", iface.TLInterface.InterfaceID, ex.Message);
                    }
                    finally
                    {
                        eventListenersMutex.ReleaseMutex();
                    }
                }
            }

            // This method defines the interface removal event callback on the system.
            // It prints the ID of the interface removed.
            //
            // *** NOTES ***
            // Only removal events for GEV interfaces are currently supported.
            protected override void OnInterfaceRemoval(IManagedInterface iface)
            {
                Console.WriteLine("System event handler:");
                Console.WriteLine("\tInterface '{0}' was removed from the system.\n", iface.TLInterface.InterfaceID);

                // Interface indices used to access an interface with GetInterfaces() may change after updating the
                // interface list. The interface at a particular index cannot be expected to remain at that index after
                // calling UpdateInterfaceList().
                system.UpdateInterfaceList();

                // Find the event handler that was registered to the removed interface and remove it.
                // Interface event handlers are automatically unregistered when the interface is removed so it is not
                // necessary to manually unregister them.
                {
                    eventListenersMutex.WaitOne();
                    try
                    {
                        int handlerIdx = 0;
                        for (handlerIdx = 0; handlerIdx < interfaceEventListeners.Count; handlerIdx++)
                        {
                            if (interfaceEventListeners[handlerIdx].GetInterfaceId() == iface.TLInterface.InterfaceID)
                            {
                                interfaceEventListeners.RemoveAt(handlerIdx);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            "Error erasing event hander from interface '{0}': {1}", iface.TLInterface.InterfaceID, ex.Message);
                    }
                    finally
                    {
                        eventListenersMutex.ReleaseMutex();
                    }
                }
            }

            public void RegisterInterfaceEventToSystem()
            {
                if (interfaceEventListenerOnSystem == null)
                {
                    //
                    // Create interface event listener for the system
                    //
                    // *** NOTES ***
                    // The InterfaceEventListener has been constructed to accept a system object in
                    // order to print the number of cameras on the system.
                    //
                    interfaceEventListenerOnSystem = new InterfaceEventListener(system);
                }

                //
                // Register interface event handler for the system
                //
                // *** NOTES ***
                // Arrival, removal, and interface event handlers can all be registered to
                // interfaces or the system. Do not think that interface event handlers can only be
                // registered to an interface. An interface event is merely a combination
                // of an arrival and a removal event.
                // Only arrival and removal events for GEV interfaces are currently supported.
                //
                // *** LATER ***
                // Arrival, removal, and interface event handlers must all be unregistered manually.
                // This must be done prior to releasing the system and while they are still
                // in scope.
                //
                system.RegisterEventHandler(interfaceEventListenerOnSystem);
                Console.WriteLine("Interface event handler registered on the system...");
            }

            public void UnregisterInterfaceEventFromSystem()
            {
                //
                // Unregister interface event handler from system object
                //
                // *** NOTES ***
                // It is important to unregister all arrival, removal, and interface event handlers
                // registered to the system.
                //
                if (interfaceEventListenerOnSystem != null)
                {
                    system.UnregisterEventHandler(interfaceEventListenerOnSystem);
                    Console.WriteLine("Interface event handler unregistered from system...");
                    interfaceEventListenerOnSystem = null;
                }
            }

            public void RegisterAllInterfaceEvents()
            {
                {
                    eventListenersMutex.WaitOne();

                    if (interfaceEventListeners.Count != 0)
                    {
                        interfaceEventListeners.Clear();
                    }

                    eventListenersMutex.ReleaseMutex();
                }

                ManagedInterfaceList interfaceList = system.GetInterfaces();
                int numInterfaces = interfaceList.Count;

                //
                // Create and register interface event handler to each interface
                //
                // *** NOTES ***
                // The process of event handler creation and registration on interfaces is similar
                // to the process of event handler creation and registration on the system. The
                // InterfaceEventListener class has been written to accept an interface and an
                // interface ID to differetiate between the interfaces.
                //
                // *** LATER ***
                // Arrival, removal, and interface event handlers must all be unregistered manually.
                // This must be done prior to releasing the system and while they are still
                // in scope.
                //
                for (uint i = 0; i < numInterfaces; i++)
                {
                    // Select interface
                    IManagedInterface manInterface = interfaceList.GetByIndex(i);
                    INodeMap nodeMap = manInterface.GetTLNodeMap();

                    IString interfaceIDNode = nodeMap.GetNode<IString>("InterfaceID");
                    // Ensure the node is valid
                    if (interfaceIDNode == null || !interfaceIDNode.IsReadable)
                    {
                        continue;
                    }

                    string interfaceID = interfaceIDNode.Value;

                    {
                        eventListenersMutex.WaitOne();
                        try
                        {
                            // Create interface event handler
                            InterfaceEventListener interfaceEventListener =
                                new InterfaceEventListener(interfaceID, manInterface);
                            interfaceEventListeners.Add(interfaceEventListener);

                            // Register interface event handler
                            manInterface.RegisterEventHandler(interfaceEventListener);

                            Console.WriteLine("Event handler registered to interface '{0}'...", interfaceID);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                "Error erasing event hander from interface '{0}': {1}", interfaceID, ex.Message);
                        }
                        finally
                        {
                            eventListenersMutex.ReleaseMutex();
                        }
                    }
                }
                Console.WriteLine();
            }

            public void UnregisterAllInterfaceEvents()
            {
                ManagedInterfaceList interfaceList = system.GetInterfaces(false);
                int numInterfaces = interfaceList.Count;

                eventListenersMutex.WaitOne();
                //
                // Unregister interface event handler from each interface
                //
                // *** NOTES ***
                // It is important to unregister all arrival, removal, and interface event handlers
                // from all interfaces that they may be registered to.
                //
                for (uint i = 0; i < numInterfaces; i++)
                {
                    // Select interface
                    IManagedInterface manInterface = interfaceList.GetByIndex(i);
                    INodeMap nodeMap = manInterface.GetTLNodeMap();

                    IString interfaceIDNode = nodeMap.GetNode<IString>("InterfaceID");
                    // Ensure the node is valid
                    if (interfaceIDNode == null || !interfaceIDNode.IsReadable)
                    {
                        continue;
                    }

                    string interfaceID = interfaceIDNode.Value;
                    {
                        try
                        {
                            foreach(InterfaceEventListener listener in interfaceEventListeners)
                            {
                                if (interfaceID == listener.GetInterfaceId())
                                {
                                    manInterface.UnregisterEventHandler(listener);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                "Error erasing event hander from interface '{0}': {1}", interfaceID, ex.Message);
                        }
                    }
                }

                interfaceEventListeners.Clear();
                Console.WriteLine("Event handler unregistered from interfaces...");
                eventListenersMutex.ReleaseMutex();
            }
        }

        // This function checks if GEV enumeration is enabled on the system.
        static void CheckGevEnabled(ManagedSystem pSystem)
        {
            // Retrieve the System TL Nodemap and EnumerateGEVInterfaces node.
            INodeMap nodeMapTLSystem = pSystem.GetTLNodeMap();
            IBool enumerateGevInterfacesNode = nodeMapTLSystem.GetNode<IBool>("EnumerateGEVInterfaces");

            // Ensure the node is valid.
            if (enumerateGevInterfacesNode != null && enumerateGevInterfacesNode.IsReadable)
            {
                bool gevEnabled = enumerateGevInterfacesNode.Value;

                // Check if node is enabled.
                if (!gevEnabled)
                {
                    Console.WriteLine("\nWARNING: GEV Enumeration is disabled.");
                    Console.WriteLine("If you intend to use GigE cameras please run the EnableGEVInterfaces shortcut");
                    Console.WriteLine("or set EnumerateGEVInterfaces to true and relaunch your application.\n");
                    return;
                }
            }
            else
            {
                Console.WriteLine("EnumerateGEVInterfaces node is unavailable");
                return;
            }

            Console.WriteLine("EnumerateGEVInterfaces is enabled. Continuing..");
        }

        // Example entry point; this function sets up the example to act
        // appropriately upon arrival and removal events; please see
        // Enumeration_CSharp example for more in-depth comments on
        // preparing and cleaning up the system.
        static int Main(string[] args)
        {
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

            // Check if GEV enumeration is enabled
            CheckGevEnabled(system);

            // Retrieve list of cameras from the system
            ManagedCameraList camList = system.GetCameras();

            Console.WriteLine("Number of cameras detected: {0}\n", camList.Count);

            // Retrieve list of interfaces from the system
            ManagedInterfaceList interfaceList = system.GetInterfaces();

            Console.WriteLine("Number of interfaces detected: {0}\n", interfaceList.Count);

            Console.WriteLine("\n*** CONFIGURING ENUMERATION EVENTS ***\n");

            //
            // Create system event listener
            //
            // *** NOTES ***
            // The SystemEventListener has been written to accept a system object in
            // the constructor in order to register/unregister events to/from the system object
            //
            SystemEventListener systemEventListener = new SystemEventListener(system);

            //
            // Register system event to the system
            //
            // *** NOTES ***
            // A system event is merely a combination of an interface arrival and an
            // interface removal event.
            // This feature is currently only supported for GEV interface arrivals and removals.
            //
            // *** LATER ***
            // Interface arrival and removal events must all be unregistered manually.
            // This must be done prior to releasing the system and while they are still
            // in scope.
            //
            system.RegisterEventHandler(systemEventListener);

            systemEventListener.RegisterInterfaceEventToSystem();
            systemEventListener.RegisterAllInterfaceEvents();

            // Wait for user to plug in and/or remove camera devices
            Console.WriteLine("\nReady! Remove/Plug in cameras to test or press Enter to exit...\n");
            Console.ReadLine();

            systemEventListener.UnregisterAllInterfaceEvents();
            systemEventListener.UnregisterInterfaceEventFromSystem();

            //
            // Unregister system event handler from system object
            //
            // *** NOTES ***
            // It is important to unregister all interface arrival and removal event handlers
            // registered to the system.
            //
            system.UnregisterEventHandler(systemEventListener);

            Console.WriteLine("System event handler unregistered from system...");

            // Clear camera list before releasing system
            camList.Clear();

            // Clear interface list before releasing system
            interfaceList.Clear();

            // Release system
            system.Dispose();

            Console.WriteLine("\nDone! Press Enter to exit...");
            Console.ReadLine();

            return 0;
        }
    }
}
