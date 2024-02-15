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
 *  @example NodeMapInfo_CSharp.cs
 *
 *  @brief NodeMapInfo_CSharp.cs shows how to retrieve node map information.
 *  It relies on information provided in the Enumeration_CSharp example.
 *  Following this, check out the Acquisition_CSharp and
 *  ExceptionHandling_CSharp example if you haven't already. It explores
 *  acquiring images.
 *
 *	This example explores retrieving information from all major node types on
 *  the camera. This includes string, integer, float, boolean, command,
 *  enumeration, category, and value types. Looping through multiple child nodes
 *  is also covered. A few node types are not covered here - base, port, and
 *  register - as they are not representations of a fundamental data types.
 *  Enumeration entry node type is explored only in terms of its enumeration
 *  type parent.
 *
 *	Once comfortable with Acquisition_CSharp and NodeMapInfo_CSharp, we suggest
 *  checking out ImageFormatControl_CSharp and Exposure_CSharp.
 *  ImageFormatControl_CSharp explores customizing image settings on a camera
 *  while Exposure_CSharp introduces the standard structure of configuring a
 *  device, acquiring some images, and then returning the device to a default
 *  state.
 *  
 *  Please leave us feedback at: https://www.surveymonkey.com/r/TDYMVAPI
 *  More source code examples at: https://github.com/Teledyne-MV/Spinnaker-Examples
 *  Need help? Check out our forum at: https://teledynevisionsolutions.zendesk.com/hc/en-us/community/topics
 */

