using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
  public class AdminController : BaseApiController
  {
    private readonly UserManager<AppUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPhotoService _photoService;
    public AdminController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork, IPhotoService photoService)
    {
      _photoService = photoService;
      _unitOfWork = unitOfWork;
      _userManager = userManager;
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpGet("users-with-roles")]
    public async Task<ActionResult> GetUsersWithRoles()
    {
      var users = await _userManager.Users
        .Include(ur => ur.UserRoles)
        .ThenInclude(r => r.Role)
        .OrderBy(u => u.UserName)
        .Select(u => new
        {
          u.Id,
          UserName = u.UserName,
          Roles = u.UserRoles.Select(n => n.Role.Name).ToList()
        })
        .ToListAsync();

      return Ok(users);
    }

    [HttpPost("edit-roles/{username}")]
    public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles)
    {
      var selectedRoles = roles.Split(",").ToArray();
      var user = await _userManager.FindByNameAsync(username);
      if (user == null) return NotFound("Could not find user");

      var userRoles = await _userManager.GetRolesAsync(user);

      var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
      if (!result.Succeeded) return BadRequest("Failed to add the roles");

      result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
      if (!result.Succeeded) return BadRequest("Failed to remove the roles");

      return Ok(await _userManager.GetRolesAsync(user));
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpGet("photos-to-moderate")]
    public async Task<ActionResult> GetPhotosForModeration()
    {
      return Ok(await _unitOfWork.PhotoRepository.GetUnapprovedPhotos());
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("approve-photo/{photoId}")]
    public async Task<ActionResult> ApprovePhoto(int photoId)
    {
      var photo = await _unitOfWork.PhotoRepository.GetPhotoById(photoId);
      if (photo == null) return BadRequest("Cannot find photo with that specified id");
      photo.IsApproved = true;

      var user = await _unitOfWork.UserRepository.GetUserByPhotoId(photoId);

      if (!user.Photos.Any(x => x.IsMain)) photo.IsMain = true;

      await _unitOfWork.Complete();
      return Ok();
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("reject-photo/{photoId}")]
    public async Task<ActionResult> RejectPhoto(int photoId)
    {
      var photo = await _unitOfWork.PhotoRepository.GetPhotoById(photoId);
      if (photo == null) return BadRequest("Cannot find photo with that specified id");
      if (photo.PublicId != null)
      {
        await _photoService.DeletePhotoAsync(photo.PublicId);
      }
      _unitOfWork.PhotoRepository.RemovePhoto(photo);

      await _unitOfWork.Complete();
      return Ok();
    }
  }
}