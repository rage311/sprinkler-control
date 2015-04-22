using System;
using System.IO;

using Microsoft.SPOT;
using Microsoft.SPOT.IO;

using GHIElectronics.NETMF.IO;



namespace SprinklerControl
{
    class FileSystem
    {
        public static bool MountSDCard()
        {
            PersistentStorage sdCard;

            try
            {
                // Create a new storage device
                sdCard = new PersistentStorage("SD");

                // Mount the file system
                sdCard.MountFileSystem();

                // Assume one storage device is available, access it through 
                // Micro Framework and display available files and folders:
                Debug.Print("Getting files and folders:");
                if (VolumeInfo.GetVolumes()[0].IsFormatted)
                {
                    return true;
                }
                else
                {
                    Debug.Print("Storage is not formatted. Format on PC with FAT32/FAT16 first.");
                    return false;
                }                
            }
            catch (Exception e)
            {
                Debug.Print("MountSDCard() exception caught: " + e.ToString());
                return false;
            }
        }

        public static FileStream GetFileStream(string filename)
        {
            try
            {
                string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                FileStream fStream = new FileStream(rootDirectory + "\\" + filename,
                                                    FileMode.Open, FileAccess.Read);
                return fStream;
            }
            catch (Exception e)
            {
                Debug.Print("GetFileStream() exception: " + e.ToString());
                return null;
            }
        }
    }
}
