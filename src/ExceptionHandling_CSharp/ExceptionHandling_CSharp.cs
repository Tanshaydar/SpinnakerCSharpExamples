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
 *	@example ExceptionHandling.cpp
 *
 *	@brief ExceptionHandling_CSharp.cpp shows the catching of an exception in
 *	Spinnaker. Following this, check out the Acquisition_CSharp or
 *  NodeMapInfo_CSharp examples if you haven't already. Acquisition_CSharp
 *  demonstrates image acquisition while NodeMapInfo_CSharp explores retrieving
 *  information from various node types.
 *
 *	This example shows three typical paths of exception handling in Spinnaker:
 *	catching the exception as a Spinnaker exception, as a standard exception, or
 *	as a standard exception which is then cast to a Spinnaker exception.
 *
 *	Once comfortable with Acquisition_CSharp, ExceptionHandling_CSharp, and
 *  NodeMapInfo_CSharp, we suggest checking out AcquisitionMultipleCamera_CSharp,
 *  NodeMapCallback_CSharp, or SaveToAvi_CSharp. AcquisitionMultipleCamera_CSharp
 *  demonstrates simultaneously acquiring images from a number of cameras,
 *  NodeMapCallback_CSharp serves as a good introduction to programming with
 *  callbacks and events, and SaveToAvi_CSharp exhibits video creation.
 *  
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics
 */

using System;
using System.Collections.Generic;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace Acquisition_CSharp
{
    class Program
    {
        // Use the following enum and global static variable to select the type
        // of exception handling used for the example.
        enum exceptionType
        {
            SpinnakerException,
            StandardException,
            StandardCastToSpinnaker
        }

        static exceptionType chosenException = exceptionType.SpinnakerException;

        // This helper function causes a Spinnaker exception by still holding a
        // camera reference while attempting to release the system.
        void causeSpinnakerException()
        {
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

            Console.WriteLine("System retrieved...");

            // Retrieve list of cameras from the system
            ManagedCameraList cameraList = system.GetCameras();

            Console.WriteLine("Camera list retrieved...\n");

            // The exception will only be thrown if a camera is connected
            if (cameraList.Count == 0)
            {
                Console.WriteLine("\nNot enough cameras!\n");

                return;
            }

            //
            // Begin acquisition without initializing camera
            //
            // *** NOTES ***
            // One of the requirements of acquiring images is that the camera is
            // initialized. Failing to initialize the camera is the reason that
            // the -1002 SPINNAKER_ERR_NOT_INITIALIZED error throws.
            //
            cameraList[0].BeginAcquisition();
        }

        // This helper function causes a standard exception by attempting to
        // access an member of a list that is out of range.
        void causeStandardException()
        {
            const int MaxNum = 10;
            List<int>numbers = new List<int>();

            // The list is initalized with 10 members, from indexes 0 to 9.
            for (int i = 0; i < MaxNum; i++)
            {
                numbers.Add(i);
            }

            Console.WriteLine("List initialized...\n");

            // The number attempting to be called here is index 10, or the 11th
            // member, which throws an out-of-range exception.
            Console.WriteLine("The highest number in the vector is {0}.", numbers[MaxNum]);
        }

        // Example entry point; this function demonstrates the handling of
        // a variety of exception use-cases.
        static int Main(string[] args)
        {
            Program program = new Program();

            switch (chosenException)
            {
                //
                // Catch a Spinnaker exception
                //
                // *** NOTES ***
                // The Spinnaker library has a number of built-in exceptions that
                // provide more information than standard exceptions.
                //
                case exceptionType.SpinnakerException:
                    try
                    {
                        program.causeSpinnakerException();
                    }
                    catch (SpinnakerException ex)
                    {
                        Console.WriteLine("\nSpinnaker exception caught.\n");

                        //
                        // Print Spinnaker exception
                        //
                        // *** NOTES ***
                        // Spinnaker exceptions are still able to access
                        // information using the standard what() method.
                        //
                        Console.WriteLine("Error: {0}", ex.Message);

                        //
                        // Print additional information available to Spinnaker
                        // exceptions
                        //
                        // *** NOTES ***
                        // However, Spinnaker exceptions have additional
                        // information. The message below prints out the error
                        // code and source; this functionality is not available
                        // for standard exceptions.
                        //
                        Console.WriteLine("Error code {0} at {1}.", ex.ErrorCode, ex.Source);
                    }
                    break;

                //
                // Catch a standard exception
                //
                // *** NOTES ***
                // Standard try-catch blocks can catch Spinnaker exceptions, but
                // provide no access to the additional information available.
                // Spinnaker exceptions, on the other hand, cannot catch standard
                // exceptions, but provide additional information on Spinnaker
                // exceptions.
                //
                case exceptionType.StandardException:
                    try
                    {
                        program.causeStandardException();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Standard exception caught.\n");

                        //
                        // Print more information
                        //
                        // *** NOTES ***
                        // The simplest way to catch exceptions in Spinnaker is
                        // with standard try-catch blocks. This will catch all
                        // exceptions, but sacrifice extra functionality of
                        // Spinnaker exceptions.
                        //
                        Console.WriteLine("Error: {0}.", ex.Message);
                    }
                    break;

                case exceptionType.StandardCastToSpinnaker:
                    try
                    {
                        program.causeSpinnakerException();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("\nStandard exception caught; will be cast as Spinnaker exception.\n");

                        //
                        // Wrap the cast in a further try-catch
                        //
                        // *** NOTES ***
                        // The cast needs to be wrapped in a further try-catch
                        // block because standard exceptions will fail the cast.
                        //
                        try
                        {
                            //
                            // Attempt to cast exception as Spinnaker exception
                            //
                            // *** NOTES ***
                            // A successful cast means that the exception is
                            // particular to Spinnaker and will keep the flow of
                            // control in the try block while a failed cast means
                            // that the exception is standard and will push the
                            // flow of control into the catch block.
                            //
                            SpinnakerException spinEx = (SpinnakerException) ex;

                            // Print additional information if Spinnaker exception
                            Console.WriteLine("Error: {0}", spinEx.Message);
                            Console.WriteLine("Error code {0} at {1}.", spinEx.ErrorCode, spinEx.Source);
                        }
                        catch (Exception stdEx)
                        {
                            Console.WriteLine("Cannot cast; not a Spinnaker exception: {0}.", stdEx.Message);

                            // Print standard information if standard exception
                            Console.WriteLine("Error: {0}.", ex.Message);
                        }
                    }
                    break;
            }

            Console.WriteLine("\nDone! Press Enter to exit...");
            Console.ReadLine();

            return 0;
        }
    }
}
