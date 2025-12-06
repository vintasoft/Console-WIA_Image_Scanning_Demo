using System;
using System.IO;

using Vintasoft.WiaImageScanning;

namespace WiaImageScanningConsoleDemo
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                //WiaEnvironment.EnableDebugging("wia.log");

                // create WIA device manager
                using (WiaDeviceManager deviceManager = new WiaDeviceManager())
                {
                    // open WIA device manager
                    deviceManager.Open();

                    // select WIA device
                    WiaDevice device = SelectWiaDevice(deviceManager);
                    // if device is not selected
                    if (device == null)
                        return;

                    // specify that device should not show UI
                    device.ShowUI = false;
                    // specify that device should not show the image scanning progess dialog
                    device.ShowIndicators = false;

                    // open the device
                    device.Open();

                    // output the device name
                    Console.WriteLine(string.Format("Device name: {0}", device.Name));
                    Console.WriteLine();

                    // select the scan input source for device
                    SelectDeviceScanInputSource(device);

                    // specify that intent is not set
                    device.ScanIntent = WiaScanIntent.None;
                    // select the image pixel type for scanning images
                    WiaImagePixelType? scanImagePixelType = SelectWiaImagePixelType(device);
                    // if image pixel type is selected
                    if (scanImagePixelType != null)
                    {
                        // specify the image pixel type for scanning images
                        device.ScanImagePixelType = scanImagePixelType.Value;
                    }
                    // specify that images must be scanned with 300 dpi resolution
                    device.ScanXResolution = 300;
                    device.ScanYResolution = 300;

                    Console.WriteLine();
                    Console.WriteLine("Images acquisition is started...");
                    int imageIndex = 0;
                    WiaAcquiredImage acquiredImage = null;
                    do
                    {
                        try
                        {
                            // acquire image from WIA device
                            acquiredImage = device.AcquireImageSync();
                            // if image is acquired
                            if (acquiredImage != null)
                            {
                                Console.WriteLine("Image is acquired.");

                                string filename = string.Format("scannedImage{0}.png", imageIndex);
                                if (File.Exists(filename))
                                    File.Delete(filename);

                                // save acquired image to a PNG file
                                acquiredImage.Save(filename);

                                Console.WriteLine(string.Format("Image{0} is saved.", imageIndex++));
                            }
                            // if image is not acquired
                            else
                            {
                                Console.WriteLine("Scan is finished.");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(string.Format("Scan is failed: {0}", ex.Message));
                            break;
                        }
                    }
                    while (acquiredImage != null);

                    // close the device
                    device.Close();

                    // close the device manager
                    deviceManager.Close();
                }
            }
            catch (WiaException ex)
            {
                Console.WriteLine("Error: " + GetFullExceptionMessage(ex));
            }
            catch (Exception ex)
            {
                System.ComponentModel.LicenseException licenseException = GetLicenseException(ex);
                if (licenseException != null)
                {
                    // show information about licensing exception
                    Console.WriteLine("{0}: {1}", licenseException.GetType().Name, licenseException.Message);

                    // open article with information about usage of evaluation license
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = "https://www.vintasoft.com/docs/vstwain-dotnet/Licensing-Twain-Evaluation.html";
                    process.StartInfo.UseShellExecute = true;
                    process.Start();
                }
                else
                {
                    throw;
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Selects the WIA device.
        /// </summary>
        /// <param name="deviceManager">WIA device manager.</param>
        /// <returns>WIA device if device is selected; otherwiase, <b>null</b>.</returns>
        private static WiaDevice SelectWiaDevice(WiaDeviceManager deviceManager)
        {
            int deviceCount = deviceManager.Devices.Count;
            // if no devices are found in the system
            if (deviceCount == 0)
            {
                Console.WriteLine("Devices are not found.");
                return null;
            }

            Console.WriteLine("Device list:");
            // for each device
            for (int i = 0; i < deviceCount; i++)
            {
                // display the device name
                Console.WriteLine(string.Format("{0}. {1}", i + 1, deviceManager.Devices[i].Name));
            }

            int deviceIndex = -1;
            while (deviceIndex < 0 || deviceIndex > deviceCount)
            {
                Console.Write(string.Format("Please select device by entering the device number from '1' to '{0}' ('0' to cancel) and press 'Enter' key: ", deviceCount));
                string deviceIndexString = Console.ReadLine();
                int.TryParse(deviceIndexString, out deviceIndex);
            }
            Console.WriteLine();

            if (deviceIndex == 0)
                return null;

            return deviceManager.Devices[deviceIndex - 1];
        }

        /// <summary>
        /// Selects the scan input source for WIA device.
        /// </summary>
        /// <param name="device">WIA device.</param>
        private static void SelectDeviceScanInputSource(WiaDevice device)
        {
            // if device has flatbed and feeder
            if (device.HasFlatbed && device.HasFeeder)
            {
                if (device.HasDuplex)
                    Console.Write("Device has flatbed and feeder with duplex.");
                else
                    Console.Write("Device has flatbed and feeder without duplex.");

                if (device.IsFeederEnabled)
                    Console.WriteLine(" Now device uses feeder.");
                else
                    Console.WriteLine(" Now device uses flatbed.");


                // ask user to select flatbed or feeder

                int scanInputSourceIndex = -1;
                while (scanInputSourceIndex != 1 && scanInputSourceIndex != 2)
                {
                    Console.Write("What do you want to use: flatbed (press '1') or feeder (press '2'): ");
                    scanInputSourceIndex = Console.ReadKey().KeyChar - '0';
                    Console.WriteLine();
                }
                Console.WriteLine();

                if (scanInputSourceIndex == 1)
                    device.IsFlatbedEnabled = true;
                else
                    device.IsFeederEnabled = true;
            }
            // if device has feeder only
            else if (!device.HasFlatbed && device.HasFeeder)
            {
                if (device.HasDuplex)
                    Console.WriteLine("Device has feeder with duplex.");
                else
                    Console.WriteLine("Device has feeder without duplex.");
                Console.WriteLine();
            }
            // if device has flatbed only
            else if (device.HasFlatbed && !device.HasFeeder)
            {
                Console.WriteLine("Device has flatbed only.");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Selects the image pixel type for WIA device.
        /// </summary>
        /// <param name="device">WIA device.</param>
        /// <returns>null - image pixel type is not selected; otherwise, selectes image pixel type.</returns>
        private static WiaImagePixelType? SelectWiaImagePixelType(WiaDevice device)
        {
            WiaImagePixelType[] supportedScanPixelTypes = device.GetSupportedImagePixelTypes();
            Console.WriteLine("Image pixel types:");
            for (int i = 0; i < supportedScanPixelTypes.Length; i++)
            {
                Console.WriteLine(string.Format("{0}. {1}", i + 1, supportedScanPixelTypes[i]));
            }

            int scanColorModeIndex = -1;
            while (scanColorModeIndex < 0 || scanColorModeIndex > supportedScanPixelTypes.Length)
            {
                Console.Write(string.Format("Please select image pixel type by entering the number from '1' to '{0}' or press '0' to cancel: ", supportedScanPixelTypes.Length));
                scanColorModeIndex = Console.ReadKey().KeyChar - '0';
                Console.WriteLine();
            }
            Console.WriteLine();

            if (scanColorModeIndex == 0)
                return null;

            return supportedScanPixelTypes[scanColorModeIndex - 1];
        }

        /// <summary>
        /// Returns message that contains information about exception and inner exceptions.
        /// </summary>
        private static string GetFullExceptionMessage(Exception ex)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(ex.Message);

            Exception innerException = ex.InnerException;
            while (innerException != null)
            {
                if (ex.Message != innerException.Message)
                    sb.AppendLine(string.Format("Inner exception: {0}", innerException.Message));
                innerException = innerException.InnerException;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns the license exception from specified exception.
        /// </summary>
        /// <param name="exceptionObject">The exception object.</param>
        /// <returns>Instance of <see cref="LicenseException"/>.</returns>
        private static System.ComponentModel.LicenseException GetLicenseException(object exceptionObject)
        {
            Exception ex = exceptionObject as Exception;
            if (ex == null)
                return null;
            if (ex is System.ComponentModel.LicenseException)
                return (System.ComponentModel.LicenseException)exceptionObject;
            if (ex.InnerException != null)
                return GetLicenseException(ex.InnerException);
            return null;
        }

    }
}
