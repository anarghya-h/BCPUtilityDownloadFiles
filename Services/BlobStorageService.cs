using Azure.Storage.Blobs;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.IO;

namespace BCPUtilityDownloadFiles.Services
{
    public class BlobStorageService
    {
        #region Private members
        string AccessKey { get; set; }
        string ContainerName { get; set; }
        BlobContainerClient client { get; set; }
        #endregion

        public BlobStorageService(string Key, string container)
        {
            this.AccessKey = Key;
            this.ContainerName = container;
            client = new BlobContainerClient(AccessKey, ContainerName);
        }

        public Uri UploadFileToBlob(string FileName, MemoryStream fileData)
        {
            //var a = client.Uri;
            var blob = client.GetBlobClient(FileName);
            var task = blob.UploadAsync(fileData, overwrite: true);
            task.Wait();
            return blob.Uri;
        }

        public void DeleteBlob(string fileUrl)
        {
            var blob = client.GetBlobClient(fileUrl);
            var task = blob.DeleteAsync();
            task.Wait();
        }

        public bool CheckExists(string FileUrl)
        {
            var blob = client.GetBlobClient(FileUrl);
            var task = blob.ExistsAsync();
            task.Wait();
            return task.Result.Value;
        }
    }
}
