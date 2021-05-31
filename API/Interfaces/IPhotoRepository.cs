using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
  public interface IPhotoRepository
  {
    void RemovePhoto(Photo photo);
    Task<Photo> GetPhotoById(int id);
    Task<IEnumerable<PhotoForApprovalDTO>> GetUnapprovedPhotos();
  }
}