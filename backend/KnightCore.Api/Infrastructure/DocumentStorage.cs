using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using KnightCore.Application;

namespace KnightCore.Infrastructure;

public sealed class LocalInvoiceStorage(IConfiguration config,IWebHostEnvironment environment) : IInvoiceStorage
{
    private readonly string root=Path.GetFullPath(Path.Combine(environment.ContentRootPath,config["DataDirectory"]??"data","invoices"));
    private string Resolve(string key)
    {
        var path=Path.GetFullPath(Path.Combine(root,key));
        if(!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidOperationException("Invalid document key.");
        return path;
    }
    public async Task SaveAsync(string key,byte[] data,CancellationToken ct)
    {
        var path=Resolve(key);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        await File.WriteAllBytesAsync(temp,data,ct);
        try{File.Move(temp,path,false);}catch(IOException) when(File.Exists(path)){File.Delete(temp);}
    }
    public Task<byte[]> ReadAsync(string key,CancellationToken ct)=>File.ReadAllBytesAsync(Resolve(key),ct);
}
public sealed class AzureInvoiceStorage(IConfiguration config) : IInvoiceStorage
{
    private readonly BlobContainerClient container=new(config["InvoiceStorage:ConnectionString"],config["InvoiceStorage:Container"]??"invoices");
    public async Task SaveAsync(string key,byte[] data,CancellationToken ct)
    {
        await container.CreateIfNotExistsAsync(PublicAccessType.None,cancellationToken:ct);
        using var stream=new MemoryStream(data);
        try{await container.GetBlobClient(key).UploadAsync(stream,new BlobUploadOptions{HttpHeaders=new(){ContentType="application/pdf"},Conditions=new(){IfNoneMatch=ETag.All}},ct);}
        catch(RequestFailedException e) when(e.Status is 409 or 412){}
    }
    public async Task<byte[]> ReadAsync(string key,CancellationToken ct)=>(await container.GetBlobClient(key).DownloadContentAsync(ct)).Value.Content.ToArray();
}

