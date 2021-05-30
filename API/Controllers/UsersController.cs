using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
  [Authorize]
  public class UsersController : BaseApiController
  {
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IPhotoService _photoService;
    public UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
    {
      _photoService = photoService;
      _mapper = mapper;
      _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<PagedList<MemberDTO>>> GetUsers([FromQuery] UserParams userParams)
    {
      var user = await _userRepository.GetUserByUserNameAsync(User.GetUserName());
      userParams.CurrentUserName = user.UserName;

      if (string.IsNullOrEmpty(userParams.Gender))
      {
        userParams.Gender = userParams.Gender == "male" ? "female" : "male";
      }


      PagedList<MemberDTO> users = await _userRepository.GetMembersAsync(userParams);

      Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);
      return Ok(users);
    }

    [HttpGet("{username}", Name = "GetUser")]
    public async Task<ActionResult<MemberDTO>> GetUser(string username)
    {
      return await _userRepository.GetMemberAsync(username);
    }

    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateDTO memberUpdateDTO)
    {
      AppUser user = await _userRepository.GetUserByUserNameAsync(User.GetUserName());

      _mapper.Map(memberUpdateDTO, user);
      _userRepository.Update(user);

      if (await _userRepository.SaveAllAsync()) return NoContent();

      return BadRequest("Failed to update user");
    }

    [HttpPost("add-photo")]
    public async Task<ActionResult<PhotoDTO>> AddPhoto(IFormFile file)
    {
      AppUser user = await _userRepository.GetUserByUserNameAsync(User.GetUserName());
      var result = await _photoService.AddPhotoAsync(file);

      if (result.Error != null) return BadRequest(result.Error.Message);

      var photo = new Photo
      {
        Url = result.SecureUrl.AbsoluteUri,
        PublicId = result.PublicId
      };

      if (user.Photos.Count == 0) photo.IsMain = true;

      user.Photos.Add(photo);

      if (await _userRepository.SaveAllAsync()) return CreatedAtRoute("GetUser", new { username = user.UserName }, _mapper.Map<PhotoDTO>(photo));

      return BadRequest("Problem occurred when adding photo to the profile.");
    }

    [HttpPut("set-main-photo/{photoId}")]
    public async Task<ActionResult> SetPhotoAsMain(int photoId)
    {
      var user = await _userRepository.GetUserByUserNameAsync(User.GetUserName());
      var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);

      if (photo.IsMain) return BadRequest("This is already your main photo");

      var currentMain = user.Photos.FirstOrDefault(p => p.IsMain);
      if (currentMain != null) currentMain.IsMain = false;
      photo.IsMain = true;

      if (await _userRepository.SaveAllAsync()) return NoContent();

      return BadRequest("Failed to set main photo");
    }

    [HttpDelete("delete-photo/{photoId}")]
    public async Task<ActionResult> DeletePhoto(int photoId)
    {
      var user = await _userRepository.GetUserByUserNameAsync(User.GetUserName());
      var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);
      if (photo == null) return NotFound();
      if (photo.IsMain) return BadRequest("Main photo cannot be deleted");
      if (photo.PublicId != null)
      {
        var result = await _photoService.DeletePhotoAsync(photo.PublicId);
        if (result.Error != null) return BadRequest(result.Error.Message);
      }

      user.Photos.Remove(photo);
      if (await _userRepository.SaveAllAsync()) return Ok();

      return BadRequest("Failed to delete photo");
    }
  }
}