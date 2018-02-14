#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"
 
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage; 
using Microsoft.WindowsAzure.Storage.Blob;
using ImageResizer;
using ImageResizer.ExtensionMethods;

static string storageAccountConnectionString = System.Environment.GetEnvironmentVariable("myBlobStorage_STORAGE");
static string thumbContainerName = System.Environment.GetEnvironmentVariable("myContainerName");
static string imagesContainerName = System.Environment.GetEnvironmentVariable("myContainerImagesName");

public static async Task Run(JObject eventGridEvent, TraceWriter log)
{
    // Instructions to resize the blob image.
    var instructions = new Instructions
    {
        Width = 150,
        Height = 150,
        Mode = FitMode.Crop,
        Scale = ScaleMode.Both
    };   

    log.Info(eventGridEvent.ToString(Formatting.Indented));
    // Get the blobname from the event's JObject.
    string bloblUrl = (string)eventGridEvent["data"]["url"];
    
    string blobName = GetBlobNameFromUrl(bloblUrl);
    // Retrieve storage account from connection string.
    
    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
    // Create the blob client.
    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

    // Retrieve reference to a previously created container.
    CloudBlobContainer outputcontainer = blobClient.GetContainerReference(thumbContainerName);
    CloudBlobContainer inputcontainer = blobClient.GetContainerReference(imagesContainerName);
    using (Stream inputBlob = await inputcontainer.GetBlockBlobReference(blobName).OpenReadAsync())
    {
        // Create reference to a blob named "blobName".
        CloudBlockBlob blockBlob = outputcontainer.GetBlockBlobReference(blobName);

        using (MemoryStream myStream = new MemoryStream())
        {
            // Resize the image with the given instructions into the stream.
            ImageBuilder.Current.Build(new ImageJob(inputBlob, myStream, instructions));

            // Reset the stream's position to the beginning.
            myStream.Position = 0;

            // Write the stream to the new blob.
            await blockBlob.UploadFromStreamAsync(myStream);
        }
    }
}
private static string GetBlobNameFromUrl(string bloblUrl)
{
    var myUri = new Uri(bloblUrl);
    var myCloudBlob = new CloudBlob(myUri);
    return myCloudBlob.Name;
}