using System;
using System.Collections.Generic;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace NodeMapInfo_CSharp
{
    class Program
    {
        // This constant defines the maximum number of characters that will be
        // printed out for any information retrieved from a node.
        const int MaxChars = 35;

        // Use the following enum and global static variable to select whether
        // nodes are read as 'value' nodes or their individual types.
        enum readType
        {
            Value,
            Individual
        }

        static readType chosenRead = readType.Value;

        // This helper function deals with output indentation, of which there
        // is a lot.
        void indent(int level)
        {
            for (int i = 0; i < level; i++)
            {
                Console.Write("   ");
            }
        }

        // This function retrieves and prints the display name and value of all
        // node types as value nodes. A value node is a general node type that
        // allows for the reading and writing of any node type as a string.
        int printValueNode(INode node, int level)
        {
            int result = 0;

            try
            {
                // Cast as value node
                IValue iValueNode = (IValue) node;

                //
                // Retrieve display name
                //
                // *** NOTES ***
                // A node's 'display name' is generally more appropriate for
                // output and user interaction whereas its 'name' is what the
                // camera understands. Generally, its name is the same as its
                // display name but without spaces - for instance, the name of
                // the node that houses a camera's serial number is
                // 'DeviceSerialNumber' while its display name is 'Device
                // Serial Number'.
                //
                string displayName = iValueNode.DisplayName;

                //
                // Retrieve value of any node type as string
                //
                // *** NOTES ***
                // Because value nodes return any node type as a string, it can be much
                // easier to deal with nodes as value nodes rather than their actural
                // individual types. However, certain type-based functionality
                // - e.g. getMin(), which is only available on integer and float nodes
                // - is unavailable if cast as a value node.
                string value = iValueNode.ToString();

                // Ensure that the value length is not excessive for printing
                if (value.Length > MaxChars)
                {
                    value = value.Substring(0, MaxChars) + "...";
                }

                // Print value
                indent(level);
                Console.WriteLine("{0}: {1}", displayName, value);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function retrieves and prints the display name and value of a
        // string node, limiting the number of printed characters to a maximum
        // defined by MaxChars constant. Level parameter determines the
        // indentation level for the output.
        int printStringNode(INode node, int level)
        {
            int result = 0;

            try
            {
                // Cast as string node
                IString iStringNode = (IString) node;

                // Retrieve display name
                string displayName = iStringNode.DisplayName;

                //
                // Retrieve string node value
                //
                // *** NOTES ***
                // String node values in C# return string literals.
                //
                string value = iStringNode.Value;

                // Ensure that the value length is not excessive for printing
                if (value.Length > MaxChars)
                {
                    value = value.Substring(0, MaxChars) + "...";
                }

                // Print value
                indent(level);
                Console.WriteLine("{0}: {1}", displayName, value);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function retrieves and prints the display name and value of an
        // integer node.
        int printIntegerNode(INode node, int level)
        {
            int result = 0;

            try
            {
                // Cast node as integer node
                IInteger iIntegerNode = (IInteger) node;

                // Retrieve display name
                string displayName = iIntegerNode.DisplayName;

                //
                // Retrieve integer node value
                //
                // *** NOTES ***
                // Keep in mind that the data type of an integer node value is
                // a long as opposed to a standard int. While it is true that
                // the two are often interchangeable, it is recommended to use
                // long to avoid the introduction of bugs.
                //
                // All node types except for base and port nodes include a handy
                // ToString() method which returns a value as a string literal.
                //
                long value = iIntegerNode.Value;

                // Print value
                indent(level);
                Console.WriteLine("{0}: {1}", displayName, value);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function retrieves and prints the display name and value of a
        // float node.
        int printFloatNode(INode node, int level)
        {
            int result = 0;

            try
            {
                // Cast as float node
                IFloat iFloatNode = (IFloat) node;

                // Retrieve display name
                string displayName = iFloatNode.DisplayName;

                //
                // Retrieve float node value
                //
                // *** NOTES ***
                // Please take note that floating point numbers in the Spinnaker
                // SDK are almost always represented by the larger data type
                // double rather than float.
                //
                double value = iFloatNode.Value;

                // Print value
                indent(level);
                Console.WriteLine("{0}: {1}", displayName, value);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function retrieves and prints the display name and value of a
        // boolean, printing "true" for true and "false" for false rather than
        // the corresponding integer value ('1' and '0', respectively).
        int printBooleanNode(INode node, int level)
        {
            int result = 0;

            try
            {
                // Cast as boolean node
                IBool iBooleanNode = (IBool) node;

                // Retrieve display name
                string displayName = iBooleanNode.DisplayName;

                //
                // Retrieve value as a string representation
                //
                // *** NOTES ***
                // Boolean node type values are represented by the standard
                // bool data type. The boolean ToString() method returns either
                // a '1' or '0' as a string rather than a more descriptive word
                // like 'true' or 'false'.
                //
                string value = (iBooleanNode.Value ? "true" : "false");

                // Print value
                indent(level);
                Console.WriteLine("{0}: {1}", displayName, value);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function retrieves and prints the display name and tooltip of a
        // command node, limiting the number of printed characters to a
        // constant-defined maximum. The tooltip is printed because command
        // nodes do not have an intelligible value.
        int printCommandNode(INode node, int level)
        {
            int result = 0;

            try
            {
                // Cast as command node
                ICommand iCommandNode = (ICommand) node;

                // Retrieve display name
                string displayName = iCommandNode.DisplayName;

                //
                // Retrieve tooltip
                //
                // *** NOTES ***
                // All node types have a tooltip available. Tooltips provide
                // useful information about nodes. Command nodes do not have a
                // method to retrieve values as their is no intelligible value
                // to retrieve.
                //
                string tooltip = iCommandNode.ToolTip;

                // Ensure that the value length is not excessive for printing
                if (tooltip.Length > MaxChars)
                {
                    tooltip = tooltip.Substring(0, MaxChars) + "...";
                }

                // Print tooltip
                indent(level);
                Console.WriteLine("{0}: {1}", displayName, tooltip);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function retrieves and prints the display names of an enumeration
        // node and its current entry (which is actually housed in another node
        // unto itself).
        int printEnumerationNodeAndCurrentEntry(INode node, int level)
        {
            int result = 0;

            try
            {
                // Cast as enumeration node
                IEnum iEnumerationNode = (IEnum) node;

                //
                // Retrieve current entry as enumeration node
                //
                // *** NOTES ***
                // Enumeration nodes have three methods to differentiate between:
                // first, GetIntValue() returns the integer value of the current
                // entry node; second, GetCurrentEntry() returns the entry node
                // itself; and third, ToString() returns the symbolic of the
                // current entry.
                //
                EnumValue iEnumEntryValue = iEnumerationNode.Value;

                // Retrieve display name
                string displayName = iEnumerationNode.DisplayName;

                //
                // Retrieve current symbolic
                //
                // *** NOTES ***
                // Rather than retrieving the current entry node and then
                // retrieving its symbolic, ToString() accomplishes both
                // in a single step.
                //
                string currentEntrySymbolic = iEnumEntryValue.String;

                // Print current entry symbolic
                indent(level);
                Console.WriteLine("{0}: {1}", displayName, currentEntrySymbolic);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // This function retrieves and prints out the display name of a category
        // node before printing all child nodes. Child nodes that are also
        // category nodes are  printed recursively.
        int printCategoryNodeAndAllFeatures(INode node, int level)
        {
            int result = 0;

            try
            {
                // Cast as category node
                ICategory iCategoryNode = (ICategory) node;

                // Retrieve display name
                string displayName = iCategoryNode.DisplayName;

                // Print display name
                indent(level);
                Console.WriteLine("{0}", displayName);

                //
                // Retrieve children
                //
                // *** NOTES ***
                // The two nodes that typically have children are category nodes
                // and enumeration nodes. Throughout the examples, the children
                // of category nodes are referred to as features while the
                // children of enumeration nodes are referred to as entries.
                // Keep in mind that enumeration nodes can be cast as category
                // nodes, but category nodes cannot be cast as enumerations.
                //
                INode[] features = iCategoryNode.Features;

                //
                // Iterate through all children
                //
                // *** NOTES ***
                // If dealing with a variety of node types and their values, it
                // may be simpler to cast them as value nodes rather than as
                // their individual types. However, with this increased
                // ease -of-use, functionality is sacrificed.
                //
                foreach(INode iFeatureNode in features)
                {
                    // Ensure node is readable
                    if (!iFeatureNode.IsReadable)
                    {
                        continue;
                    }

                    // Category nodes must be dealt with separately in order to
                    // retrieve subnodes recursively.
                    if (iFeatureNode.GetType() == typeof (Category))
                    {
                        result = result | printCategoryNodeAndAllFeatures(iFeatureNode, level + 1);
                    }
                    // Cast all non-category nodes as value nodes
                    else if (chosenRead == readType.Value)
                    {
                        result = result | printValueNode(iFeatureNode, level + 1);
                    }
                    // Cast all non-category nodes as actual types
                    else if (chosenRead == readType.Individual)
                    {
                        if (iFeatureNode.GetType() == typeof (StringNode))
                        {
                            result = result | printStringNode(iFeatureNode, level + 1);
                        }
                        else if (iFeatureNode.GetType() == typeof (Integer))
                        {
                            result = result | printIntegerNode(iFeatureNode, level + 1);
                        }
                        else if (iFeatureNode.GetType() == typeof (Float))
                        {
                            result = result | printFloatNode(iFeatureNode, level + 1);
                        }
                        else if (iFeatureNode.GetType() == typeof (BoolNode))
                        {
                            result = result | printBooleanNode(iFeatureNode, level + 1);
                        }
                        else if (iFeatureNode.GetType() == typeof (Command))
                        {
                            result = result | printCommandNode(iFeatureNode, level + 1);
                        }
                        else if (iFeatureNode.GetType() == typeof (Enumeration))
                        {
                            result = result | printEnumerationNodeAndCurrentEntry(iFeatureNode, level + 1);
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

            return result;
        }

        // This function acts as the body of the example. First, nodes from the
        // transport layer device and stream nodemaps are retrieved and printed.
        // Following this, the camera is initialized and nodes from the
        // GenICam are retrieved and printed.
        int RunSingleCamera(IManagedCamera cam)
        {
            int result = 0;
            int level = 0;

            try
            {
                //
                // Retrieve TL device nodemap
                //
                // *** NOTES ***
                // The TL device nodemap is available on the transport
                // layer. As such, camera initialization is unnecessary. It
                // provides mostly immutable information fundamental to the
                // camera such as the serial number, vendor, and model.
                //
                Console.WriteLine("\n*** PRINTING TL DEVICE NODEMAP ***\n");

                INodeMap genTLNodeMap = cam.GetTLDeviceNodeMap();

                result = printCategoryNodeAndAllFeatures(genTLNodeMap.GetNode<ICategory>("Root"), level);

                //
                // Retrieve TL stream nodemap
                //
                // *** NOTES ***
                // The TL stream nodemap is also available on the transport layer.
                // Camera initialization is again unnecessary. As you can
                // probably guess, it provides information on the camera's
                // streaming performance at any given moment. Having this
                // information available on the transport layer allows the
                // information to be retrieved without affecting camera
                // performance.
                //
                Console.WriteLine("*** PRINTING TL STREAM NODEMAP ***\n");

                INodeMap nodeMapTLStream = cam.GetTLStreamNodeMap();

                result = result | printCategoryNodeAndAllFeatures(nodeMapTLStream.GetNode<ICategory>("Root"), level);

                //
                // Initialize camera
                //
                // *** NOTES ***
                // The camera becomes connected upon initialization. This
                // provides access to configurable options and additional
                // information, accessible through the GenICam nodemap
                // nodemap.
                //
                // *** LATER ***
                // Cameras should be deinitialized when no longer needed.
                //
                Console.WriteLine("*** PRINTING GENICAM NODEMAP ***\n");

                cam.Init();

                //
                // Retrieve GenICam nodemap
                //
                // *** NOTES ***
                // The GenICam nodemap is the primary gateway to customizing
                // and configuring the camera to suit your needs. Configuration
                // options such as image height and width, trigger mode enabling
                // and disabling
                //
                INodeMap appLayerNodeMap = cam.GetNodeMap();

                result = result | printCategoryNodeAndAllFeatures(appLayerNodeMap.GetNode<ICategory>("Root"), level);

                //
                // Deinitialize camera
                //
                // *** NOTES ***
                // Camera deinitialization helps ensure that devices clean up
                // properly and do not need to be power-cycled to maintain
                // integrity.
                //
                cam.DeInit();

                // Dispose of camera
                cam.Dispose();
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

            Console.WriteLine("Number of cameras detected: {0}\n", camList.Count);

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
