using System.IO;
using System.Threading.Tasks;
using API.Helpers;
using API.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace API.Services
{
  public class PhotoService : IPhotoService
  {
      private readonly Cloudinary _cloudinary;
    public PhotoService(IOptions<CloudinarySettings> config)
    {
        Account acc = new Account (
            config.Value.CloudName,
            config.Value.ApiKey,
            config.Value.ApiSecretKey
        );

        _cloudinary = new Cloudinary(acc);
    }

    public async Task<ImageUploadResult> AddPhotoAsync(IFormFile file)
    {
      ImageUploadResult uploadResult = new ImageUploadResult();
      if (file.Length > 0)
      {
          using Stream stream = file.OpenReadStream();
          ImageUploadParams uploadParameter = new ImageUploadParams
          {
              File = new FileDescription(file.FileName, stream),
              Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face")
          };
          uploadResult = await _cloudinary.UploadAsync(uploadParameter);
      } 

      return uploadResult;
    }    

    public async Task<DeletionResult> DeletePhotoAsync(string publicId)
    {
      DeletionParams deleteParams = new DeletionParams(publicId);
      DeletionResult result = await _cloudinary.DestroyAsync(deleteParams);
      return result;
    }
  }
}