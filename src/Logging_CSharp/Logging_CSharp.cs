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
 *  @example Logging_CSharp.cs
 *
 *  @brief Logging_CSharp.cs shows how to log events on the system. It relies
 *  on information provided in the Enumeration_CSharp, Acquisition_CSharp, and
 *  NodeMapInfo_CSharp examples.
 *
 *	It can also be helpful to familiarize yourself with the
 *  NodeMapCallback_CSharp example, as callbacks follow the same general
 *  procedure as events, but with a few less steps.
 *
 *	This example creates a user-defined class, LogEventHandler, that inherits
 *	from the Spinnaker class, ManagedLoggingEventHandler. The child class,
 *  allows the user to define any properties, parameters, and the event itself
 *  while the parent class allows the child class to appropriately interface
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

namespace Logging_CSharp
{
    class Program
    {
        // Define callback priority threshold; please see documentation for
        // additional information on logging level philosophy.
        const ManagedLoggingLevel LoggingLevel = ManagedLoggingLevel.LOG_LEVEL_DEBUG;

        // Although logging events are just as flexible and extensible as other
        // events, they are generally only used for logging purposes, which is
        // why a number of helpful properties that provide logging information
        // have been added. Generally, if the purpose is not logging, an
        // interface, device, or image event is probably more appropriate.
        class LogEventListener : ManagedLoggingEventHandler
        {
            // This function displays readily available logging information.
            public override void OnLogEvent(ManagedLoggingEventData loggingEventData)
            {
                Console.WriteLine("--------Log Event Received----------");
                Console.WriteLine("Logger: {0}", loggingEventData.Logger);
                Console.WriteLine("UserName: {0}", loggingEventData.UserName);
                Console.WriteLine("Level: {0}", loggingEventData.Level);
                Console.WriteLine("Domain: {0}", loggingEventData.Domain);
                Console.WriteLine(string.Format("Timestamp: {0:dd-MM-yyyy hh:mm:ss.fff}", loggingEventData.Timestamp));
                Console.WriteLine("Thread: {0}", loggingEventData.TheadName);
                Console.WriteLine("Message: {0}", loggingEventData.Message);
                Console.WriteLine("------------------------------------\n");
            }
        }

        // Example entry point; notice the volume of data that the logging event
        // listener prints out on debug despite the fact that very little really
        // happens in this example. Because of this, it may be better to have
        // the logger set to lower level in order to provide a more concise,
        // focussed log.
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

            //
            // Create and register the logging event listener
            //
            // *** NOTES ***
            // Logging event handlers are registered to the system. Take note that a
            // logging event handler is very verbose when the logging level
            // is set to debug.
            //
            // *** LATER ***
            // Logging event handlers must be unregistered manually. This must be
            // done prior to releasing the system and while the logging event handlers
            // are still in scope.
            //
            LogEventListener logEventListener = new LogEventListener();
            system.RegisterLoggingEventHandler(logEventListener);

            //
            // Set callback priority level
            //
            // *** NOTES ***
            // Please see documentation for up-to-date information on the
            // logging philosophies of the Spinnaker SDK.
            //
            system.SetLoggingEventPriorityLevel(LoggingLevel);

            // Retrieve list of cameras from the system
            ManagedCameraList camList = system.GetCameras();

            Console.WriteLine("Number of cameras detected: {0}\n", camList.Count);

            // Dispose of cameras
            foreach(IManagedCamera cam in camList)
            {
                cam.Dispose();
            }

            // Clear camera list before releasing system
            camList.Clear();

            //
            // Unregister logging event listener
            //
            // *** NOTES ***
            // It is important to unregister all logging event handlers from the
            // system.
            //
            system.UnregisterLoggingEventHandler(logEventListener);

            // Release system
            system.Dispose();

            Console.WriteLine("\nDone! Press Enter to exit...");
            Console.ReadLine();

            return 0;
        }
    }
}
