using System.IO;

namespace CRM.Infra.Misc {
    public static class FolderIO {

        public static void CheckIfFolderExists(string folderName) {

            if(!Directory.Exists(folderName)) {
                try {
                    Directory.CreateDirectory(folderName);
                } catch {
                    throw new Exception("Failed to create folder: " + folderName); 
                }
            }
        } 

    }
}